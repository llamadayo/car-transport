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
 static CARGO_D01DetailDto Dispatchable(long id,DateTime start,DateTime end) {
  return CARGO_D01DemoData.NewDetail(new CARGO_D01SaveDto { Request=new CARGO_D_REQUEST {
   REQUEST_ID=id, REQUEST_NO="REG"+id, VERSION_NO=1, STATUS="approved", START_AT=start, END_AT=end, PAX=2
  } });
 }
 static void QueryWork(CARGO_D03Service service) {
  var q=new CARGO_DWorkQueryDto {Status="work",Page=1,PageSize=100};
  Check(service.Query(q,Actor).Items.Select(x=>x.REQUEST_ID).SequenceEqual(new long[] {4}),"work filter starts with approved requests only");
  var original=CARGO_D01DemoData.Records[5]; var pending=CARGO_D01DemoData.Copy(original);
  pending.Application.Request.PENDING_RETURN_AT=pending.Application.Request.START_AT.Value.AddHours(1);
  pending.Application.Request.CATEGORY="routine";pending.Application.Request.CREATED_AT=DateTime.Now.AddYears(-1);
  CARGO_D01DemoData.Records[5]=pending;
  try {
   Check(service.Query(q,Actor).Items.Select(x=>x.REQUEST_ID).SequenceEqual(new long[] {5,4}),"work filter includes pending returns and preserves routine priority");
   q.Status="";Check(service.Query(q,Actor).Total==4,"all statuses still includes sign pending and no vehicle records");
   q.Status="todo";Check(service.Query(q,Actor).Items.Select(x=>x.REQUEST_ID).SequenceEqual(new long[] {4}),"todo filter excludes pending return on dispatched request");
   q.Status="ready";Check(service.Query(q,Actor).Items.Select(x=>x.REQUEST_ID).SequenceEqual(new long[] {5}),"flow status still includes matching pending return");
   q.Status="work";q.UseDate=DateTime.Today;Check(service.Query(q,Actor).Total==0,"work filter keeps use date criteria");
   q.UseDate=null;q.Applicant="unknown";Check(service.Query(q,Actor).Total==0,"work filter keeps applicant criteria");
  } finally { CARGO_D01DemoData.Records[5]=original; }
 }
 static void DefaultEffectiveTime(CARGO_D03Service service) {
  var d=Record(CARGO_DWorkflowDemoData.Load(),5);var r=d.Application.Request;
  var now=new DateTime(2030,1,2,12,0,0);r.START_AT=now.AddHours(-4);r.END_AT=now.AddHours(4);
  d.Assignments.Clear();
  d.Assignments.Add(new CARGO_D_ASSIGNMENT {LINE_NO=1,FROM_AT=r.START_AT.Value,TO_AT=now.AddHours(-1)});
  d.Assignments.Add(new CARGO_D_ASSIGNMENT {LINE_NO=2,FROM_AT=now.AddHours(-1),TO_AT=r.END_AT.Value});
  Check(CARGO_DWorkflowRules.DefaultEffectiveAt(d,now.AddHours(-3))==now.AddHours(-1),"reassignment default respects latest segment start");
  Check(CARGO_DWorkflowRules.DefaultEffectiveAt(d,now)==now,"reassignment default advances to current time");
  Check(CARGO_DWorkflowRules.DefaultEffectiveAt(d,r.END_AT.Value)==r.END_AT.Value.AddMinutes(-1),"reassignment default caps at end minus one minute");
  Check(service.Detail(5,Actor).EffectiveAt==CARGO_D01DemoData.Records[5].Application.Request.START_AT,"future detail defaults to current segment start");
 }
 static void EmptyAssignmentOccupancy() {
  var s=CARGO_DWorkflowDemoData.Load();s.Records.Clear();s.Signs.Clear();
  var start=new DateTime(2030,1,2,10,0,0);var end=start.AddHours(6);
  var d=Dispatchable(101,start,end);s.Records.Add(d);
  var a=ActionFor(d,"dispatch");a.Vehicle="測試車 C";a.Driver1="示範駕駛 C";CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);Apply(s,d,"approve",4);
  a=ActionFor(d,"reassign");a.Vehicle="測試車 D";a.Driver1="示範駕駛 B";a.EffectiveAt=start;CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  Check(d.Assignments[0].FROM_AT==d.Assignments[0].TO_AT,"reassignment at original start retains empty history segment");
  var other=Dispatchable(102,start.AddHours(-1),start.AddHours(1));s.Records.Add(other);
  a=ActionFor(other,"dispatch");a.Vehicle="測試車 C";a.Driver1="示範駕駛 C";CARGO_DWorkflowRules.Apply(s,other,a,Actor,3);
  Check(other.Application.Request.STATUS=="dispatched","empty history releases original vehicle and driver for a crossing request");
  Check(CARGO_DWorkflowRules.Busy(s,"測試車 D",start.AddHours(-1),start.AddHours(1),102)!=null &&
   CARGO_DWorkflowRules.Busy(s,"示範駕駛 B",start.AddHours(-1),start.AddHours(1),102)!=null,"replacement vehicle and driver remain occupied");
  a=ActionFor(d,"reassign");a.Vehicle="測試車 A";a.Driver1="示範駕駛";a.EffectiveAt=start.AddHours(2);CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  a=ActionFor(d,"reassign");a.Vehicle="測試車 B";a.Driver1="示範駕駛 B";a.EffectiveAt=start.AddHours(2);CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  var later=Dispatchable(103,start.AddHours(1),start.AddHours(3));s.Records.Add(later);
  a=ActionFor(later,"dispatch");a.Vehicle="測試車 A";a.Driver1="示範駕駛";CARGO_DWorkflowRules.Apply(s,later,a,Actor,3);
  Check(later.Application.Request.STATUS=="dispatched","empty segment at a later reassignment releases vehicle and driver");
  Check(CARGO_DWorkflowRules.Busy(s,"測試車 D",start.AddHours(1),start.AddHours(3),103)!=null,"nonempty earlier segment continues to block overlap");
  Check(CARGO_DWorkflowRules.Busy(s,"測試車 D",start.AddHours(3),start.AddHours(3),103)==null,"empty queried interval reserves no resource");
 }
 static void EffectiveTimeRoundTrip() {
  var s=CARGO_DWorkflowDemoData.Load();s.Records.Clear();s.Signs.Clear();
  var start=new DateTime(2030,1,2,10,0,0);var end=start.AddHours(6);
  var d=Dispatchable(104,start,end);s.Records.Add(d);
  var a=ActionFor(d,"dispatch");a.Vehicle="測試車 C";a.Driver1="示範駕駛 C";CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);Apply(s,d,"approve",4);
  var segmentStart=start.AddHours(1).AddSeconds(42).AddTicks(1234567);
  a=ActionFor(d,"reassign");a.Vehicle="測試車 D";a.Driver1="示範駕駛 C";a.EffectiveAt=segmentStart;CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  var effective=CARGO_DWorkflowRules.DefaultEffectiveAt(d,segmentStart);
  var posted=DateTime.ParseExact(effective.Value.ToString("yyyy-MM-ddTHH:mm"),"yyyy-MM-ddTHH:mm",System.Globalization.CultureInfo.InvariantCulture);
  Check(posted>=segmentStart,"default time field round trip cannot move before fractional segment start");
  a=ActionFor(d,"reassign");a.Vehicle="測試車 A";a.Driver1="示範駕駛 C";a.EffectiveAt=posted;CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  Check(d.Application.Request.VEHICLE=="測試車 A","default field permits reassignment immediately after backfill");
 }
 static void ShortEffectiveTimeRoundTrip() {
  var s=CARGO_DWorkflowDemoData.Load();s.Records.Clear();s.Signs.Clear();
  var start=new DateTime(2030,1,2,10,0,0);var end=start.AddHours(6);
  var d=Dispatchable(105,start,end);s.Records.Add(d);
  var a=ActionFor(d,"dispatch");a.Vehicle="測試車 C";a.Driver1="示範駕駛 C";CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);Apply(s,d,"approve",4);
  var segmentStart=end.AddSeconds(-18).AddTicks(1234567);
  a=ActionFor(d,"reassign");a.Vehicle="測試車 D";a.Driver1="示範駕駛 C";a.EffectiveAt=segmentStart;CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  var effective=CARGO_DWorkflowRules.DefaultEffectiveAt(d,end);
  Check(effective.HasValue && effective.Value.Ticks%TimeSpan.TicksPerMillisecond==0 && effective>=segmentStart && effective<end,"short final segment defaults to a valid HTML millisecond");
  var posted=DateTime.ParseExact(effective.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff"),"yyyy-MM-ddTHH:mm:ss.fff",System.Globalization.CultureInfo.InvariantCulture);
  a=ActionFor(d,"reassign");a.Vehicle="測試車 A";a.Driver1="示範駕駛 C";a.EffectiveAt=posted;CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  Check(d.Application.Request.VEHICLE=="測試車 A" && d.Assignments.Last().FROM_AT==posted,"short final segment precision round trip permits actual reassignment");
  d.Application.Request.END_AT=end.AddMinutes(1).AddSeconds(40);d.Assignments.Last().FROM_AT=end.AddSeconds(42);d.Assignments.Last().TO_AT=d.Application.Request.END_AT.Value;
  effective=CARGO_DWorkflowRules.DefaultEffectiveAt(d,d.Application.Request.END_AT.Value);
  Check(effective==end.AddMinutes(1),"default stays in minutes when a valid minute remains after margin clamp");
  a=ActionFor(d,"reassign");a.Vehicle="測試車 D";a.Driver1="示範駕駛 C";a.EffectiveAt=effective;CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  Check(d.Assignments.Last().FROM_AT==effective,"minute default remains valid when end contains seconds");
  d.Application.Request.END_AT=end.AddSeconds(40);segmentStart=end.AddSeconds(35).AddTicks(1234567);
  d.Assignments.Last().FROM_AT=segmentStart;d.Assignments.Last().TO_AT=d.Application.Request.END_AT.Value;
  effective=CARGO_DWorkflowRules.DefaultEffectiveAt(d,d.Application.Request.END_AT.Value);
  posted=DateTime.ParseExact(effective.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff"),"yyyy-MM-ddTHH:mm:ss.fff",System.Globalization.CultureInfo.InvariantCulture);
  a=ActionFor(d,"reassign");a.Vehicle="測試車 A";a.Driver1="示範駕駛 C";a.EffectiveAt=posted;CARGO_DWorkflowRules.Apply(s,d,a,Actor,3);
  Check(posted>=segmentStart && posted<d.Application.Request.END_AT && d.Assignments.Last().FROM_AT==posted,"millisecond fallback posts a valid default when end contains seconds");
  d.Application.Request.END_AT=end;d.Assignments.Last().FROM_AT=end.AddTicks(-1);d.Assignments.Last().TO_AT=end;
  Check(!CARGO_DWorkflowRules.DefaultEffectiveAt(d,end).HasValue,"submillisecond interval without an HTML value has no default");
 }
 static void Main() {
  var d02=new CARGO_D02Service(); var d03=new CARGO_D03Service(); var d04=new CARGO_D04Service();
  QueryReview(d02,d04);
  QueryWork(d03);DefaultEffectiveTime(d03);EmptyAssignmentOccupancy();EffectiveTimeRoundTrip();ShortEffectiveTimeRoundTrip();
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
 static void QueryReview(CARGO_D02Service d02,CARGO_D04Service d04) {
  var q=new CARGO_DWorkQueryDto {Keyword=" gu000007 ",Page=1,PageSize=20};
  Check(d04.Query(q,Actor).Items.Single().REQUEST_ID==7,"D04 trims case-insensitive request number keyword");
  q.Keyword="gd000005";Check(d04.Query(q,Actor).Items.Single().REQUEST_ID==5,"D04 searches dispatch number");
  q.Keyword="示範申請";Check(d04.Query(q,Actor).Total==2,"D04 retains applicant keyword search");
  q.Keyword="GU000001";Check(d04.Query(q,Actor).Total==0,"D04 keyword cannot expose unsigned requests");
  q.Keyword="GU000007";q.Status="approved";Check(d04.Query(q,Actor).Total==0,"D04 combines keyword with sign status");
  q.Status="pending";q.UseDate=DateTime.Today;Check(d04.Query(q,Actor).Total==0,"D04 combines keyword with date");
  q.UseDate=DateTime.Today.AddDays(1);Check(d04.Query(q,Actor).Total==1,"D04 combined filters find matching request");
  q.Keyword="' OR 1=1 --";Check(d04.Query(q,Actor).Total==0,"D04 treats punctuation as literal keyword");
  var original=CARGO_D01DemoData.Copy(CARGO_D01DemoData.Records[2]);
  try {
   var record=CARGO_D01DemoData.Records[2];record.Application.Request.SELF_DRIVE=1;record.Application.Request.APPROVER_NAME="previous reviewer";
   for(var i=0;i<3;i++) record.Application.Cargo.Add(new CARGO_D_CARGO {QUANTITY=999999999});
   var page=d02.Query(new CARGO_DWorkQueryDto {Status="submitted",Page=1,PageSize=20},Actor);
   Check(page.Items.Single().SELF_DRIVE==1 && page.ReviewInfo[2].CargoCount==3 && page.ReviewInfo[2].CargoQuantity==2999999997L,"D02 includes self drive and large cargo totals");
   Check(page.ReviewInfo[2].Approver==Actor.UserId,"pending review shows active supervisor rather than old reviewer");
   page=d02.Query(new CARGO_DWorkQueryDto {Status="rejected",Page=1,PageSize=20},Actor);
   Check(page.ReviewInfo[3].Approver=="示範主管" && page.ReviewInfo[3].CargoCount==0,"review history keeps deciding supervisor and empty cargo");
   page=d02.Query(new CARGO_DWorkQueryDto {Page=1,PageSize=1},Actor);
   Check(page.ReviewInfo.Count==1 && page.ReviewInfo.ContainsKey(page.Items.Single().REQUEST_ID),"D02 aggregates only the requested page");
   page=d02.Query(new CARGO_DWorkQueryDto {Page=1,PageSize=20},new CARGO_DStaffDto {UserId="unmapped",CanApprove=true});
   Check(page.ReviewInfo.Count==0,"D02 does not return cargo or supervisor data outside scope");
  } finally {CARGO_D01DemoData.Records[2]=original;}
 }
}
