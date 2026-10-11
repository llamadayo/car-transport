using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.Models;

internal static class DisplayTests
{
    private static int passed;
    private static void Check(bool value, string name)
    { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    public static void Main()
    {
        var resources = new List<CARGO_D_RESOURCE> {
            new CARGO_D_RESOURCE { RESOURCE_ID = "V01", KIND = "VEHICLE", NAME = "測試廂車", VEHICLE_TYPE = "VAN" },
            new CARGO_D_RESOURCE { RESOURCE_ID = "DR1", KIND = "DRIVER", NAME = "測試駕駛甲" },
            new CARGO_D_RESOURCE { RESOURCE_ID = "DR2", KIND = "DRIVER", NAME = "測試駕駛乙" }
        };
        var data = new CARGO_D01DetailDto {
            Application = new CARGO_D01SaveDto { Request = new CARGO_D_REQUEST {
                REQUEST_ID = 1, REQUEST_NO = "GU1", CATEGORY = "general", STATUS = "dispatched", SIGN_STATUS = "approved",
                START_AT = DateTime.Today, END_AT = DateTime.Today.AddDays(1), VEHICLE = "V01", DRIVER1 = "DR1", DRIVER2 = "DR2"
            } }, History = new List<CARGO_D_LOG>(), Notices = new List<CARGO_D_NOTICE>(),
            Assignments = new List<CARGO_D_ASSIGNMENT> { new CARGO_D_ASSIGNMENT { VEHICLE = "V01", DRIVER1 = "DR1", DRIVER2 = "DR2" } }
        };
        var detail = CARGO_D01Mapper.Detail(data, resources);
        Check(detail.VehicleLabel == "V01（測試廂車）" && detail.Driver1Label == "測試駕駛甲" && detail.Driver2Label == "測試駕駛乙", "request detail resolves distinct resource IDs and names");
        Check(detail.Vehicle == "V01" && detail.Driver1 == "DR1" && detail.Driver2 == "DR2", "dispatch selection retains IDs rather than display labels");
        Check(detail.Assignments[0].VehicleLabel == detail.VehicleLabel && detail.Assignments[0].Driver1Label == detail.Driver1Label, "assignment history resolves names");
        Check(data.Application.Request.DRIVER1 == "DR1" && data.Assignments[0].DRIVER1 == "DR1", "display conversion does not mutate source records");
        Check(CARGO_D01Mapper.ResourceLabel(resources, "DRIVER", "missing") == "missing" && CARGO_D01Mapper.ResourceLabel(resources, "DRIVER", null) == null, "unknown and absent resources retain safe fallbacks");
        Check(CARGO_D01Mapper.ResourceLabel(resources, "DRIVER", "SELF") == "使用者自駕", "self drive keeps its existing label");
        var usage = new CARGO_D_USAGE { VEHICLE = "V01", DRIVER1 = "DR2", DRIVER2 = "DR1", VEHICLE_TYPE = "VAN", START_KM = 100, END_KM = 101.05m, DISTANCE_KM = 1.1m };
        var row = CARGO_D05Mapper.Row(new CARGO_D05RowDto { Request = data.Application.Request, Usage = usage }, resources);
        Check(row.Vehicle == detail.VehicleLabel && row.Driver1 == "測試駕駛乙", "usage list resolves actual rather than planned driver");
        var usageDetail = CARGO_D05Mapper.Detail(new CARGO_D05DetailDto { Application = data, Resources = resources, History = new List<CARGO_D_USAGE> { usage } });
        Check(usageDetail.History[0].Driver1 == "測試駕駛乙" && usageDetail.History[0].Vehicle == detail.VehicleLabel, "usage revisions display readable resources");
        Check(usageDetail.Form.Vehicle == "V01" && usageDetail.Form.Driver1 == "DR2", "usage editing posts resource IDs unchanged");
        Check(usageDetail.Application.Driver1Label == "測試駕駛甲", "usage application still shows planned driver");
        Check(CARGO_D05Mapper.Save(usageDetail.Form).Driver1 == "DR2", "usage save mapping never stores display names");
        Check(new CARGO_DService().GetResourceLabels(new[] { "測試車 A", "SELF", "missing", "測試車 A" }).Count == 1, "display lookup is restricted to referenced resources");
        var dispatch = CARGO_D03Mapper.Detail(new CARGO_DWorkDetailDto {
            Detail = data, EffectiveAt = new DateTime(2030, 1, 2, 11, 1, 0),
            Signs = new List<CARGO_D_SIGN>(), Candidates = new List<CARGO_DCandidateDto>(), PriorityRequests = new List<string>()
        });
        Check(dispatch.EffectiveAtInputValue == "2030-01-02T11:01" && dispatch.EffectiveAtStep == "60", "D03 mapper retains minute defaults and existing field precision");
        dispatch.EffectiveAt = new DateTime(2030, 1, 2, 11, 0, 42).AddMilliseconds(124);
        Check(dispatch.EffectiveAtInputValue == "2030-01-02T11:00:42.124" && dispatch.EffectiveAtStep == "0.001" &&
            DateTime.ParseExact(dispatch.EffectiveAtInputValue, "yyyy-MM-ddTHH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture) == dispatch.EffectiveAt,
            "D03 short segment input preserves the exact millisecond default");
        dispatch.EffectiveAt = null;
        Check(dispatch.EffectiveAtInputValue == "" && dispatch.EffectiveAtStep == "0.001", "D03 unrepresentable interval leaves the existing input empty");
        var review = CARGO_D02Mapper.Row(data.Application.Request, new CARGO_D02ListInfoDto { Approver = "主管甲、主管乙", CargoCount = 3, CargoQuantity = 2999999997L });
        Check(review.Approver == "主管甲、主管乙" && review.SelfDrive == "否" && review.Cargo == "3 項／2999999997 件", "D02 displays supervisor, self-drive and 64-bit cargo total");
        var index = new CARGO.Web.ViewModels.CARGO_D06.CARGO_D06IndexViewModel();
        Check(index.FromDate == DateTime.Today && index.ToDate == DateTime.Today.AddDays(14), "D06 defaults to today through two weeks ahead");
        index.FromDate = DateTime.Today.AddDays(1); index.ToDate = DateTime.Today;
        Check(index.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(index)).Any(), "D06 model rejects reversed date range");
        index.FromDate = null; index.ToDate = null;
        Check(!index.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(index)).Any(), "D06 model permits unrestricted dates");
        // Whole-request hazard remains visible even with no cargo, and legacy line flags never override it.
        var task = new CARGO_D06TaskDto { Request = data.Application.Request, Assignment = data.Assignments[0],
            Locations = data.Application.Locations, Cargo = data.Application.Cargo };
        foreach (var hazard in new[] { 1, 0 }) {
            data.Application.Request.HAZARDOUS_CARGO = hazard;
            data.Application.Cargo.Clear();
            Check(CARGO_D06Mapper.Tasks(new List<CARGO_D06TaskDto> { task })[0].HazardousCargo == (hazard == 1), "D06 whole-request hazard with no cargo: " + hazard);
            data.Application.Cargo.Add(new CARGO_D_CARGO { NAME = "legacy", HAZARDOUS = 1 - hazard });
            Check(CARGO_D06Mapper.Tasks(new List<CARGO_D06TaskDto> { task })[0].HazardousCargo == (hazard == 1), "D06 ignores conflicting legacy cargo flag: " + hazard);
            var shared = CARGO_DSharedMapper.Application(data, resources);
            Check(shared.Form.HazardousCargo == (hazard == 1), "D02-D05 shared detail uses request flag: " + hazard);
            Check(CARGO_D01Mapper.Form(data.Application).HazardousCargo == (hazard == 1), "D01 editing uses request flag: " + hazard);
            var roundTrip = CARGO_D01Mapper.ToDto(CARGO_D01Mapper.Form(data.Application));
            Check(roundTrip.Request.HAZARDOUS_CARGO == hazard && roundTrip.Cargo[0].HAZARDOUS == 0, "D01 stores request flag without legacy line flag: " + hazard);
        }
        Console.WriteLine("PASS " + passed + " display checks");
    }
}
