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
        public static CARGO_D04RowViewModel Row(CARGO_D_REQUEST r) {
            return new CARGO_D04RowViewModel { RequestId = r.REQUEST_ID, Version = r.VERSION_NO, RequestNo = r.REQUEST_NO, Category = r.CATEGORY == "routine" ? "例行用車" : "一般用車", Applicant = r.APPLICANT,
                StartAt = r.START_AT, EndAt = r.END_AT, Passengers = r.PAX, Status = CARGO_D01Mapper.Status(CARGO_D01Rules.DisplayStatus(r, DateTime.Now)),
                Outcome = CARGO_D01Mapper.Outcome(r.OUTCOME), SignStatus = CARGO_DSharedMapper.SignStatus(r.SIGN_STATUS), Vehicle = r.VEHICLE, Driver = r.DRIVER1 + " " + r.DRIVER2,
                CanApprove = r.STATUS == "dispatched" && r.SIGN_STATUS == "pending" };
        }
        public static CARGO_D04DetailViewModel Detail(CARGO_DWorkDetailDto d) {
            var r = d.Detail.Application.Request;
            return new CARGO_D04DetailViewModel { Application = CARGO_DSharedMapper.Application(d), Signs = CARGO_DSharedMapper.Signs(d),
                CanSign = r.STATUS == "dispatched" && r.SIGN_STATUS == "pending" };
        }
    }
}
