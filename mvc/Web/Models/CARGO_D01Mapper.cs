using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.ViewModels.CARGO_D01;

namespace CARGO.Web.Models
{
    public static class CARGO_D01Mapper
    {
        public static readonly Dictionary<string, string> Statuses = new Dictionary<string, string> {
            { "draft", "申請中" }, { "review", "待二級審" }, { "revise", "退回修編" }, { "todo", "待調度" },
            { "noCar", "無車退回" }, { "signing", "調度主管審" }, { "ready", "待出車" }, { "departed", "已出車" }, { "logged", "已回登" }
        };
        public static string Status(string code) { string label; return Statuses.TryGetValue(code ?? "", out label) ? label : code; }
        public static string Outcome(string code)
        { return code == "withDriver" ? "派車＋派司機" : code == "selfDrive" ? "派車（自駕）" : code == "noVehicle" ? "無車退回" : "尚未派車"; }
        public static CARGO_D01SaveDto ToDto(CARGO_D01FormViewModel m)
        {
            return new CARGO_D01SaveDto {
                Request = new CARGO_D_REQUEST {
                    REQUEST_ID = m.RequestId, VERSION_NO = m.Version,
                    CATEGORY = m.Category,
                    TRANSPORT_TYPE = m.TransportType,
                    APPLICANT = m.Applicant,
                    DEPT = m.Department,
                    EXT = m.Extension,
                    PLAN_CODE = m.PlanCode,
                    CAMPUS = m.Campus,
                    PREFERENCE1 = m.Preference1,
                    PREFERENCE2 = m.Preference2,
                    PREFERENCE3 = m.Preference3,
                    EXERCISE_PLAN = m.ExercisePlan,
                    LEADER = m.Leader,
                    LEADER_EXT = m.LeaderExtension,
                    LEADER_PHONE = m.LeaderPhone,
                    PICKUP_LOCATION = m.PickupLocation,
                    PURPOSE = m.Purpose,
                    TRIPLICATE_NO = m.TriplicateNo,
                    ESCORT_NO = m.EscortNo,
                    CHANGE_REASON = m.ChangeReason,
                    REMARKS = m.Remarks,
                    START_AT = m.StartAt,
                    END_AT = m.EndAt,
                    PAX = m.Passengers,
                    SELF_DRIVE = m.SelfDrive.HasValue ? (m.SelfDrive.Value ? 1 : 0) : (int?)null,
                    NEED_K = m.NeedK ? 1 : 0,
                    NEED_P = m.NeedP ? 1 : 0,
                    ONE_WAY = m.OneWay ? 1 : 0,
                    ALLOW_MERGE = m.AllowMerge ? 1 : 0,
                    NIGHT_OT = m.NightOvertime ? 1 : 0,
                    HOLIDAY_OT = m.HolidayOvertime ? 1 : 0,
                    CONTINUE_MATCHING = m.ContinueMatching ? 1 : 0,
                    WILLING_SELF_DRIVE = m.WillingSelfDrive ? 1 : 0,
                    QUALIFIED_LICENSE = m.QualifiedLicense ? 1 : 0,
                    ENTER_TAIPEI = m.EnterTaipei ? 1 : 0,
                    CROSS_CAMPUS = m.CrossCampus ? 1 : 0,
                    HAS_CARGO = m.HasCargo ? 1 : 0,
                    HAZARDOUS_CARGO = m.HazardousCargo ? 1 : 0,
                    PYROTECHNIC_CARGO = m.PyrotechnicCargo ? 1 : 0,
                    NEEDS_HOISTING = m.NeedsHoisting ? 1 : 0,
                    REQUEST_REVIEW = m.RequestReview ? 1 : 0,
                },
                Locations = (m.Locations ?? new List<CARGO_D01LocationViewModel>()).Select(x => new CARGO_D_LOCATION { NAME = x.Name }).ToList(),
                SelfDrivers = (m.SelfDrivers ?? new List<CARGO_D01SelfDriverViewModel>()).Select(x => new CARGO_D_SELF_DRIVER { NAME = x.Name, EXT = x.Extension, PHONE = x.Phone }).ToList(),
                Cargo = (m.Cargo ?? new List<CARGO_D01CargoViewModel>()).Select(x => new CARGO_D_CARGO { NAME = x.Name, LENGTH_CM = x.Length, WIDTH_CM = x.Width, HEIGHT_CM = x.Height, WEIGHT_KG = x.Weight, QUANTITY = x.Quantity, CATEGORY = x.Category, PACKAGING = x.Packaging }).ToList(),
            };
        }
        public static CARGO_D01FormViewModel Form(CARGO_D01SaveDto data)
        {
            var r = data.Request;
            return new CARGO_D01FormViewModel {
                RequestId = r.REQUEST_ID, RequestNo = r.REQUEST_NO, Version = r.VERSION_NO, Status = r.STATUS, ReviewNote = r.REVIEW_NOTE,
                Category = r.CATEGORY,
                TransportType = r.TRANSPORT_TYPE,
                Applicant = r.APPLICANT,
                Department = r.DEPT,
                Extension = r.EXT,
                PlanCode = r.PLAN_CODE,
                Campus = r.CAMPUS,
                Preference1 = r.PREFERENCE1,
                Preference2 = r.PREFERENCE2,
                Preference3 = r.PREFERENCE3,
                ExercisePlan = r.EXERCISE_PLAN,
                Leader = r.LEADER,
                LeaderExtension = r.LEADER_EXT,
                LeaderPhone = r.LEADER_PHONE,
                PickupLocation = r.PICKUP_LOCATION,
                Purpose = r.PURPOSE,
                TriplicateNo = r.TRIPLICATE_NO,
                EscortNo = r.ESCORT_NO,
                ChangeReason = r.CHANGE_REASON,
                Remarks = r.REMARKS,
                StartAt = r.START_AT,
                EndAt = r.END_AT,
                Passengers = r.PAX,
                SelfDrive = r.SELF_DRIVE.HasValue ? (bool?)(r.SELF_DRIVE == 1) : null,
                NeedK = r.NEED_K == 1,
                NeedP = r.NEED_P == 1,
                OneWay = r.ONE_WAY == 1,
                AllowMerge = r.ALLOW_MERGE == 1,
                NightOvertime = r.NIGHT_OT == 1,
                HolidayOvertime = r.HOLIDAY_OT == 1,
                ContinueMatching = r.CONTINUE_MATCHING == 1,
                WillingSelfDrive = r.WILLING_SELF_DRIVE == 1,
                QualifiedLicense = r.QUALIFIED_LICENSE == 1,
                EnterTaipei = r.ENTER_TAIPEI == 1,
                CrossCampus = r.CROSS_CAMPUS == 1,
                HasCargo = r.HAS_CARGO == 1,
                HazardousCargo = r.HAZARDOUS_CARGO == 1,
                PyrotechnicCargo = r.PYROTECHNIC_CARGO == 1,
                NeedsHoisting = r.NEEDS_HOISTING == 1,
                RequestReview = r.REQUEST_REVIEW == 1,
                Locations = data.Locations.Select(x => new CARGO_D01LocationViewModel { Name = x.NAME }).ToList(),
                SelfDrivers = data.SelfDrivers.Select(x => new CARGO_D01SelfDriverViewModel { Name = x.NAME, Extension = x.EXT, Phone = x.PHONE }).ToList(),
                Cargo = data.Cargo.Select(x => new CARGO_D01CargoViewModel { Name = x.NAME, Length = x.LENGTH_CM, Width = x.WIDTH_CM, Height = x.HEIGHT_CM, Weight = x.WEIGHT_KG, Quantity = x.QUANTITY, Category = x.CATEGORY, Packaging = x.PACKAGING }).ToList(),
            };
        }
        public static CARGO_D01RowViewModel Row(CARGO_D01ListItemDto r)
        {
            return new CARGO_D01RowViewModel { RequestId = r.REQUEST_ID, RequestNo = r.REQUEST_NO,
                Category = r.CATEGORY == "routine" ? "例行用車" : "一般用車", Applicant = r.APPLICANT,
                StartAt = r.START_AT, EndAt = r.END_AT, Passengers = r.PAX, SelfDrive = r.SELF_DRIVE == 1 ? "是" : "否",
                Cargo = r.CARGO_COUNT + " 項／" + r.CARGO_QUANTITY + " 件", Status = Status(r.DISPLAY_STATUS),
                Outcome = Outcome(r.OUTCOME), CreatedAt = r.CREATED_AT };
        }
        public static IEnumerable<string> ResourceIds(CARGO_D01DetailDto d)
        {
            var r = d.Application.Request;
            return new[] { r.VEHICLE, r.DRIVER1, r.DRIVER2 }.Concat(d.Assignments.SelectMany(x => new[] { x.VEHICLE, x.DRIVER1, x.DRIVER2 }));
        }
        public static string ResourceLabel(IEnumerable<CARGO_D_RESOURCE> resources, string kind, string id)
        {
            if (kind == "DRIVER" && id == "SELF") return "使用者自駕";
            var resource = resources == null ? null : resources.FirstOrDefault(x => x.KIND == kind && x.RESOURCE_ID == id);
            if (resource == null || String.IsNullOrEmpty(resource.NAME)) return id;
            return kind == "VEHICLE" && resource.NAME != id ? id + "（" + resource.NAME + "）" : resource.NAME;
        }
        public static CARGO_D01DetailViewModel Detail(CARGO_D01DetailDto d, IEnumerable<CARGO_D_RESOURCE> resources = null)
        {
            var r = d.Application.Request;
            return new CARGO_D01DetailViewModel {
                Form = Form(d.Application), StatusLabel = Status(CARGO_D01Rules.DisplayStatus(r, DateTime.Now)), Outcome = Outcome(r.OUTCOME),
                Approver = r.APPROVER_NAME, Vehicle = r.VEHICLE, Driver1 = r.DRIVER1, Driver2 = r.DRIVER2,
                VehicleLabel = ResourceLabel(resources, "VEHICLE", r.VEHICLE), Driver1Label = ResourceLabel(resources, "DRIVER", r.DRIVER1), Driver2Label = ResourceLabel(resources, "DRIVER", r.DRIVER2),
                DispatchNo = r.DISPATCH_NO, DispatchNote = r.DISPATCH_NOTE, DispatchedBy = r.DISPATCHED_BY,
                DispatchedAt = r.DISPATCHED_AT, CreatedAt = r.CREATED_AT, PendingReturnAt = r.PENDING_RETURN_AT, ReturnReason = r.RETURN_REASON,
                CanEdit = CARGO_D01Rules.CanEdit(r), CanWithdraw = CARGO_D01Rules.CanWithdraw(r), CanReturn = CARGO_D01Rules.CanReturn(r),
                History = d.History.Select(x => new CARGO_D01EventViewModel { At = x.CREATED_AT, Kind = x.ACTION, Actor = x.ACTOR, Text = x.NOTE }).ToList(),
                Notices = d.Notices.Select(x => new CARGO_D01EventViewModel { At = x.SENT_AT, Kind = x.KIND, Actor = x.RECIPIENT, Text = x.MESSAGE }).ToList(),
                Assignments = d.Assignments.Select(x => new CARGO_D01AssignmentViewModel { From = x.FROM_AT, To = x.TO_AT, Vehicle = x.VEHICLE, Driver1 = x.DRIVER1, Driver2 = x.DRIVER2,
                    VehicleLabel = ResourceLabel(resources, "VEHICLE", x.VEHICLE), Driver1Label = ResourceLabel(resources, "DRIVER", x.DRIVER1), Driver2Label = ResourceLabel(resources, "DRIVER", x.DRIVER2), Kind = x.KIND, Reason = x.REASON }).ToList()
            };
        }
        public static List<CARGO_DOptionViewModel> Options(List<CARGO_D_OPTION> items)
        { return items.Select(x => new CARGO_DOptionViewModel { Type = x.OPTION_TYPE, Code = x.CODE, Label = x.LABEL }).ToList(); }
    }
}

