#if LOCAL_DEMO
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using CARGO.DTO;
using CARGO.DTO.Tables;

namespace CARGO.Service
{
    // Only compiled into LocalDemo. No files, Oracle connections or external services.
    internal static class CARGO_D01DemoData
    {
        internal static readonly object Sync = new object();
        internal static readonly Dictionary<long, CARGO_D01DetailDto> Records = Create();
        internal static long Sequence = 7;

        internal static T Copy<T>(T value)
        {
            var serializer = new DataContractSerializer(typeof(T));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value); stream.Position = 0;
                return (T)serializer.ReadObject(stream);
            }
        }
        internal static List<CARGO_D_APPROVER> Approvers()
        { return new List<CARGO_D_APPROVER> { new CARGO_D_APPROVER { OWNER_ID = "示範申請人", APPROVER_ID = "示範申請人", ACTIVE = 1 } }; }
        internal static List<CARGO_D_OPTION> Options()
        {
            return new List<CARGO_D_OPTION> {
                Option("CAMPUS", "TEST_A", "測試院區 A"), Option("CAMPUS", "TEST_B", "測試院區 B"),
                Option("VEHICLE_TYPE", "CAR", "測試小客車"), Option("VEHICLE_TYPE", "VAN", "測試廂型車"),
                Option("VEHICLE_TYPE", "BUS", "測試中型巴士"),
                Option("CARGO_CATEGORY", "BOX", "標準紙箱"), Option("CARGO_CATEGORY", "PALLET", "棧板貨"),
                Option("CARGO_CATEGORY", "LONG", "長條物品"), Option("CARGO_CATEGORY", "IRREG", "不規則件"),
                Option("CARGO_CATEGORY", "DRUM", "桶裝物品"), Option("CARGO_CATEGORY", "FRAG", "易碎物品")
            };
        }
        private static CARGO_D_OPTION Option(string type, string code, string label)
        { return new CARGO_D_OPTION { OPTION_TYPE = type, CODE = code, LABEL = label, ACTIVE = 1 }; }
        private static Dictionary<long, CARGO_D01DetailDto> Create()
        {
            var records = new Dictionary<long, CARGO_D01DetailDto>();
            var states = new[] { "draft", "submitted", "rejected", "approved", "dispatched", "noVehicle", "dispatched" };
            for (var i = 0; i < states.Length; i++)
            {
                var id = i + 1;
                var r = new CARGO_D_REQUEST {
                    REQUEST_ID = id, REQUEST_NO = "GU" + id.ToString("D6"), OWNER_ID = "示範申請人", APPLICANT = "示範申請人",
                    DEPT = "測試行政組", EXT = "1234", CATEGORY = "general", TRANSPORT_TYPE = "一般",
                    PREFERENCE1 = "CAR", PREFERENCE2 = "VAN", CAMPUS = "TEST_A",
                    START_AT = DateTime.Today.AddDays(1).AddHours(9), END_AT = DateTime.Today.AddDays(1).AddHours(17),
                    PAX = 2, SELF_DRIVE = 0, REQUEST_REVIEW = 1, STATUS = states[i], VERSION_NO = 1,
                    PURPOSE = "示範行程 " + id, PICKUP_LOCATION = "測試行政大樓門口", LEADER = "測試車長", LEADER_EXT = "1234",
                    CREATED_AT = DateTime.Now.AddMinutes(-30 + i), UPDATED_AT = DateTime.Now.AddMinutes(-30 + i)
                };
                var detail = NewDetail(new CARGO_D01SaveDto { Request = r });
                detail.Application.Locations.Add(new CARGO_D_LOCATION { REQUEST_ID = id, LINE_NO = 1, NAME = "測試目的地 A" });
                if (r.STATUS == "rejected") { r.REVIEW_NOTE = "請補充用車事由與上車地點後重新送出。"; r.APPROVER_NAME = "示範主管"; }
                if (r.STATUS == "dispatched")
                {
                    r.DISPATCHED_AT = DateTime.Now.AddMinutes(-15); r.DISPATCHED_BY = "示範調度";
                    r.DISPATCH_NO = "GD" + id.ToString("D6"); r.OUTCOME = "withDriver"; r.VEHICLE = id == 5 ? "測試車 A" : "測試車 B"; r.DRIVER1 = id == 5 ? "示範駕駛" : "示範駕駛 B";
                    r.SIGN_STATUS = id == 5 ? "approved" : "pending";
                    detail.Assignments.Add(new CARGO_D_ASSIGNMENT { REQUEST_ID = id, LINE_NO = 1,
                        FROM_AT = r.START_AT.Value, TO_AT = r.END_AT.Value, VEHICLE = r.VEHICLE, DRIVER1 = r.DRIVER1,
                        KIND = "初次指派", REASON = "示範資料", ACTOR = "示範調度" });
                }
                if (r.STATUS == "noVehicle") { r.OUTCOME = "noVehicle"; r.DISPATCHED_AT = DateTime.Now; r.DISPATCH_NOTE = "示範無車退回紀錄"; }
                AddLog(detail, "建立示範資料", "示範系統", "虛構測試資料，供查看不同狀態的可用操作。");
                records.Add(id, detail);
            }
            return records;
        }
        internal static CARGO_D01DetailDto NewDetail(CARGO_D01SaveDto data)
        {
            return new CARGO_D01DetailDto { Application = data, History = new List<CARGO_D_LOG>(),
                Assignments = new List<CARGO_D_ASSIGNMENT>(), Notices = new List<CARGO_D_NOTICE>() };
        }
        internal static void AddLog(CARGO_D01DetailDto d, string action, string actor, string note)
        {
            d.History.Add(new CARGO_D_LOG { LOG_ID = d.History.Count + 1, REQUEST_ID = d.Application.Request.REQUEST_ID,
                ACTION = action, ACTOR = actor, NOTE = note, CREATED_AT = DateTime.Now });
        }
    }
}
#endif
