using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.Service;
using CARGO.Web.ViewModels.CARGO_D01;
using CARGO.Web.ViewModels.Shared;
namespace CARGO.Web.Models {
    internal static class CARGO_DSharedMapper {
        internal static string SignStatus(string value) { return value == "pending" ? "待簽審" : value == "approved" ? "已同意" : value == "rejected" ? "已退回" : "—"; }
        internal static CARGO_D01DetailViewModel Application(CARGO_DWorkDetailDto d) {
            return Application(d.Detail);
        }
        internal static CARGO_D01DetailViewModel Application(CARGO_D01DetailDto d) {
            var application = CARGO_D01Mapper.Detail(d);
            application.Form.Options = CARGO_D01Mapper.Options(new CARGO_DService().GetOptions());
            return application;
        }
        internal static List<CARGO_DSignViewModel> Signs(CARGO_DWorkDetailDto d) {
            return d.Signs.Select(x => new CARGO_DSignViewModel { Round = x.ROUND_NO, Status = SignStatus(x.STATUS), SubmittedBy = x.SUBMITTED_BY, SubmittedAt = x.SUBMITTED_AT,
                Summary = x.SUMMARY, DecidedBy = x.DECIDED_BY, DecidedAt = x.DECIDED_AT, Note = x.NOTE }).ToList();
        }
    }
}
