using System;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
class D01DemoTests
{
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); }
    static void Denied(Action action, string label) { try { action(); } catch (InvalidOperationException) { Check(true, label); return; } throw new Exception(label); }
    static void Main()
    {
        var service = new CARGO_D01Service();
        var actor = new CARGO_DActorDto { UserId = "示範申請人", CanChooseRoutine = true };
        Check(service.Query(new CARGO_D01QueryDto { PageSize = 20 }, actor).Total == 7, "seed query");
        Check(service.Detail(1, new CARGO_DActorDto { UserId = "other" }) == null, "owner isolation");
        var copy = service.Detail(1, actor).Application;
        copy.Request.PURPOSE = "changed locally";
        Check(service.Detail(1, actor).Application.Request.PURPOSE != copy.Request.PURPOSE, "detached detail");
        copy.Request.REQUEST_ID = 0;
        long id = service.Save(copy, actor, false);
        var saved = service.Detail(id, actor).Application;
        Check(saved.Request.STATUS == "draft", "create draft");
        service.Save(saved, actor, false);
        Denied(() => service.Save(saved, actor, false), "stale save blocked");
        saved = service.Detail(id, actor).Application;
        service.Save(saved, actor, true);
        Check(service.Detail(id, actor).Application.Request.STATUS == "submitted", "submit");
        Denied(() => service.Save(service.Detail(id, actor).Application, actor, false), "submitted edit blocked");
        service.Withdraw(new CARGO_D01ActionDto { RequestId = id, Version = service.Detail(id, actor).Application.Request.VERSION_NO }, actor);
        Check(service.Detail(id, actor).Application.Request.STATUS == "draft", "withdraw");
        var assigned = service.Detail(5, actor).Application.Request;
        service.RequestEarlyReturn(new CARGO_D01ActionDto { RequestId = 5, Version = assigned.VERSION_NO, ReturnAt = assigned.END_AT.Value.AddHours(-1) }, actor);
        var pending = service.Detail(5, actor).Application.Request;
        Check(pending.END_AT == assigned.END_AT && pending.PENDING_RETURN_AT.HasValue, "return keeps original period");
        Denied(() => service.RequestEarlyReturn(new CARGO_D01ActionDto { RequestId = 5, Version = pending.VERSION_NO, ReturnAt = assigned.END_AT.Value.AddHours(-2) }, actor), "duplicate return blocked");
        service.Save(service.Detail(3, actor).Application, actor, true);
        Check(service.Detail(3, actor).Application.Request.STATUS == "submitted", "rejected resubmit");
        var large = service.Detail(1, actor).Application;
        large.Request.REQUEST_ID = 0;
        for (var i = 0; i < 3; i++) large.Cargo.Add(new CARGO_D_CARGO {
            NAME = "大量貨物", LENGTH_CM = 1, WIDTH_CM = 1, HEIGHT_CM = 1, QUANTITY = 5000, CATEGORY = "BOX"
        });
        var largeId = service.Save(large, actor, false);
        var largeRow = service.Query(new CARGO_D01QueryDto { PageSize = 100 }, actor).Items.Single(x => x.REQUEST_ID == largeId);
        Check(largeRow.CARGO_COUNT == 3 && largeRow.CARGO_QUANTITY == 15000, "three maximum cargo quantities save and sum correctly");
        var invalid = service.Detail(largeId, actor).Application;
        invalid.Cargo[0].QUANTITY = 5001;
        Denied(() => service.Save(invalid, actor, false), "draft quantity above 5000 rejected");
        Denied(() => service.Save(invalid, actor, true), "submit quantity above 5000 rejected");
        Check(service.Detail(largeId, actor).Application.Cargo[0].QUANTITY == 5000, "invalid saves preserve stored quantity");
        var hazardApplication = service.Detail(1, actor).Application;
        hazardApplication.Request.REQUEST_ID = 0;
        hazardApplication.Request.HAZARDOUS_CARGO = 1;
        hazardApplication.Cargo.Clear();
        var hazardId = service.Save(hazardApplication, actor, false);
        var hazardSaved = service.Detail(hazardId, actor).Application;
        Check(hazardSaved.Request.HAZARDOUS_CARGO == 1 && hazardSaved.Cargo.Count == 0, "whole-request hazard saves without cargo");
        hazardSaved.Request.HAZARDOUS_CARGO = 0;
        service.Save(hazardSaved, actor, true);
        Check(service.Detail(hazardId, actor).Application.Request.HAZARDOUS_CARGO == 0, "changing whole-request hazard to no persists on submit");

    }
}
