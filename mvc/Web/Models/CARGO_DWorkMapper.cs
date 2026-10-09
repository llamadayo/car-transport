using System;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.ViewModels;
namespace CARGO.Web.Models {
    public static class CARGO_DWorkMapper {
        public static string SignStatus(string value) { return value == "pending" ? "待簽審" : value == "approved" ? "已同意" : value == "rejected" ? "已退回" : "—"; }
        public static CARGO_DWorkActionDto Action(CARGO_DWorkActionViewModel m) { return new CARGO_DWorkActionDto { RequestId = m.RequestId, Version = m.Version, Command = m.Command, Note = m.Note,
            Vehicle = m.Vehicle, Driver1 = m.Driver1, Driver2 = m.Driver2, EffectiveAt = m.EffectiveAt, EndAt = m.EndAt }; }
        public static CARGO_DWorkRowViewModel Row(CARGO_D_REQUEST r, int module) { return new CARGO_DWorkRowViewModel {
            RequestId = r.REQUEST_ID, Version = r.VERSION_NO, RequestNo = r.REQUEST_NO, Category = r.CATEGORY == "routine" ? "例行用車" : "一般用車", Applicant = r.APPLICANT,
            StartAt = r.START_AT, EndAt = r.END_AT, Passengers = r.PAX, Status = CARGO_D01Mapper.Status(CARGO_D01Rules.DisplayStatus(r, DateTime.Now)),
            Outcome = CARGO_D01Mapper.Outcome(r.OUTCOME), SignStatus = SignStatus(r.SIGN_STATUS), Vehicle = r.VEHICLE, Driver = r.DRIVER1 + " " + r.DRIVER2,
            CanApprove = module == 2 ? r.STATUS == "submitted" : module == 4 && r.STATUS == "dispatched" && r.SIGN_STATUS == "pending"
        }; }
        public static CARGO_DWorkDetailViewModel Detail(CARGO_DWorkDetailDto d) {
            var r = d.Detail.Application.Request;
            var application = CARGO_D01Mapper.Detail(d.Detail); application.Form.Options = CARGO_D01Mapper.Options(new CARGO_DService().GetOptions());
            return new CARGO_DWorkDetailViewModel { Application = application, PriorityRequests = d.PriorityRequests,
                CanApprove = r.STATUS == "submitted", CanDispatch = r.STATUS == "approved", CanChange = r.STATUS == "dispatched" && r.SIGN_STATUS == "approved", CanSign = r.STATUS == "dispatched" && r.SIGN_STATUS == "pending",
                Resources = d.Candidates.Where(x => x.Resource.KIND != "VEHICLE" || ((r.NEED_K != 1 || x.Resource.PERMIT_K == 1) && (r.NEED_P != 1 || x.Resource.PERMIT_P == 1))).Select(x => new CARGO_DResourceViewModel { Id = x.Resource.RESOURCE_ID, Kind = x.Resource.KIND, Reason = x.UnavailableReason,
                    Label = x.Resource.NAME + " · " + x.Resource.SITE + (x.Resource.KIND == "VEHICLE" ? " · " + x.Resource.SEATS + " 人 · " + x.Resource.VEHICLE_TYPE + " · K:" + (x.Resource.PERMIT_K == 1 ? "有" : "無") + " P:" + (x.Resource.PERMIT_P == 1 ? "有" : "無") : "") }).ToList(),
                Signs = d.Signs.Select(x => new CARGO_DSignViewModel { Round = x.ROUND_NO, Status = SignStatus(x.STATUS), SubmittedBy = x.SUBMITTED_BY, SubmittedAt = x.SUBMITTED_AT,
                    Summary = x.SUMMARY, DecidedBy = x.DECIDED_BY, DecidedAt = x.DECIDED_AT, Note = x.NOTE }).ToList()
            };
        }
    }
}
