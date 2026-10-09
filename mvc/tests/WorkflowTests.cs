using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
internal static class WorkflowTests {
 static int count;
 static readonly CARGO_DStaffDto Actor = new CARGO_DStaffDto { UserId="示範申請人", CanApprove=true, CanDispatch=true, CanSign=true };
 static void Check(bool ok,string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
 static void Fails(Action action,string name) { try { action(); } catch (InvalidOperationException) { Check(true,name); return; } catch (UnauthorizedAccessException) { Check(true,name); return; } throw new Exception("Expected failure: " + name); }
 static CARGO_DWorkActionDto ActionFor(CARGO_D01DetailDto d,string cmd) { return new CARGO_DWorkActionDto { RequestId=d.Application.Request.REQUEST_ID, Version=d.Application.Request.VERSION_NO, Command=cmd }; }
 static void Apply(CARGO_DWorkState s,CARGO_D01DetailDto d,string cmd,int module,string note="test") { var a=ActionFor(d,cmd); a.Note=note; CARGO_DWorkflowRules.Apply(s,d,a,Actor,module); }
 static CARGO_D01DetailDto Record(CARGO_DWorkState s,long id) { return s.Records.Single(x => x.Application.Request.REQUEST_ID==id); }
 static void Main() {
  var d02=new CARGO_D02Service(); var d03=new CARGO_D03Service(); var d04=new CARGO_D04Service();
  var q=new CARGO_DWorkQueryDto { Page=1,PageSize=100 };
  Check(d02.Query(q,Actor).Total==6,"supervisor scope excludes drafts");
  Check(d02.Query(q,new CARGO_DStaffDto {UserId="unmapped",CanApprove=true}).Total==0,"unmapped supervisor sees nothing");
  Fails(()=>d03.Query(q,new CARGO_DStaffDto {UserId="user"}),"role enforced in service");
  Fails(()=>d02.Change(new List<CARGO_DWorkActionDto> {new CARGO_DWorkActionDto {RequestId=2,Version=1,Command="approve"},new CARGO_DWorkActionDto {RequestId=3,Version=1,Command="approve"}},Actor),"batch rejects invalid member");
  Check(d02.Detail(2,Actor).Detail.Application.Request.STATUS=="submitted","failed batch atomic rollback");
  var s=CARGO_DWorkflowDemoData.Load(); var d=Record(s,2); var r=d.Application.Request;
  Fails(()=>Apply(s,d,"reject",2,""),"rejection note required");
  Apply(s,d,"approve",2); Check(r.STATUS=="approved" && r.VERSION_NO==2,"unit approval opens dispatch");
  var a=ActionFor(d,"dispatch"); a.Vehicle="測試車 A"; a.Driver1="示範駕駛 C";
  Fails(()=>CARGO_DWorkflowRules.Apply(s,d,a,Actor,3),"existing vehicle occupancy blocks dispatch");
  a.Vehicle="測試車 C"; a.Driver1=null;
  Fails(()=>CARGO_DWorkflowRules.Apply(s,d,a,Actor,3),"available driver cannot be omitted");
  a.Driver1="示範駕駛 C"; a.Driver2=a.Driver1;
  Fails(()=>CARGO_DWorkflowRules.Apply(s,d,a,Actor,3),"duplicate drivers rejected");
  a.Driver2=null; CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  Check(r.SIGN_STATUS=="pending" && d.Assignments.Count==1 && d.Notices.Count==0,"dispatch reserves before sign and does not notify");
  Check(CARGO_DWorkflowRules.Busy(s,"測試車 C",r.START_AT.Value,r.END_AT.Value,4)!=null,"pending sign occupies resources");
  Fails(()=>CARGO_DWorkflowRules.Apply(s,d,a,Actor,3),"stale version rejected");
  Apply(s,d,"reject",4);
  Check(r.STATUS=="approved" && d.Assignments.Count==0 && r.VEHICLE==null,"sign rejection releases resources");
  Check(s.Signs.Any(x=>x.REQUEST_ID==2 && x.STATUS=="rejected"),"rejected sign round retained");
  a=ActionFor(d,"dispatch");a.Vehicle="測試車 C";a.Driver1="示範駕駛 C";CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);Apply(s,d,"approve",4);
  Check(r.SIGN_STATUS=="approved" && d.Notices.Count==1 && !d.Notices[0].SENT_AT.HasValue,"approved sign queues unsent notice");
  Check(s.Signs.Count(x=>x.REQUEST_ID==2)==2,"redispatch opens new sign round");
  a=ActionFor(d,"reassign");a.Vehicle="測試車 D";a.Driver1="示範駕駛 C";a.EffectiveAt=r.START_AT.Value.AddHours(2);CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  Check(d.Assignments.Count==2 && d.Assignments[0].TO_AT==a.EffectiveAt && r.VEHICLE=="測試車 D","reassignment splits interval");
  s.Blocks.Add(new CARGO_D_RESOURCE_BLOCK {RESOURCE_ID="測試車 D",FROM_AT=r.END_AT.Value,TO_AT=r.END_AT.Value.AddHours(2),SOURCE="MAINTENANCE",REASON="test"});
  a=ActionFor(d,"extend");a.EndAt=r.END_AT.Value.AddHours(1);
  Fails(()=>CARGO_DWorkflowRules.Apply(s,d,a,Actor,3),"maintenance blocks extension");s.Blocks.Clear();
  r.PENDING_RETURN_AT=r.START_AT.Value.AddHours(3);CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  Check(r.END_AT==a.EndAt && !r.PENDING_RETURN_AT.HasValue,"extension updates interval and cancels pending return");
  r.PENDING_RETURN_AT=r.START_AT.Value.AddHours(1);Apply(s,d,"returnApprove",3);
  Check(d.Assignments.Count==1 && r.VEHICLE=="測試車 C" && r.END_AT==r.START_AT.Value.AddHours(1),"early return removes future assignment and restores correct resources");
  Check(CARGO_DWorkflowRules.Busy(s,"測試車 C",r.END_AT.Value,r.END_AT.Value.AddHours(1),4)==null,"adjacent intervals do not overlap");
  s=CARGO_DWorkflowDemoData.Load();d=Record(s,4);r=d.Application.Request;
  r.PAX=6;Check(CARGO_DWorkflowRules.Candidates(s,r,r.START_AT.Value,r.END_AT.Value).Where(x=>x.Resource.KIND=="VEHICLE").All(x=>x.UnavailableReason!=null),"capacity rule");r.PAX=2;
  r.NEED_K=r.NEED_P=1;s.Resources.Single(x=>x.RESOURCE_ID=="測試車 C").PERMIT_P=0;
  Check(CARGO_DWorkflowRules.Candidates(s,r,r.START_AT.Value,r.END_AT.Value).Single(x=>x.Resource.RESOURCE_ID=="測試車 C").UnavailableReason!=null,"K and P intersection rule");r.NEED_K=r.NEED_P=0;
  s.Blocks.Add(new CARGO_D_RESOURCE_BLOCK {RESOURCE_ID="示範駕駛 C",FROM_AT=r.START_AT.Value,TO_AT=r.END_AT.Value,SOURCE="LEAVE",REASON="leave"});
  r.SELF_DRIVE=1;r.CONTINUE_MATCHING=1;a=ActionFor(d,"dispatch");a.Vehicle="測試車 C";CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  Check(r.OUTCOME=="selfDrive" && r.SIGN_STATUS=="pending","no free driver permits opted-in self-drive");Apply(s,d,"approve",4);
  CARGO_D01DemoData.Records[4]=d;CARGO_DWorkflowDemoData.Signs=s.Signs;
  var report=d03.Backfill(Actor);Check(report.Any(x=>x.Contains("已補派")) && d03.Detail(4,Actor).Detail.Application.Request.OUTCOME=="withDriver","backfill uses newly free driver");
  s=CARGO_DWorkflowDemoData.Load();d=Record(s,2);Apply(s,d,"approve",2);Apply(s,d,"noVehicle",3);
  Check(d.Application.Request.STATUS=="noVehicle" && d.Assignments.Count==0 && d.Application.Request.SIGN_STATUS==null,"no vehicle closes without sign or occupancy");
  Console.WriteLine("PASS " + count + " workflow checks");
 }
}
