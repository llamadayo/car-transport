using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
#if !LOCAL_DEMO
using System.Data;
using Dapper;
#endif
namespace CARGO.Service {
 internal sealed class CARGO_DUseState {
  internal CARGO_DWorkState Work;
  internal List<CARGO_D_USAGE> Usages;
  internal List<CARGO_D_DRIVER_USER> DriverUsers;
 }
 internal static class CARGO_DUseData {
#if LOCAL_DEMO
  internal static List<CARGO_D_USAGE> Usage = new List<CARGO_D_USAGE>();
  internal static T Read<T>(Func<CARGO_DUseState,T> run) { lock(CARGO_D01DemoData.Sync) return run(Load()); }
  private static CARGO_DUseState Load() { return new CARGO_DUseState {Work=CARGO_DWorkflowDemoData.Load(),Usages=CARGO_D01DemoData.Copy(Usage),DriverUsers=new List<CARGO_D_DRIVER_USER> {new CARGO_D_DRIVER_USER {USER_ID="示範申請人",RESOURCE_ID="示範駕駛",ACTIVE=1}}}; }
  internal static void Save(CARGO_D05SaveDto data,CARGO_DUsageActorDto actor) { lock(CARGO_D01DemoData.Sync) {
   var state=Load(); var d=state.Work.Records.FirstOrDefault(x=>x.Application.Request.REQUEST_ID==data.RequestId);
   if(d==null) throw new InvalidOperationException("查無派車單");
   var saved=CARGO_D05Rules.Validate(d.Application.Request,state.Usages.Where(x=>x.REQUEST_ID==data.RequestId).ToList(),state.Work.Resources,data,actor);
   Record(d,saved); CARGO_D01DemoData.Records[data.RequestId]=d; Usage.Add(saved);
  } }
#else
  internal static T Read<T>(Func<CARGO_DUseState,T> run) { using(var db=CARGO_DDatabase.Open()) using(var tx=db.BeginTransaction()) { var result=run(Load(db,tx));tx.Commit();return result; } }
  private static CARGO_DUseState Load(IDbConnection db,IDbTransaction tx) { return new CARGO_DUseState {
   Work=CARGO_DWorkflowService.Load(db,tx),Usages=db.Query<CARGO_D_USAGE>("SELECT * FROM CARGO_D_USAGE ORDER BY REVISION_NO",transaction:tx).ToList(),
   DriverUsers=db.Query<CARGO_D_DRIVER_USER>("SELECT * FROM CARGO_D_DRIVER_USER WHERE ACTIVE=1",transaction:tx).ToList() }; }
  internal static void Save(CARGO_D05SaveDto data,CARGO_DUsageActorDto actor) {
   CARGO_D05Rules.Demand(actor,true);
   using(var db=CARGO_DDatabase.Open()) using(var tx=db.BeginTransaction()) {
    var r=db.Query<CARGO_D_REQUEST>("SELECT * FROM CARGO_D_REQUEST WHERE REQUEST_ID=:Id",CARGO_DDatabase.Params(new {Id=data.RequestId}),tx).SingleOrDefault();
    if(r==null) throw new InvalidOperationException("查無派車單");
    var history=db.Query<CARGO_D_USAGE>("SELECT * FROM CARGO_D_USAGE WHERE REQUEST_ID=:Id",CARGO_DDatabase.Params(new {Id=data.RequestId}),tx).ToList();
    var resources=db.Query<CARGO_D_RESOURCE>("SELECT * FROM CARGO_D_RESOURCE WHERE POOL='BIZ'",transaction:tx).ToList();
    var x=CARGO_D05Rules.Validate(r,history,resources,data,actor);
    if(db.Execute("UPDATE CARGO_D_REQUEST SET USAGE_AT=:At, UPDATED_AT=:At, VERSION_NO=VERSION_NO+1 WHERE REQUEST_ID=:Id AND VERSION_NO=:Version",CARGO_DDatabase.Params(new {At=x.RECORDED_AT,Id=data.RequestId,data.Version}),tx)!=1) throw new InvalidOperationException("資料已更新，請重新開啟後操作");
    db.Execute(@"INSERT INTO CARGO_D_USAGE (REQUEST_ID,REVISION_NO,VEHICLE_TYPE,VEHICLE,DRIVER1,DRIVER2,START_KM,END_KM,DISTANCE_KM,RECORDED_BY,RECORDED_AT)
      VALUES (:REQUEST_ID,:REVISION_NO,:VEHICLE_TYPE,:VEHICLE,:DRIVER1,:DRIVER2,:START_KM,:END_KM,:DISTANCE_KM,:RECORDED_BY,:RECORDED_AT)",CARGO_DDatabase.Params(x),tx);
    db.Execute(@"INSERT INTO CARGO_D_LOG (LOG_ID,REQUEST_ID,ACTION,ACTOR,NOTE,CREATED_AT) VALUES (CARGO_D_LOG_SEQ.NEXTVAL,:Id,:Action,:Actor,:Note,:At)",CARGO_DDatabase.Params(new {Id=data.RequestId,Action=x.REVISION_NO==1?"實登":"修改實登",Actor=actor.UserId,Note=Summary(x),At=x.RECORDED_AT}),tx);
    tx.Commit();
   }
  }
#endif
  private static string Summary(CARGO_D_USAGE x) {return x.VEHICLE+"／"+x.DRIVER1+" "+x.DRIVER2+"；"+x.START_KM+" → "+x.END_KM+" km";}
  private static void Record(CARGO_D01DetailDto d,CARGO_D_USAGE x) {var r=d.Application.Request;r.USAGE_AT=r.UPDATED_AT=x.RECORDED_AT;r.VERSION_NO++;CARGO_DWorkflowRules.Log(d,x.REVISION_NO==1?"實登":"修改實登",x.RECORDED_BY,Summary(x));}
 }
 public sealed class CARGO_D05Service {
  public CARGO_D05PageDto Query(CARGO_D05QueryDto q,CARGO_DUsageActorDto actor) {
   CARGO_D05Rules.Demand(actor,true);
   return CARGO_DUseData.Read(state=>{
    var rows=state.Work.Records.Select(d=>new CARGO_D05RowDto {Request=d.Application.Request,Usage=state.Usages.Where(x=>x.REQUEST_ID==d.Application.Request.REQUEST_ID).OrderByDescending(x=>x.REVISION_NO).FirstOrDefault(),
     PlannedVehicleType=state.Work.Resources.Where(x=>x.RESOURCE_ID==d.Application.Request.VEHICLE).Select(x=>x.VEHICLE_TYPE).FirstOrDefault()})
     .Where(x=>!String.IsNullOrEmpty(x.Request.DISPATCH_NO)&&CARGO_D05Rules.Eligible(x.Request,x.Usage!=null)).ToList();
    var pending=rows.Count(x=>x.Usage==null);var kw=(q.Keyword??"").Trim();
    var filtered=rows.Where(x=>(kw.Length==0 || new[]{x.Request.DISPATCH_NO,x.Request.REQUEST_NO,x.Request.APPLICANT,x.Request.DEPT,x.Request.VEHICLE,x.Usage==null?null:x.Usage.VEHICLE}.Any(t=>(t??"").IndexOf(kw,StringComparison.OrdinalIgnoreCase)>=0)) &&
     (String.IsNullOrEmpty(q.Status)||(q.Status=="done" && x.Usage!=null)||(q.Status=="todo"&&x.Usage==null)))
     .OrderBy(x=>x.Usage!=null).ThenByDescending(x=>x.Request.START_AT).ThenBy(x=>x.Request.DISPATCH_NO).ToList();
    var size=Math.Max(1,Math.Min(100,q.PageSize));return new CARGO_D05PageDto {Total=filtered.Count,Pending=pending,Items=filtered.Skip((Math.Max(1,Math.Min(1000000,q.Page))-1)*size).Take(size).ToList()};
   });
  }
  public CARGO_D05DetailDto Detail(long id,CARGO_DUsageActorDto actor) {
   CARGO_D05Rules.Demand(actor,true);
   return CARGO_DUseData.Read(state=>{var d=state.Work.Records.FirstOrDefault(x=>x.Application.Request.REQUEST_ID==id);var h=state.Usages.Where(x=>x.REQUEST_ID==id).OrderByDescending(x=>x.REVISION_NO).ToList();
    if(d==null || String.IsNullOrEmpty(d.Application.Request.DISPATCH_NO) || !CARGO_D05Rules.Eligible(d.Application.Request,h.Count>0)) return null;
    return new CARGO_D05DetailDto {Application=d,History=h,Resources=state.Work.Resources.Where(x=>x.POOL=="BIZ").ToList()};});
  }
  public void Save(CARGO_D05SaveDto data,CARGO_DUsageActorDto actor) {CARGO_D05Rules.Demand(actor,true);CARGO_DUseData.Save(data,actor);}
 }
}
