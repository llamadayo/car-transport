using System;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.ViewModels.CARGO_D04;
namespace CARGO.Web.Models {
    public static class CARGO_D04Mapper {
        public static CARGO_DWorkActionDto Action(CARGO_D04ActionViewModel m) {
            return new CARGO_DWorkActionDto { RequestId = m.RequestId, Version = m.Version, Command = m.Command, Note = m.Note };
        }
        public static CARGO_D04RowViewModel Row(CARGO_D_REQUEST r, CARGO_D_SIGN sign = null) {
            return new CARGO_D04RowViewModel { RequestId = r.REQUEST_ID, Version = r.VERSION_NO, RequestNo = r.REQUEST_NO, Category = r.CATEGORY == "routine" ? "例行用車" : "一般用車", Applicant = r.APPLICANT,
                StartAt = r.START_AT, EndAt = r.END_AT, Passengers = r.PAX, Status = CARGO_D01Mapper.Status(CARGO_D01Rules.DisplayStatus(r, DateTime.Now)),
                Outcome = CARGO_D01Mapper.Outcome(r.OUTCOME), SignStatus = CARGO_DSharedMapper.SignStatus(sign == null ? r.SIGN_STATUS : sign.STATUS),
                Round = sign == null ? 0 : sign.ROUND_NO, SubmittedAt = sign == null ? (DateTime?)null : sign.SUBMITTED_AT, SubmittedBy = sign == null ? null : sign.SUBMITTED_BY,
                DecidedAt = sign == null ? (DateTime?)null : sign.DECIDED_AT, DecidedBy = sign == null ? null : sign.DECIDED_BY, SignSummary = sign == null ? null : sign.SUMMARY, Vehicle = r.VEHICLE, Driver = r.DRIVER1 + " " + r.DRIVER2,
                CanApprove = r.STATUS == "dispatched" && r.SIGN_STATUS == "pending" };
        }
        public static CARGO_D04DetailViewModel Detail(CARGO_DWorkDetailDto d) {
            var r = d.Detail.Application.Request;
            return new CARGO_D04DetailViewModel { Application = CARGO_DSharedMapper.Application(d), Signs = CARGO_DSharedMapper.Signs(d),
                CanSign = r.STATUS == "dispatched" && r.SIGN_STATUS == "pending" };
        }
    }
}
