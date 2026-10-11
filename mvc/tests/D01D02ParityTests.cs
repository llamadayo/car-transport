using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.Models;
internal static class D01D02ParityTests {
    static int passed;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    public static void Main() {
        var actor = new CARGO_DActorDto { UserId = "示範申請人" };
        var reviewer = new CARGO_DStaffDto { UserId = actor.UserId, CanApprove = true };
        var r = new CARGO_D_REQUEST { REQUEST_ID=9001, REQUEST_NO="PARITY9001", OWNER_ID=actor.UserId, APPLICANT=actor.UserId,
            CATEGORY="general", STATUS="submitted", APPROVER_NAME="前次審核主管", START_AT=DateTime.Today.AddDays(1), END_AT=DateTime.Today.AddDays(1).AddHours(3), SELF_DRIVE=0,
            CREATED_AT=DateTime.Now, HAZARDOUS_CARGO=1, SIGN_STATUS="rejected", PENDING_RETURN_AT=DateTime.Today.AddDays(1).AddHours(2), RETURN_REQUESTED_AT=DateTime.Now };
        var source = CARGO_D01DemoData.NewDetail(new CARGO_D01SaveDto { Request=r });
        source.Assignments.Add(new CARGO_D_ASSIGNMENT { FROM_AT=r.START_AT.Value, TO_AT=r.START_AT.Value, VEHICLE="測試車 A", ACTOR="調度甲", KIND="原始派車" });
        CARGO_D01DemoData.Records.Add(r.REQUEST_ID,source);
        try {
            var svc = new CARGO_D01Service();
            var app = svc.Detail(r.REQUEST_ID,actor);
            Check(app.ReviewApprover==actor.UserId && CARGO_D01Mapper.Detail(app).Approver==actor.UserId,"D01 pending detail resolves current supervisor instead of stale actual reviewer");
            Check(r.APPROVER_NAME=="前次審核主管", "detail lookup does not mutate recorded reviewer");
            var work = new CARGO_D02Service().Detail(r.REQUEST_ID,reviewer);
            Check(CARGO_D02Mapper.Detail(work).Application.Approver==actor.UserId,"D02 pending detail resolves same supervisor as list");
            foreach(var status in new[] {"approved","rejected"}) {
                r.STATUS=status;
                Check(CARGO_D01Mapper.Detail(svc.Detail(r.REQUEST_ID,actor)).Approver=="前次審核主管", "D01 " + status + " preserves actual reviewer");
                Check(CARGO_D02Mapper.Detail(new CARGO_D02Service().Detail(r.REQUEST_ID,reviewer)).Application.Approver=="前次審核主管", "D02 " + status + " preserves actual reviewer");
            }
            r.STATUS="submitted"; r.OWNER_ID="無主管使用者";
            var missing=svc.Detail(r.REQUEST_ID,new CARGO_DActorDto {UserId=r.OWNER_ID});
            Check(missing.ReviewApprover=="" && CARGO_D01Mapper.Detail(missing).Approver=="未設定", "pending detail without mapping never falls back to stale reviewer");
            r.OWNER_ID=actor.UserId;
            var row=CARGO_D01Mapper.Row(svc.Query(new CARGO_D01QueryDto {Page=1,PageSize=100},actor).Items.Single(x=>x.REQUEST_ID==r.REQUEST_ID));
            Check(row.HazardousCargo && row.Cargo=="0 項／0 件", "D01 shows whole-request hazard even without cargo");
            Check(row.PendingReturn && row.SignStatus=="已退回", "D01 list carries return-pending and supervisor sign result");
            Check(CARGO_D02Mapper.Row(r,new CARGO_D02ListInfoDto()).HazardousCargo,"D02 list carries whole-request hazard");
            r.HAZARDOUS_CARGO=0; source.Application.Cargo.Add(new CARGO_D_CARGO { HAZARDOUS=1, QUANTITY=1 });
            row=CARGO_D01Mapper.Row(svc.Query(new CARGO_D01QueryDto {Page=1,PageSize=100},actor).Items.Single(x=>x.REQUEST_ID==r.REQUEST_ID));
            Check(!row.HazardousCargo && !CARGO_D02Mapper.Row(r,new CARGO_D02ListInfoDto()).HazardousCargo,"legacy cargo flag never overrides whole-request hazard definition");
            app=svc.Detail(r.REQUEST_ID,actor); var mapped=CARGO_D01Mapper.Detail(app);
            Check(mapped.ReturnRequestedAt==r.RETURN_REQUESTED_AT && mapped.PendingReturnAt==r.PENDING_RETURN_AT,"return request time and requested end remain distinct");
            Check(mapped.Assignments[0].Actor=="調度甲" && mapped.Assignments[0].IsSelfDrive && mapped.Assignments[0].IsEmpty,"history exposes actor self-drive and zero-length nonoccupancy");
            Check(source.Assignments[0].FROM_AT==source.Assignments[0].TO_AT && source.Assignments[0].DRIVER1==null,"history display does not change original interval or driver");
            source.Assignments[0].DRIVER1="示範駕駛"; source.Assignments[0].TO_AT=r.END_AT.Value;
            mapped=CARGO_D01Mapper.Detail(svc.Detail(r.REQUEST_ID,actor));
            Check(!mapped.Assignments[0].IsSelfDrive && !mapped.Assignments[0].IsEmpty,"normal staffed intervals retain ordinary display");
        } finally { CARGO_D01DemoData.Records.Remove(r.REQUEST_ID); }
        Console.WriteLine("D01/D02 parity: " + passed + " passed");
    }
}
