using System;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.ViewModels.CARGO_D03;
namespace CARGO.Web.Models {
    public static class CARGO_D03Mapper {
        public static CARGO_DWorkActionDto Action(CARGO_D03ActionViewModel m) {
            return new CARGO_DWorkActionDto { RequestId = m.RequestId, Version = m.Version, Command = m.Command, SupplementDriver = m.SupplementDriver, Note = m.Note,
                Vehicle = m.Vehicle, Driver1 = m.Driver1, Driver2 = m.Driver2, EffectiveAt = m.EffectiveAt, EndAt = m.EndAt };
        }
        public static CARGO_D03RowViewModel Row(CARGO_D_REQUEST r) {
            return new CARGO_D03RowViewModel { RequestId = r.REQUEST_ID, Version = r.VERSION_NO, RequestNo = r.REQUEST_NO, Category = r.CATEGORY == "routine" ? "例行用車" : "一般用車", Applicant = r.APPLICANT,
                StartAt = r.START_AT, EndAt = r.END_AT, Passengers = r.PAX, Status = CARGO_D01Mapper.Status(CARGO_D01Rules.DisplayStatus(r, DateTime.Now)),
                Outcome = CARGO_D01Mapper.Outcome(r.OUTCOME), SignStatus = CARGO_DSharedMapper.SignStatus(r.SIGN_STATUS), Vehicle = r.VEHICLE, Driver = r.DRIVER1 + " " + r.DRIVER2,
                SelfDrive = r.SELF_DRIVE == 1, HazardousCargo = r.HAZARDOUS_CARGO == 1,
                Requirements = String.Join("、", new[] { r.NEED_K == 1 ? "K 證" : null, r.NEED_P == 1 ? "P 證" : null, r.ENTER_TAIPEI == 1 ? "進台北市" : null, r.NIGHT_OT == 1 ? "夜間加班" : null, r.HOLIDAY_OT == 1 ? "假日加班" : null }.Where(x => x != null)),
                HasPendingReturn = r.PENDING_RETURN_AT.HasValue, ActionLabel = r.STATUS == "approved" ? "調度" : r.PENDING_RETURN_AT.HasValue ? "處理" : "細節" };
        }
        public static CARGO_D03DetailViewModel Detail(CARGO_DWorkDetailDto d) {
            var r = d.Detail.Application.Request;
            return new CARGO_D03DetailViewModel { Application = CARGO_DSharedMapper.Application(d), Signs = CARGO_DSharedMapper.Signs(d),
                CanSupplement = r.STATUS == "dispatched" && r.SIGN_STATUS == "approved" && r.CONTINUE_MATCHING == 1 && String.IsNullOrEmpty(r.DRIVER1) && String.IsNullOrEmpty(r.DRIVER2),
                Signing = r.SIGN_STATUS == "pending",
                ExcludedVehicles = d.Candidates.Where(x => x.Resource.KIND == "VEHICLE" && ((r.NEED_K == 1 && x.Resource.PERMIT_K != 1) || (r.NEED_P == 1 && x.Resource.PERMIT_P != 1))).Select(x => x.Resource.NAME).ToList(),
                CanDispatch = r.STATUS == "approved", CanChange = r.STATUS == "dispatched" && r.SIGN_STATUS == "approved",
                PriorityRequests = d.PriorityRequests, EffectiveAt = d.EffectiveAt,
                Resources = d.Candidates.Where(x => x.Resource.KIND != "VEHICLE" || (r.STATUS == "dispatched" && r.SIGN_STATUS == "approved" && x.Resource.RESOURCE_ID == r.VEHICLE) || ((r.NEED_K != 1 || x.Resource.PERMIT_K == 1) && (r.NEED_P != 1 || x.Resource.PERMIT_P == 1))).Select(x => new CARGO_D03ResourceViewModel { Name = x.Resource.NAME, Site = x.Resource.SITE, Seats = x.Resource.SEATS, Tons = x.Resource.TONS, Permits = "K:" + (x.Resource.PERMIT_K == 1 ? "有" : "無") + " P:" + (x.Resource.PERMIT_P == 1 ? "有" : "無"), Id = x.Resource.RESOURCE_ID, Kind = x.Resource.KIND, Reason = x.UnavailableReason,
                    Label = x.Resource.NAME + " · " + x.Resource.SITE + (x.Resource.KIND == "VEHICLE" ? " · " + x.Resource.SEATS + " 人 · " + (x.Resource.TONS.HasValue ? x.Resource.TONS.Value.ToString("0.###") + " 噸" : "噸位未提供") + (r.ENTER_TAIPEI == 1 && x.Resource.TONS >= 6.5m ? " ⚠ 台北市通行注意" : "") + " · " + x.Resource.VEHICLE_TYPE + " · K:" + (x.Resource.PERMIT_K == 1 ? "有" : "無") + " P:" + (x.Resource.PERMIT_P == 1 ? "有" : "無") : "") }).ToList() };
        }
    }
}
