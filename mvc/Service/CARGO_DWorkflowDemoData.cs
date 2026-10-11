#if LOCAL_DEMO
using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO.Tables;
namespace CARGO.Service {
    internal static class CARGO_DWorkflowDemoData {
        internal static List<CARGO_D_SIGN> Signs = CARGO_D01DemoData.Records.Values.Where(x => x.Application.Request.STATUS == "dispatched").Select(x => new CARGO_D_SIGN {
            REQUEST_ID = x.Application.Request.REQUEST_ID, ROUND_NO = 1, STATUS = x.Application.Request.SIGN_STATUS, SUBMITTED_BY = "示範調度", SUBMITTED_AT = DateTime.Now,
            SUMMARY = x.Application.Request.VEHICLE + "／" + x.Application.Request.DRIVER1, DECIDED_BY = x.Application.Request.SIGN_STATUS == "approved" ? "示範運輸主管" : null
        }).ToList();
        internal static CARGO_DWorkState Load() {
            var state = new CARGO_DWorkState { Records = CARGO_D01DemoData.Copy(CARGO_D01DemoData.Records.Values.ToList()), Signs = CARGO_D01DemoData.Copy(Signs),
                Approvers = CARGO_D01DemoData.Approvers() };
            foreach (var name in new[] { "測試車 A", "測試車 B", "測試車 C", "測試車 D" }) state.Resources.Add(new CARGO_D_RESOURCE { RESOURCE_ID = name, NAME = name, KIND = "VEHICLE", POOL = "BIZ", SITE = "TEST_A", VEHICLE_TYPE = "CAR", SEATS = 5, TONS = name == "測試車 D" ? 6.5m : 2m, PERMIT_K = 1, PERMIT_P = 1, ACTIVE = 1 });
            foreach (var name in new[] { "示範駕駛", "示範駕駛 B", "示範駕駛 C" }) state.Resources.Add(new CARGO_D_RESOURCE { RESOURCE_ID = name, NAME = name, KIND = "DRIVER", POOL = "BIZ", SITE = "TEST_A", ACTIVE = 1 });
            return state;
        }
    }
}
#endif
