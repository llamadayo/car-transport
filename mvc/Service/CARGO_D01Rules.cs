using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;

namespace CARGO.Service
{
    public static class CARGO_D01Rules
    {
        public static bool CanEdit(CARGO_D_REQUEST r) { return r.STATUS == "draft" || r.STATUS == "rejected"; }
        public static bool CanWithdraw(CARGO_D_REQUEST r)
        { return (r.STATUS == "submitted" || r.STATUS == "approved") && !r.DISPATCHED_AT.HasValue; }
        public static bool CanReturn(CARGO_D_REQUEST r)
        { return r.STATUS == "dispatched" && r.SIGN_STATUS == "approved" && !r.PENDING_RETURN_AT.HasValue; }
        public static string DisplayStatus(CARGO_D_REQUEST r, DateTime now)
        {
            switch (r.STATUS)
            {
                case "draft": return "draft";
                case "submitted": return "review";
                case "rejected": return "revise";
                case "approved": return "todo";
                case "noVehicle": return "noCar";
                case "dispatched":
                    if (r.SIGN_STATUS != "approved") return "signing";
                    if (r.USAGE_AT.HasValue) return "logged";
                    return now >= r.START_AT ? "departed" : "ready";
                default: throw new InvalidOperationException("未知申請狀態");
            }
        }
        public static void Validate(CARGO_D01SaveDto data, CARGO_DActorDto actor, IList<CARGO_D_OPTION> options, bool submit)
        {
            if (data == null || data.Request == null || data.Locations == null || data.SelfDrivers == null || data.Cargo == null)
                throw new InvalidOperationException("申請資料不完整");
            var r = data.Request;
            var errors = new List<string>();
            if (r.CATEGORY != "general" && r.CATEGORY != "routine") errors.Add("用車類別不正確");
            if (r.CATEGORY == "routine" && !actor.CanChooseRoutine) errors.Add("目前身分不可申請例行用車");
            if (!new[] { "一般", "自駕", "演訓", "演訓自駕", "院區接駁" }.Contains(r.TRANSPORT_TYPE)) errors.Add("運輸類別不正確");
            if (String.IsNullOrWhiteSpace(r.APPLICANT)) errors.Add("請填寫申請人");
            if (!r.START_AT.HasValue || !r.END_AT.HasValue || r.END_AT <= r.START_AT) errors.Add("請填寫有效的用車起訖時間，迄時須晚於起時");
            if (r.PAX < 1 || r.PAX > 999999) errors.Add("乘客數需為 1 至 999999 的整數");
            if (r.SELF_DRIVE != 0 && r.SELF_DRIVE != 1) errors.Add("請選擇是否自駕");
            if (submit && r.REQUEST_REVIEW != 1) errors.Add("是否送審選否時，請使用暫存");
            if (!String.IsNullOrEmpty(r.CAMPUS) && !HasOption(options, "CAMPUS", r.CAMPUS)) errors.Add("車屬院區不在清單內");
            var prefs = new[] { r.PREFERENCE1, r.PREFERENCE2, r.PREFERENCE3 };
            if (String.IsNullOrWhiteSpace(r.PREFERENCE1)) errors.Add("請選擇車種順位 1");
            var chosen = prefs.Where(x => !String.IsNullOrWhiteSpace(x)).ToArray();
            if (chosen.Distinct().Count() != chosen.Length) errors.Add("車種順位不可重複");
            if (chosen.Any(x => !HasOption(options, "VEHICLE_TYPE", x))) errors.Add("車種順位不在商務車種清單內");
            if (data.SelfDrivers.Count > 8) errors.Add("自駕人最多 8 筆");
            foreach (var row in data.SelfDrivers)
            {
                if (row == null) { errors.Add("自駕人資料不完整"); continue; }
                CheckLength(errors, row.NAME, 100, "自駕人姓名"); CheckLength(errors, row.EXT, 30, "自駕人分機"); CheckLength(errors, row.PHONE, 30, "自駕人手機");
            }
            foreach (var row in data.Locations)
            {
                if (row == null || String.IsNullOrWhiteSpace(row.NAME)) errors.Add("行程地點不可空白，請填寫或刪除該列");
                else CheckLength(errors, row.NAME, 300, "行程地點");
            }
            foreach (var row in data.Cargo)
            {
                if (row == null) { errors.Add("隨行貨物資料不完整"); continue; }
                if (String.IsNullOrWhiteSpace(row.NAME)) errors.Add("請填寫隨行貨物名稱");
                CheckLength(errors, row.NAME, 200, "貨物名稱"); CheckLength(errors, row.PACKAGING, 200, "外包裝");
                if (row.LENGTH_CM <= 0 || row.WIDTH_CM <= 0 || row.HEIGHT_CM <= 0 || row.QUANTITY <= 0) errors.Add("貨物尺寸需為正數，件數需為正整數");
                if (row.WEIGHT_KG < 0 || row.WEIGHT_KG >= 1000000000m || row.LENGTH_CM >= 1000000000m || row.WIDTH_CM >= 1000000000m || row.HEIGHT_CM >= 1000000000m || row.QUANTITY > 999999999) errors.Add("貨物數值超出可儲存範圍");
                if (!HasOption(options, "CARGO_CATEGORY", row.CATEGORY)) errors.Add("貨物品類不在清單內");
                if (row.HAZARDOUS != 0 && row.HAZARDOUS != 1) errors.Add("危險品標記不正確");
            }
            CheckLength(errors, r.CATEGORY, 20, "用車類別");
            CheckLength(errors, r.TRANSPORT_TYPE, 20, "運輸類別");
            CheckLength(errors, r.APPLICANT, 100, "申請人");
            CheckLength(errors, r.DEPT, 100, "部門");
            CheckLength(errors, r.EXT, 30, "分機");
            CheckLength(errors, r.PLAN_CODE, 100, "計畫代號");
            CheckLength(errors, r.CAMPUS, 50, "車屬院區");
            CheckLength(errors, r.PREFERENCE1, 50, "車種順位 1");
            CheckLength(errors, r.PREFERENCE2, 50, "車種順位 2");
            CheckLength(errors, r.PREFERENCE3, 50, "車種順位 3");
            CheckLength(errors, r.EXERCISE_PLAN, 200, "演訓計畫名稱");
            CheckLength(errors, r.LEADER, 100, "車長");
            CheckLength(errors, r.LEADER_EXT, 30, "車長分機");
            CheckLength(errors, r.LEADER_PHONE, 30, "車長手機");
            CheckLength(errors, r.PICKUP_LOCATION, 300, "上車地點");
            CheckLength(errors, r.PURPOSE, 1000, "事由");
            CheckLength(errors, r.TRIPLICATE_NO, 100, "三聯單表單編號");
            CheckLength(errors, r.ESCORT_NO, 100, "護運單號");
            CheckLength(errors, r.CHANGE_REASON, 1000, "異動事由");
            CheckLength(errors, r.REMARKS, 1000, "備註");
            if (r.NEED_K != 0 && r.NEED_K != 1) errors.Add("NEED_K 選項不正確");
            if (r.NEED_P != 0 && r.NEED_P != 1) errors.Add("NEED_P 選項不正確");
            if (r.ONE_WAY != 0 && r.ONE_WAY != 1) errors.Add("ONE_WAY 選項不正確");
            if (r.ALLOW_MERGE != 0 && r.ALLOW_MERGE != 1) errors.Add("ALLOW_MERGE 選項不正確");
            if (r.NIGHT_OT != 0 && r.NIGHT_OT != 1) errors.Add("NIGHT_OT 選項不正確");
            if (r.HOLIDAY_OT != 0 && r.HOLIDAY_OT != 1) errors.Add("HOLIDAY_OT 選項不正確");
            if (r.CONTINUE_MATCHING != 0 && r.CONTINUE_MATCHING != 1) errors.Add("CONTINUE_MATCHING 選項不正確");
            if (r.WILLING_SELF_DRIVE != 0 && r.WILLING_SELF_DRIVE != 1) errors.Add("WILLING_SELF_DRIVE 選項不正確");
            if (r.QUALIFIED_LICENSE != 0 && r.QUALIFIED_LICENSE != 1) errors.Add("QUALIFIED_LICENSE 選項不正確");
            if (r.ENTER_TAIPEI != 0 && r.ENTER_TAIPEI != 1) errors.Add("ENTER_TAIPEI 選項不正確");
            if (r.CROSS_CAMPUS != 0 && r.CROSS_CAMPUS != 1) errors.Add("CROSS_CAMPUS 選項不正確");
            if (r.HAS_CARGO != 0 && r.HAS_CARGO != 1) errors.Add("HAS_CARGO 選項不正確");
            if (r.HAZARDOUS_CARGO != 0 && r.HAZARDOUS_CARGO != 1) errors.Add("HAZARDOUS_CARGO 選項不正確");
            if (r.PYROTECHNIC_CARGO != 0 && r.PYROTECHNIC_CARGO != 1) errors.Add("PYROTECHNIC_CARGO 選項不正確");
            if (r.NEEDS_HOISTING != 0 && r.NEEDS_HOISTING != 1) errors.Add("NEEDS_HOISTING 選項不正確");
            if (r.REQUEST_REVIEW != 0 && r.REQUEST_REVIEW != 1) errors.Add("REQUEST_REVIEW 選項不正確");
            if (errors.Count > 0) throw new InvalidOperationException(String.Join("；", errors));
            if (!(r.CATEGORY == "general" && r.TRANSPORT_TYPE == "演訓")) r.EXERCISE_PLAN = null;
            // 提示欄位不作為駕駛資格、危險品或加班的自動派車限制。
        }
        private static bool HasOption(IList<CARGO_D_OPTION> options, string kind, string code)
        { return options.Any(x => x.OPTION_TYPE == kind && x.CODE == code && x.ACTIVE == 1); }
        private static void CheckLength(List<string> errors, string value, int max, string label)
        { if (value != null && value.Length > max) errors.Add(label + "長度不可超過 " + max + " 字"); }
        public static void ValidateReturn(CARGO_D_REQUEST r, DateTime? returnAt)
        {
            if (!CanReturn(r)) throw new InvalidOperationException("僅派車簽審已通過且無待確認歸還申請的單據可提出提前歸還");
            if (!returnAt.HasValue || returnAt <= r.START_AT || returnAt >= r.END_AT)
                throw new InvalidOperationException("提前歸還時間須晚於用車起時、早於目前結束時間");
        }
    }
}

