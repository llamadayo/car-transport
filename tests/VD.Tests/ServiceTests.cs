using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;
using VD.Core;
using VD.Services;
namespace VD.Tests {
 [NonParallelizable] public class ServiceTests {
  private string path;private DatabaseSettings settings;private RegionalApplicationService apps;private RegionalQueryService queries;private RegionalDispatchService dispatch;
  private sealed class FixedClock:IClock { public DateTime Now=>new DateTime(2026,9,21,7,0,0); }
  private static CargoItem Box()=>new CargoItem {Name="測試箱",LengthCm=50,WidthCm=40,HeightCm=30,Category="BOX",Qty=1,Weight=10};
  private static ApplicationInput Input()=>new ApplicationInput {Applicant="測試人 O'Brien",BranchId="D1",PickStationId="D1-100",DropStationId="D1-900",PickBuilding="100",DropBuilding="900",RecvMode="exact",ServiceDate="2026-09-21",LoadMin=10,UnloadMin=5,RecipientName="接收人",Items=new List<CargoItem>{Box()}};
  [SetUp] public void Setup() {
   path=Path.Combine(Path.GetTempPath(),"vd-test-"+Guid.NewGuid()+".sqlite");settings=new DatabaseSettings("SQLite","Data Source="+path+";Version=3;Pooling=False;Default Timeout=30;");
   new DatabaseSetupService(settings).InitializeSqlite(Path.Combine(TestContext.CurrentContext.TestDirectory,"Database"));
   apps=new RegionalApplicationService(settings,new FixedClock());queries=new RegionalQueryService(settings);dispatch=new RegionalDispatchService(settings);
  }
  [TearDown] public void Cleanup() {WasteFactorProvider.Invalidate(settings.Provider+settings.ConnectionString);SQLiteConnection.ClearAllPools();if(File.Exists(path))File.Delete(path);}
  [Test] public void SubmitPersistsAcrossServiceInstancesAndInitializerIsIdempotent() {
   var row=apps.Submit(Input());Assert.That(row.Status,Is.EqualTo("matched"));
   new DatabaseSetupService(settings).InitializeSqlite(Path.Combine(TestContext.CurrentContext.TestDirectory,"Database"));
   var again=new RegionalQueryService(settings).GetApplication(row.Id);Assert.That(again.Application.Items.Single().Name,Is.EqualTo("測試箱"));Assert.That(again.Application.Trace,Is.Not.Empty);Assert.That(queries.ReadApplications(new ApplicationQuery{Applicant="O'Brien"}).Count,Is.EqualTo(1));
  }
  [Test] public void InvalidItemsLeaveNoPartialOrderOrConsumedSqliteCounter() {
   var bad=Input();bad.Items[0].Qty=0;Assert.Throws<ArgumentException>(()=>apps.Submit(bad));Assert.That(queries.ReadApplications(),Is.Empty);Assert.That(apps.Submit(Input()).AppNo,Is.EqualTo("LA001"));
  }
  [Test] public void InsertFailureRollsBackOrderCargoTripAndCounter() {
   using(var c=new SQLiteConnection(settings.ConnectionString)){c.Open();c.Execute("CREATE TRIGGER fail_item BEFORE INSERT ON VD_REG_ITEM BEGIN SELECT RAISE(ABORT, 'forced test error'); END;");}
   Assert.Throws<SQLiteException>(()=>apps.Submit(Input()));Assert.That(queries.ReadApplications(),Is.Empty);
   using(var c=new SQLiteConnection(settings.ConnectionString)){c.Open();Assert.That(c.QuerySingle<int>("SELECT COUNT(*) FROM VD_REG_TRIP"),Is.Zero);c.Execute("DROP TRIGGER fail_item");}
   Assert.That(apps.Submit(Input()).AppNo,Is.EqualTo("LA001"));
  }
  [Test] public void OversizeCanEditAndRematchButMatchedCannot() {
   var input=Input();input.Items[0].LengthCm=10000;var row=apps.Submit(input);Assert.That(row.Application.FailureReason,Is.EqualTo("toobig"));
   row=apps.UpdateAndRematch(row.Id,new List<CargoItem>{Box()});Assert.That(row.Status,Is.EqualTo("matched"));Assert.That(row.Application.MatchAttempt,Is.EqualTo(2));Assert.Throws<ArgumentException>(()=>apps.UpdateAndRematch(row.Id,new List<CargoItem>{Box()}));
  }
  [Test] public void DateAndBranchIsolateCapacity() {
   var i=Input();i.LoadMin=60;i.UnloadMin=0;var first=apps.Submit(i);var next=apps.Submit(Input());Assert.That(next.Application.ShiftId,Is.Not.EqualTo(first.Application.ShiftId));
   var other=Input();other.ServiceDate="2026-09-22";Assert.That(apps.Submit(other).Application.ShiftId,Is.EqualTo(first.Application.ShiftId));
   other=Input();other.BranchId="D2";other.PickStationId="D2-100";other.DropStationId="D2-900";Assert.That(apps.Submit(other).Application.ShiftId,Is.EqualTo("D2-R1"));
  }
  [Test] public void OverridesAreConsistentInQueriesAndFutureMatchingAndDateScoped() {
   dispatch.SaveTripPlan("2026-09-21","D1-R1","V-L02","DR2");
   var i=Input();i.Items[0].Weight=2700;var heavy=apps.Submit(i);Assert.That(heavy.Application.ShiftId,Is.EqualTo("D1-R3"));
   var small=apps.Submit(Input());Assert.That(small.Application.ShiftId,Is.EqualTo("D1-R1"));Assert.That(small.VehicleId,Is.EqualTo("V-L02"));Assert.That(small.DriverName,Is.EqualTo("林志明"));
   Assert.That(queries.ReadDriverTasks().Single(t=>t.ShiftId=="D1-R1").VehicleId,Is.EqualTo("V-L02"));
   var tomorrow=Input();tomorrow.ServiceDate="2026-09-22";Assert.That(apps.Submit(tomorrow).VehicleId,Is.EqualTo("V-L01"));
  }
  [Test] public void ManualReassignPreservesOverridePolicyAndClearsStaleFailure() {
   var i=Input();i.Items[0].LengthCm=10000;i.ExpectedTime="14:00";var row=apps.Submit(i);
   row=dispatch.ReassignShift(row.Id,"2026-09-21","D1-R1");Assert.That(row.Status,Is.EqualTo("matched"));Assert.That(row.Application.Note,Is.Null.Or.Empty);Assert.That(row.Application.ExpectDiffMin,Is.EqualTo(507-840));
   row=dispatch.RemoveFromShift(row.Id);Assert.That(row.Status,Is.EqualTo("unscheduled"));Assert.That(row.Application.ShiftId,Is.Null);Assert.That(row.Application.ExpectDiffMin,Is.Null);Assert.That(row.Application.Items.Count,Is.EqualTo(1));
  }
  [Test] public void RejectCrossDateBranchAndPoolMoves() {
   var row=apps.Submit(Input());Assert.Throws<ArgumentException>(()=>dispatch.ReassignShift(row.Id,"2026-09-22","D1-R1"));Assert.Throws<ArgumentException>(()=>dispatch.ReassignShift(row.Id,"2026-09-21","D2-R1"));Assert.Throws<ArgumentException>(()=>dispatch.SaveTripPlan("2026-09-21","D1-R1","V-T01","DR1"));Assert.Throws<ArgumentException>(()=>dispatch.SaveTripPlan("2026-09-21","D1-R1","V-B01","DR1"));
  }
  [Test] public void DriverTasksGroupAllDatesAndUnloadThenPickupAtSameStop() {
   var a=Input();a.DropStationId="D1-600";apps.Submit(a);var b=Input();b.PickStationId="D1-600";apps.Submit(b);var next=Input();next.ServiceDate="2026-09-22";apps.Submit(next);
   var tasks=queries.ReadDriverTasks();Assert.That(tasks.Count,Is.EqualTo(2));var stop=tasks[0].Stops.Single(s=>s.SortOrder==6);Assert.That(stop.Picks.Count,Is.EqualTo(1));Assert.That(stop.Drops.Count,Is.EqualTo(1));Assert.That(tasks[0].Stops.Select(s=>s.SortOrder),Is.Ordered);
  }
  [Test] public void UnknownCategoryUsesFallbackAndCanPersist() {var i=Input();i.Items[0].Category="UNKNOWN";Assert.That(apps.Submit(i).Status,Is.EqualTo("matched"));}
  [Test] public void IncidentAndDeliveryPersistWithoutSendingMail() {
   var a=apps.Submit(Input());apps.ReportIncident(a.Id,"使用者不準時");Assert.That(queries.GetApplication(a.Id).Application.Incident,Is.EqualTo("使用者不準時"));apps.ReportIncident(a.Id,"");Assert.That(queries.GetApplication(a.Id).Application.Incident,Is.Null.Or.Empty);
   apps.ConfirmDelivery(a.Id,"調度室");Assert.That(queries.GetApplication(a.Id).Status,Is.EqualTo("delivered"));Assert.That(queries.ReadDriverTasks().Count,Is.EqualTo(1));Assert.That(queries.ReadTrips(),Is.Empty);
  }
  [Test] public void ConcurrentRequestsDoNotOverbookTheSixtyMinuteBudget() {
   Parallel.For(0,6,new ParallelOptions{MaxDegreeOfParallelism=3},n=>{var i=Input();i.LoadMin=30;i.UnloadMin=0;new RegionalApplicationService(settings,new FixedClock()).Submit(i);});
   var trips=queries.ReadTrips();Assert.That(trips.Count,Is.EqualTo(3));Assert.That(trips.All(t=>t.Count==2),Is.True);
  }
 }
}
