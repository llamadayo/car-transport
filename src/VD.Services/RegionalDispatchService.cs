using System;
using System.Data;
using System.Linq;
using Dapper;
using VD.Core;

namespace VD.Services
{
    public sealed class RegionalDispatchService : ServiceBase
    {
        public RegionalDispatchService(DatabaseSettings settings) : base(settings) { }
        internal static void EnsureTrip(IDbConnection c,IDbTransaction tx,DatabaseSettings settings,string date,Shift shift)
        {
            object value=settings.IsOracle?(object)BusinessTime.ParseDate(date):date;
            var p=new {ServiceDate=value,ShiftId=shift.Id,shift.VehicleId,shift.DriverId};
            if(c.QuerySingle<int>("SELECT COUNT(*) FROM VD_REG_TRIP WHERE SERVICE_DATE=:ServiceDate AND SHIFT_ID=:ShiftId",p,tx)==0)
                c.Execute("INSERT INTO VD_REG_TRIP (SERVICE_DATE,SHIFT_ID,VEHICLE_ID,DRIVER_ID) VALUES (:ServiceDate,:ShiftId,:VehicleId,:DriverId)",p,tx);
        }
        public TripRow SaveTripPlan(string date,string shiftId,string vehicleId,string driverId)
        {
            BusinessTime.ParseDate(date);
            using(var c=OpenConnection()) using(var tx=BeginWrite(c))
            {
                try {
                    var m=new MasterDataService(Settings).Read(c,tx); var s=m.Shifts.SingleOrDefault(x=>x.Id==shiftId);
                    if(s==null) throw new ArgumentException("班次不存在。");
                    LockBranch(c,tx,s.BranchId);
                    if(!m.Vehicles.Any(v=>v.Id==vehicleId && v.Pool=="LOGI" && string.IsNullOrEmpty(v.SizeClass))||!m.Drivers.Any(d=>d.Id==driverId && d.Pool=="LOGI")) throw new ArgumentException("請選擇區域物流車輛及司機。");
                    EnsureTrip(c,tx,Settings,date,s);
                    c.Execute("UPDATE VD_REG_TRIP SET VEHICLE_ID=:VehicleId,DRIVER_ID=:DriverId WHERE SERVICE_DATE=:ServiceDate AND SHIFT_ID=:ShiftId",new {VehicleId=vehicleId,DriverId=driverId,ServiceDate=DateValue(date),ShiftId=shiftId},tx);
                    tx.Commit();
                } catch { if(tx.Connection!=null) tx.Rollback(); throw; }
            }
            return new RegionalQueryService(Settings).GetTrip(date,shiftId);
        }
        public ApplicationRow ReassignShift(long id,string date,string shiftId)
        {
            BusinessTime.ParseDate(date);
            using(var c=OpenConnection()) using(var tx=BeginWrite(c))
            {
                try {
                    var a=new RegionalQueryService(Settings).Get(c,tx,id); LockBranch(c,tx,a.BranchId); a=new RegionalQueryService(Settings).Get(c,tx,id); var m=new MasterDataService(Settings).Read(c,tx);
                    var s=m.Shifts.SingleOrDefault(x=>x.Id==shiftId);
                    if(s==null||s.BranchId!=a.BranchId||date!=a.ServiceDate) throw new ArgumentException("僅可改派至同日、同分公司的班次。");
                    if(a.Status!="matched"&&a.Status!="unscheduled") throw new ArgumentException("此申請狀態不可改派。");
                    EnsureTrip(c,tx,Settings,date,s);
                    int arrival=s.DepartMinute+m.Stations.Single(x=>x.Id==a.DropStationId).SortOrder*m.InterStationMin;
                    c.Execute(@"UPDATE VD_REG_APP SET SHIFT_ID=:ShiftId,STATUS='matched',ARRIVAL_MINUTE=:Arrival,EXPECT_DIFF_MIN=:Diff,NOTE=NULL,FAILURE_REASON=NULL WHERE ID=:Id",
                        new { ShiftId=shiftId,Arrival=arrival,Diff=a.ExpectedMinute.HasValue?(int?)(arrival-a.ExpectedMinute.Value):null,Id=id },tx);
                    // Preserve explicit manual override policy: do not silently add a capacity veto.
                    tx.Commit();
                } catch { if(tx.Connection!=null) tx.Rollback(); throw; }
            }
            return new RegionalQueryService(Settings).GetApplication(id);
        }
        public ApplicationRow RemoveFromShift(long id)
        {
            using(var c=OpenConnection())
            {
                int count=c.Execute(@"UPDATE VD_REG_APP SET SHIFT_ID=NULL,STATUS='unscheduled',ARRIVAL_MINUTE=NULL,EXPECT_DIFF_MIN=NULL,FAILURE_REASON='removed',
                    NOTE=:Note WHERE ID=:Id AND STATUS='matched'",new { Id=id,Note="已由「已排定車次異動」移出班次，待重新指定。" });
                if(count!=1) throw new ArgumentException("僅已排班申請可移出班次。");
            }
            return new RegionalQueryService(Settings).GetApplication(id);
        }
    }
}
