using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.Models;
using CARGO.Web.ViewModels.CARGO_D01;

internal static class D01Tests
{
    private static int passed;
    private static readonly CARGO_DActorDto Actor = new CARGO_DActorDto { UserId = "test-user" };
    private static readonly List<CARGO_D_OPTION> Options = new List<CARGO_D_OPTION> {
        new CARGO_D_OPTION { OPTION_TYPE = "VEHICLE_TYPE", CODE = "CAR", ACTIVE = 1 },
        new CARGO_D_OPTION { OPTION_TYPE = "VEHICLE_TYPE", CODE = "VAN", ACTIVE = 1 },
        new CARGO_D_OPTION { OPTION_TYPE = "CAMPUS", CODE = "TEST", ACTIVE = 1 },
        new CARGO_D_OPTION { OPTION_TYPE = "CARGO_CATEGORY", CODE = "BOX", ACTIVE = 1 }
    };
    private static CARGO_D01SaveDto Input()
    {
        return new CARGO_D01SaveDto { Request = new CARGO_D_REQUEST {
            CATEGORY = "general", TRANSPORT_TYPE = "一般", APPLICANT = "測試申請人",
            START_AT = new DateTime(2027, 1, 5, 9, 0, 0), END_AT = new DateTime(2027, 1, 5, 12, 0, 0),
            PAX = 1, SELF_DRIVE = 0, PREFERENCE1 = "CAR", REQUEST_REVIEW = 1, STATUS = "draft"
        } };
    }
    private static void Check(string name, Action test)
    { test(); passed++; Console.WriteLine("PASS " + name); }
    private static void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
    private static void Reject(Action test)
    { try { test(); } catch (InvalidOperationException) { return; } throw new Exception("Expected validation rejection"); }
    private static void Validate(CARGO_D01SaveDto d, bool submit = true)
    { CARGO_D01Rules.Validate(d, Actor, Options, submit); }
    public static int Main()
    {
        try {
            Check("valid person-only application", () => Validate(Input()));
            Check("multi-month use stays general", () => { var d = Input(); d.Request.END_AT = d.Request.START_AT.Value.AddMonths(3); Validate(d); Assert(d.Request.CATEGORY == "general"); });
            Check("equal period rejected", () => { var d = Input(); d.Request.END_AT = d.Request.START_AT; Reject(() => Validate(d)); });
            Check("reverse period rejected", () => { var d = Input(); d.Request.END_AT = d.Request.START_AT.Value.AddMinutes(-1); Reject(() => Validate(d)); });
            Check("missing time rejected", () => { var d = Input(); d.Request.START_AT = null; Reject(() => Validate(d)); });
            Check("zero passengers rejected", () => { var d = Input(); d.Request.PAX = 0; Reject(() => Validate(d)); });
            Check("self drive must be selected", () => { var d = Input(); d.Request.SELF_DRIVE = null; Reject(() => Validate(d)); });
            Check("routine role enforced", () => { var d = Input(); d.Request.CATEGORY = "routine"; Reject(() => Validate(d)); CARGO_D01Rules.Validate(d, new CARGO_DActorDto { UserId = "secretary", CanChooseRoutine = true }, Options, true); });
            Check("first preference required even for drafts", () => { var d = Input(); d.Request.PREFERENCE1 = null; Reject(() => Validate(d, false)); });
            Check("optional preferences accepted", () => { var d = Input(); d.Request.PREFERENCE2 = "VAN"; Validate(d); });
            Check("duplicate preference rejected", () => { var d = Input(); d.Request.PREFERENCE2 = "CAR"; Reject(() => Validate(d)); });
            Check("unknown preference rejected", () => { var d = Input(); d.Request.PREFERENCE1 = "BOAT"; Reject(() => Validate(d)); });
            Check("unknown campus rejected", () => { var d = Input(); d.Request.CAMPUS = "unknown"; Reject(() => Validate(d)); });
            Check("eight drivers allowed ninth rejected", () => { var d = Input(); for (int i = 0; i < 8; i++) d.SelfDrivers.Add(new CARGO_D_SELF_DRIVER { NAME = "test" }); Validate(d); d.SelfDrivers.Add(new CARGO_D_SELF_DRIVER()); Reject(() => Validate(d)); });
            Check("blank itinerary rejected", () => { var d = Input(); d.Locations.Add(new CARGO_D_LOCATION { NAME = " " }); Reject(() => Validate(d)); d.Locations.Clear(); Validate(d); });
            Check("exercise field only for general exercise", () => { var d = Input(); d.Request.EXERCISE_PLAN = "plan"; Validate(d); Assert(d.Request.EXERCISE_PLAN == null); d.Request.TRANSPORT_TYPE = "演訓"; d.Request.EXERCISE_PLAN = "plan"; Validate(d); Assert(d.Request.EXERCISE_PLAN == "plan"); });
            Check("submit denied when request review unchecked", () => { var d = Input(); d.Request.REQUEST_REVIEW = 0; Reject(() => Validate(d)); Validate(d, false); });
            Check("cargo independent of passengers and hazard is a hint", () => { var d = Input(); d.Cargo.Add(new CARGO_D_CARGO { NAME = "sample", LENGTH_CM = 10, WIDTH_CM = 10, HEIGHT_CM = 10, QUANTITY = 1, CATEGORY = "BOX", HAZARDOUS = 1 }); Validate(d); d.Cargo[0].QUANTITY = 0; Reject(() => Validate(d)); });
            Check("cargo quantity boundaries apply to drafts and submissions", () => {
                foreach (var submit in new[] { false, true }) {
                    var d = Input();
                    var cargo = new CARGO_D_CARGO { NAME = "sample", LENGTH_CM = 1, WIDTH_CM = 1, HEIGHT_CM = 1, CATEGORY = "BOX" };
                    d.Cargo.Add(cargo);
                    foreach (var quantity in new[] { 1, 5000 }) { cargo.QUANTITY = quantity; Validate(d, submit); }
                    foreach (var quantity in new[] { -1, 0, 5001, Int32.MaxValue }) { cargo.QUANTITY = quantity; Reject(() => Validate(d, submit)); }
                }
            });
            Check("cargo quantity model validation accepts only 1 through 5000", () => {
                var cargo = new CARGO_D01CargoViewModel();
                var context = new System.ComponentModel.DataAnnotations.ValidationContext(cargo) { MemberName = "Quantity" };
                foreach (var quantity in new[] { -1, 0, 1, 5000, 5001, Int32.MaxValue }) {
                    var valid = System.ComponentModel.DataAnnotations.Validator.TryValidateProperty(quantity, context, null);
                    Assert(valid == (quantity == 1 || quantity == 5000));
                }
            });
            Check("no license qualification blocking", () => { var d = Input(); d.Request.SELF_DRIVE = 1; d.Request.QUALIFIED_LICENSE = 0; Validate(d); });
            Check("editable states", () => { var r = Input().Request; foreach (var state in new[] { "draft", "rejected", "submitted", "approved", "noVehicle", "dispatched" }) { r.STATUS = state; Assert(CARGO_D01Rules.CanEdit(r) == (state == "draft" || state == "rejected")); } });
            Check("withdraw only before dispatch confirmation", () => { var r = Input().Request; r.STATUS = "approved"; Assert(CARGO_D01Rules.CanWithdraw(r)); r.DISPATCHED_AT = DateTime.Now; Assert(!CARGO_D01Rules.CanWithdraw(r)); });
            Check("early return requires effective signoff", () => { var r = Input().Request; r.STATUS = "dispatched"; r.SIGN_STATUS = "pending"; Reject(() => CARGO_D01Rules.ValidateReturn(r, r.START_AT.Value.AddHours(1))); r.SIGN_STATUS = "approved"; CARGO_D01Rules.ValidateReturn(r, r.START_AT.Value.AddHours(1)); });
            Check("early return excludes both boundaries", () => { var r = Input().Request; r.STATUS = "dispatched"; r.SIGN_STATUS = "approved"; Reject(() => CARGO_D01Rules.ValidateReturn(r, r.START_AT)); Reject(() => CARGO_D01Rules.ValidateReturn(r, r.END_AT)); });
            Check("pending return cannot be duplicated", () => { var r = Input().Request; r.STATUS = "dispatched"; r.SIGN_STATUS = "approved"; r.PENDING_RETURN_AT = r.START_AT.Value.AddHours(1); Reject(() => CARGO_D01Rules.ValidateReturn(r, r.START_AT.Value.AddHours(2))); });
            Check("effective dispatch display transitions", () => { var r = Input().Request; r.STATUS = "dispatched"; Assert(CARGO_D01Rules.DisplayStatus(r, r.START_AT.Value) == "signing"); r.SIGN_STATUS = "approved"; Assert(CARGO_D01Rules.DisplayStatus(r, r.START_AT.Value.AddMinutes(-1)) == "ready"); Assert(CARGO_D01Rules.DisplayStatus(r, r.START_AT.Value) == "departed"); r.USAGE_AT = r.END_AT; Assert(CARGO_D01Rules.DisplayStatus(r, r.END_AT.Value) == "logged"); });
            Check("viewmodel round trip retains false and independent fields", () => { var m = new CARGO_D01FormViewModel { Applicant = "test", Preference1 = "CAR", NeedK = true, HazardousCargo = true, SelfDrive = false, ContinueMatching = true, ChangeReason = "reason", Remarks = "notes", StartAt = DateTime.Today, EndAt = DateTime.Today.AddDays(1) }; m.Locations.Add(new CARGO_D01LocationViewModel { Name = "place" }); m.SelfDrivers.Add(new CARGO_D01SelfDriverViewModel { Name = "driver", Phone = "phone" }); m.Cargo.Add(new CARGO_D01CargoViewModel { Name = "cargo", Packaging = "box" }); var dto = CARGO_D01Mapper.ToDto(m); var copy = CARGO_D01Mapper.Form(dto); Assert(copy.SelfDrive == false && copy.ContinueMatching && copy.NeedK && !copy.NeedP && copy.ChangeReason == "reason" && copy.Remarks == "notes" && copy.Cargo[0].Packaging == "box" && copy.HazardousCargo && copy.SelfDrivers[0].Phone == "phone"); copy.Locations[0].Name = "changed"; Assert(dto.Locations[0].NAME == "place"); });
            Check("oversized fields rejected before Oracle write", () => { var d = Input(); d.Request.REMARKS = new string('x', 1001); Reject(() => Validate(d)); d.Request.REMARKS = new string('x', 1000); Validate(d); });
            Console.WriteLine("Passed " + passed + " D01 tests."); return 0;
        } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
