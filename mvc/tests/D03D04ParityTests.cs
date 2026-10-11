using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.Models;
internal static class D03D04ParityTests {
 static int count;
 static readonly CARGO_DStaffDto Actor = new CARGO_DStaffDto { UserId="audit", CanDispatch=true, CanSign=true };
 static void Check(bool ok,string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
 static void Fails(Action action,string name) { try { action(); } catch (InvalidOperationException) { Check(true,name); return; } throw new Exception("Expected failure: " + name); }
 static CARGO_D01DetailDto New(long id) {
  var d=CARGO_D01DemoData.NewDetail(new CARGO_D01SaveDto { Request=new CARGO_D_REQUEST { REQUEST_ID=id,REQUEST_NO="TEST"+id, VERSION_NO=1, STATUS="dispatched", SIGN_STATUS="approved", SELF_DRIVE=1,CONTINUE_MATCHING=1,OUTCOME="selfDrive",START_AT=new DateTime(2030,1,2,9,0,0),END_AT=new DateTime(2030,1,2,17,0,0),VEHICLE="測試車 C",PAX=2 }});
  d.Assignments.Add(new CARGO_D_ASSIGNMENT {LINE_NO=1,FROM_AT=d.Application.Request.START_AT.Value,TO_AT=d.Application.Request.END_AT.Value,VEHICLE="測試車 C"}); return d;
 }
 static CARGO_DWorkActionDto Reassign(CARGO_D01DetailDto d,string vehicle,string driver1,string driver2=null,bool supplement=false) {
  return new CARGO_DWorkActionDto {RequestId=d.Application.Request.REQUEST_ID,Version=d.Application.Request.VERSION_NO,Command="reassign",Vehicle=vehicle,Driver1=driver1,Driver2=driver2,SupplementDriver=supplement,EffectiveAt=d.Application.Request.START_AT.Value.AddHours(1)};
 }
 public static void Main() {
  var s=CARGO_DWorkflowDemoData.Load();s.Records.Clear();s.Signs.Clear();s.Blocks.Clear();var d=New(900);s.Records.Add(d);
  Fails(()=>CARGO_DWorkflowRules.Apply(s,d,Reassign(d,"測試車 D",null,null,true),Actor,3),"supplement cannot change only vehicle without driver");
  Check(d.Assignments.Count==1 && d.Application.Request.VERSION_NO==1,"failed supplement keeps state unchanged");
  CARGO_DWorkflowRules.Apply(s,d,Reassign(d,"測試車 D","示範駕駛",null,true),Actor,3);
  Check(d.Notices.Last().KIND=="換車＋補派司機（待寄送）" && d.Notices.Last().MESSAGE.Contains("示範駕駛"),"supplement queues classified notice without SMTP");
  Check(d.Assignments.Last().KIND=="換車＋補派司機" && d.History.Last().ACTION=="換車＋補派司機","supplement allows simultaneous vehicle change and records exact kind");
  Fails(()=>CARGO_DWorkflowRules.Apply(s,d,Reassign(d,"測試車 D","示範駕駛 B",null,true),Actor,3),"supplement rejects case that already has driver");
  CARGO_DWorkflowRules.Apply(s,d,Reassign(d,"測試車 D","示範駕駛","示範駕駛 B"),Actor,3);
  Check(d.Notices.Last().KIND=="加派司機（待寄送）","second driver queues add-driver notice");
  Check(d.Assignments.Last().KIND=="加派司機" && d.History.Last().ACTION=="加派司機","adding second driver classified as add");
  CARGO_DWorkflowRules.Apply(s,d,Reassign(d,"測試車 D","示範駕駛 B","示範駕駛"),Actor,3);
  Check(d.Notices.Last().KIND=="換司機（待寄送）","changed driver queues driver-change notice");
  Check(d.Assignments.Last().KIND=="換司機","driver position swap keeps existing mutation behavior");
  CARGO_DWorkflowRules.Apply(s,d,Reassign(d,"測試車 C","示範駕駛 B","示範駕駛"),Actor,3);
  Check(d.Notices.Last().KIND=="換車（待寄送）","changed vehicle queues vehicle-change notice");
  Check(d.Assignments.Last().KIND=="換車","vehicle-only reassignment classified as vehicle");
  d=New(901);d.Application.Request.CONTINUE_MATCHING=0;s.Records.Clear();s.Records.Add(d);
  Fails(()=>CARGO_DWorkflowRules.Apply(s,d,Reassign(d,"測試車 C","示範駕駛",null,true),Actor,3),"supplement enforces continued matching consent");
  var r=d.Application.Request;r.SELF_DRIVE=1;r.HAZARDOUS_CARGO=1;r.NEED_K=1;r.NEED_P=1;r.NIGHT_OT=1;r.HOLIDAY_OT=1;r.ENTER_TAIPEI=1;
  var row=CARGO_D03Mapper.Row(r);Check(row.SelfDrive && row.HazardousCargo && row.Requirements=="K 證、P 證、進台北市、夜間加班、假日加班","D03 list exposes all audited request flags");
  var records=CARGO_D01DemoData.Records.ToDictionary(x=>x.Key,x=>x.Value);var signs=CARGO_DWorkflowDemoData.Signs;
  try {
   CARGO_D01DemoData.Records.Clear();CARGO_DWorkflowDemoData.Signs=new List<CARGO_D_SIGN>();
   for(int i=1;i<=3;i++) {
    d=New(i);r=d.Application.Request;r.CREATED_AT=new DateTime(2020+i,1,1);r.SIGN_STATUS=i==3?"rejected":"pending";if(i==3){r.STATUS="noVehicle";r.SIGN_STATUS=null;}
    CARGO_D01DemoData.Records[i]=d;
    CARGO_DWorkflowDemoData.Signs.Add(new CARGO_D_SIGN {REQUEST_ID=i,ROUND_NO=1,STATUS=i==3?"rejected":"pending",SUBMITTED_AT=new DateTime(2030,1,1).AddHours(4-i),SUBMITTED_BY="dispatcher",SUMMARY="送簽快照"+i,DECIDED_BY=i==3?"supervisor":null,DECIDED_AT=i==3?(DateTime?)new DateTime(2030,1,2):null});
   }
   // The newest round, not an older submit time/status, must control row ordering and projection.
   CARGO_DWorkflowDemoData.Signs.Add(new CARGO_D_SIGN {REQUEST_ID=2,ROUND_NO=2,STATUS="pending",SUBMITTED_AT=new DateTime(2030,1,5),SUBMITTED_BY="resubmitter",SUMMARY="第二輪"});
   var service=new CARGO_D04Service();var page=service.Query(new CARGO_DWorkQueryDto{Page=1,PageSize=100},Actor);
   Check(page.Items.Select(x=>x.REQUEST_ID).SequenceEqual(new long[]{2,1,3}),"D04 pending first then newest latest-round submit irrespective of creation");
   var rejected=CARGO_D04Mapper.Row(page.Items.Last(),page.SignInfo[3]);
   Check(rejected.SignStatus=="已退回" && rejected.SignSummary=="送簽快照3","no-vehicle after rejection preserves sign status and snapshot");
   Check(rejected.SubmittedBy=="dispatcher" && rejected.DecidedBy=="supervisor" && rejected.DecidedAt.HasValue && rejected.Round==1,"D04 row exposes sign actors dates and round");
   var resent=CARGO_D04Mapper.Row(page.Items.First(),page.SignInfo[2]);Check(resent.Round==2 && resent.SubmittedBy=="resubmitter" && resent.SignSummary=="第二輪","D04 projects latest round metadata");
   page=service.Query(new CARGO_DWorkQueryDto{Page=1,PageSize=100,Status="rejected"},Actor);Check(page.Total==1 && page.Items.Single().REQUEST_ID==3,"rejected filter agrees with displayed latest status after no-vehicle");
  } finally {CARGO_D01DemoData.Records.Clear();foreach(var entry in records)CARGO_D01DemoData.Records.Add(entry.Key,entry.Value);CARGO_DWorkflowDemoData.Signs=signs;}
  Console.WriteLine("Passed "+count+" D03/D04 parity checks.");
 }
}
