using System;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.ViewModels.CARGO_D02;
namespace CARGO.Web.Models {
    public static class CARGO_D02Mapper {
        public static CARGO_DWorkActionDto Action(CARGO_D02ActionViewModel m) {
            return new CARGO_DWorkActionDto { RequestId = m.RequestId, Version = m.Version, Command = m.Command, Note = m.Note };
        }
        public static CARGO_D02RowViewModel Row(CARGO_D_REQUEST r, CARGO_D02ListInfoDto info) {
            return new CARGO_D02RowViewModel { RequestId = r.REQUEST_ID, Version = r.VERSION_NO, RequestNo = r.REQUEST_NO, Category = r.CATEGORY == "routine" ? "例行用車" : "一般用車", Applicant = r.APPLICANT,
                StartAt = r.START_AT, EndAt = r.END_AT, Passengers = r.PAX, Status = CARGO_D01Mapper.Status(CARGO_D01Rules.DisplayStatus(r, DateTime.Now)),
                Approver = String.IsNullOrWhiteSpace(info.Approver) ? "未設定" : info.Approver,
                SelfDrive = r.SELF_DRIVE == 1 ? "是" : "否", Cargo = info.CargoCount + " 項／" + info.CargoQuantity + " 件",
                HazardousCargo = r.HAZARDOUS_CARGO == 1, CanApprove = r.STATUS == "submitted" };
        }
        public static CARGO_D02DetailViewModel Detail(CARGO_DWorkDetailDto d) {
            var r = d.Detail.Application.Request;
            return new CARGO_D02DetailViewModel { Application = CARGO_DSharedMapper.Application(d), Signs = CARGO_DSharedMapper.Signs(d),
                CanApprove = r.STATUS == "submitted" };
        }
    }
}
