using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
namespace CARGO.Service {
    internal sealed class CARGO_DWorkState {
        public List<CARGO_D01DetailDto> Records = new List<CARGO_D01DetailDto>();
        public List<CARGO_D_RESOURCE> Resources = new List<CARGO_D_RESOURCE>();
        public List<CARGO_D_RESOURCE_BLOCK> Blocks = new List<CARGO_D_RESOURCE_BLOCK>();
        public List<CARGO_D_APPROVER> Approvers = new List<CARGO_D_APPROVER>();
        public List<CARGO_D_SIGN> Signs = new List<CARGO_D_SIGN>();
    }
    internal static class CARGO_DWorkflowRules {
        internal static void Demand(CARGO_DStaffDto actor, int module) {
            if (actor == null || String.IsNullOrWhiteSpace(actor.UserId) || actor.UserId.Length > 200 ||
                !(module == 2 ? actor.CanApprove : module == 3 ? actor.CanDispatch : module == 4 && actor.CanSign))
                throw new UnauthorizedAccessException("沒有此功能的操作權限");
        }
        internal static bool InScope(CARGO_D_REQUEST r, CARGO_DWorkState state, CARGO_DStaffDto actor, int module) {
            if (module == 2) return r.STATUS != "draft" && state.Approvers.Any(x => x.ACTIVE == 1 && x.OWNER_ID == r.OWNER_ID && x.APPROVER_ID == actor.UserId);
            if (module == 3) return new[] { "approved", "dispatched", "noVehicle" }.Contains(r.STATUS);
            return state.Signs.Any(x => x.REQUEST_ID == r.REQUEST_ID);
        }
        internal static bool Overlap(DateTime a, DateTime b, DateTime c, DateTime d) { return a < b && c < d && a < d && c < b; }
        private static DateTime FloorTime(DateTime at, long precision) { return at.AddTicks(-(at.Ticks % precision)); }
        private static DateTime CeilingTime(DateTime at, long precision) {
            var remainder = at.Ticks % precision;
            return remainder == 0 ? at : at.AddTicks(precision - remainder);
        }
        internal static DateTime? DefaultEffectiveAt(CARGO_D01DetailDto d, DateTime now) {
            var r = d.Application.Request;
            var last = d.Assignments.OrderBy(x => x.LINE_NO).LastOrDefault();
            var from = last == null ? r.START_AT.Value : last.FROM_AT;
            var end = r.END_AT.Value;
            var effective = now > from ? now : from;
            var latest = end.AddMinutes(-1);
            if (effective > latest) effective = latest;
            if (effective < from) effective = from;
            // Prefer a valid minute, preserving the usual one-minute margin when possible.
            var firstMinute = CeilingTime(from, TimeSpan.TicksPerMinute);
            if (firstMinute < end) {
                var minute = CeilingTime(effective, TimeSpan.TicksPerMinute);
                var latestMinute = FloorTime(latest, TimeSpan.TicksPerMinute);
                if (minute > latestMinute) minute = latestMinute;
                return minute < firstMinute ? firstMinute : minute;
            }
            // A short final segment may have no valid minute; HTML can retain milliseconds.
            var precise = CeilingTime(effective, TimeSpan.TicksPerMillisecond);
            var lastMillisecond = FloorTime(end.AddTicks(-1), TimeSpan.TicksPerMillisecond);
            if (precise > lastMillisecond) precise = lastMillisecond;
            return precise >= from ? precise : (DateTime?)null;
        }
        internal static string Busy(CARGO_DWorkState state, string id, DateTime from, DateTime to, long except) {
            var block = state.Blocks.FirstOrDefault(x => x.RESOURCE_ID == id && Overlap(from, to, x.FROM_AT, x.TO_AT));
            if (block != null) return block.SOURCE + "：" + block.REASON;
            foreach (var other in state.Records.Where(x => x.Application.Request.REQUEST_ID != except && x.Application.Request.STATUS == "dispatched"))
                if (other.Assignments.Any(x => (x.VEHICLE == id || x.DRIVER1 == id || x.DRIVER2 == id) && Overlap(from, to, x.FROM_AT, x.TO_AT)))
                    return other.Application.Request.REQUEST_NO + " 已佔用（含待簽審）";
            return null;
        }
        internal static List<CARGO_DCandidateDto> Candidates(CARGO_DWorkState state, CARGO_D_REQUEST r, DateTime from, DateTime to) {
            return state.Resources.Where(x => x.ACTIVE == 1 && x.POOL == "BIZ").Select(x => new CARGO_DCandidateDto {
                Resource = x, UnavailableReason = x.KIND == "VEHICLE" && ((r.NEED_K == 1 && x.PERMIT_K != 1) || (r.NEED_P == 1 && x.PERMIT_P != 1)) ? "未同時持有所需 K／P 證" :
                    x.KIND == "VEHICLE" && x.SEATS < r.PAX ? "座位不足" : Busy(state, x.RESOURCE_ID, from, to, r.REQUEST_ID)
            }).ToList();
        }
        internal static void Log(CARGO_D01DetailDto d, string action, string who, string note) {
            d.History.Add(new CARGO_D_LOG { REQUEST_ID = d.Application.Request.REQUEST_ID, ACTION = action, ACTOR = who, NOTE = note, CREATED_AT = DateTime.Now });
        }
        private static void Notice(CARGO_D01DetailDto d, string kind, string text) {
            // Notification record only; SMTP delivery must be wired by the host application.
            d.Notices.Add(new CARGO_D_NOTICE { REQUEST_ID = d.Application.Request.REQUEST_ID, LINE_NO = d.Notices.Count + 1, RECIPIENT = d.Application.Request.OWNER_ID,
                KIND = kind + "（待寄送）", MESSAGE = text.Length > 1000 ? text.Substring(0, 1000) : text });
        }
        private static void Live(CARGO_D_REQUEST r) { if (r.STATUS != "dispatched" || r.SIGN_STATUS != "approved") throw new InvalidOperationException("僅簽審生效的派車可異動"); }
        private static string[] Drivers(CARGO_DWorkActionDto a) { return new[] { a.Driver1, a.Driver2 }.Where(x => !String.IsNullOrEmpty(x)).ToArray(); }
        private static void CheckResources(CARGO_DWorkState state, CARGO_D_REQUEST r, CARGO_DWorkActionDto a, DateTime from, DateTime to, bool checkVehicle = true) {
            if (String.IsNullOrEmpty(a.Driver1) && !String.IsNullOrEmpty(a.Driver2)) throw new InvalidOperationException("請先選第一位司機");
            if (Drivers(a).Distinct().Count() != Drivers(a).Length) throw new InvalidOperationException("司機不可重複指派");
            var candidates = Candidates(state, r, from, to);
            foreach (var pair in new[] { new { Id = a.Vehicle, Kind = "VEHICLE" }, new { Id = a.Driver1, Kind = "DRIVER" }, new { Id = a.Driver2, Kind = "DRIVER" } }) {
                if (pair.Kind == "VEHICLE" && !checkVehicle) continue;
                if (pair.Kind == "DRIVER" && String.IsNullOrEmpty(pair.Id)) continue;
                var found = candidates.FirstOrDefault(x => x.Resource.RESOURCE_ID == pair.Id && x.Resource.KIND == pair.Kind);
                if (found == null || found.UnavailableReason != null) throw new InvalidOperationException("資源不可用：" + pair.Id + " " + (found == null ? "不在商務資源池" : found.UnavailableReason));
            }
        }
        internal static void Apply(CARGO_DWorkState state, CARGO_D01DetailDto d, CARGO_DWorkActionDto a, CARGO_DStaffDto actor, int module) {
            Demand(actor, module); var r = d.Application.Request;
            if (!InScope(r, state, actor, module)) throw new UnauthorizedAccessException("此申請不在你的處理範圍");
            if (r.VERSION_NO != a.Version) throw new InvalidOperationException("資料已更新，請重新開啟後操作");
            a.Note = (a.Note ?? "").Trim();
            if (a.Note.Length > 1000) throw new InvalidOperationException("意見最多 1000 字");
            string action;
            if (module == 2) {
                if (r.STATUS != "submitted" || (a.Command != "approve" && a.Command != "reject")) throw new InvalidOperationException("僅待二級審可審核");
                if (a.Command == "reject" && a.Note.Length == 0) throw new InvalidOperationException("退回修編時意見必填");
                r.STATUS = a.Command == "approve" ? "approved" : "rejected";
                r.APPROVER_NAME = actor.UserId; r.REVIEW_NOTE = a.Note; action = a.Command == "approve" ? "單位主管核准" : "單位主管退回修編";
            } else if (module == 4) {
                var sign = state.Signs.Where(x => x.REQUEST_ID == r.REQUEST_ID).OrderByDescending(x => x.ROUND_NO).FirstOrDefault();
                if (r.STATUS != "dispatched" || r.SIGN_STATUS != "pending" || sign == null || sign.STATUS != "pending" || (a.Command != "approve" && a.Command != "reject")) throw new InvalidOperationException("沒有待簽審的派車結果");
                if (a.Command == "reject" && a.Note.Length == 0) throw new InvalidOperationException("不同意時簽審意見必填");
                sign.STATUS = r.SIGN_STATUS = a.Command == "approve" ? "approved" : "rejected";
                sign.DECIDED_AT = DateTime.Now; sign.DECIDED_BY = actor.UserId; sign.NOTE = a.Note;
                action = a.Command == "approve" ? "運輸主管同意" : "運輸主管退回";
                if (a.Command == "reject") { r.STATUS = "approved"; r.OUTCOME = null; r.VEHICLE = r.DRIVER1 = r.DRIVER2 = null; r.DISPATCHED_AT = null; r.DISPATCHED_BY = r.DISPATCH_NOTE = null; d.Assignments.Clear(); }
                else Notice(d, "派車結果", sign.SUMMARY);
            } else {
                action = Dispatch(state, d, a, actor);
            }
            r.VERSION_NO++; r.UPDATED_AT = DateTime.Now;
            Log(d, action, actor.UserId, a.Note);
        }
        private static string Dispatch(CARGO_DWorkState state, CARGO_D01DetailDto d, CARGO_DWorkActionDto a, CARGO_DStaffDto actor) {
            var r = d.Application.Request; var start = r.START_AT.Value; var end = r.END_AT.Value;
            if (a.Command == "dispatch" || a.Command == "noVehicle") {
                if (r.STATUS != "approved") throw new InvalidOperationException("僅單位主管核准後可派車");
                var none = a.Command == "noVehicle";
                if (!none) {
                    CheckResources(state, r, a, start, end);
                    if (Drivers(a).Length == 0) {
                        if (Candidates(state, r, start, end).Any(x => x.Resource.KIND == "DRIVER" && x.UnavailableReason == null)) throw new InvalidOperationException("尚有可派司機，應派車＋派司機");
                        none = r.SELF_DRIVE != 1;
                    }
                }
                r.DISPATCHED_AT = DateTime.Now; r.DISPATCHED_BY = actor.UserId; r.DISPATCH_NOTE = a.Note;
                d.Assignments.Clear();
                if (none) {
                    r.STATUS = "noVehicle"; r.OUTCOME = "noVehicle"; r.SIGN_STATUS = null; r.VEHICLE = r.DRIVER1 = r.DRIVER2 = null;
                    Notice(d, "派車結果", "無車退回：" + a.Note); return "無車退回";
                }
                r.STATUS = "dispatched"; r.SIGN_STATUS = "pending";
                if (String.IsNullOrEmpty(r.DISPATCH_NO)) r.DISPATCH_NO = "GD" + r.REQUEST_ID.ToString("D6");
                Assign(d, a, start, end, actor.UserId, "原始指派");
                var round = state.Signs.Where(x => x.REQUEST_ID == r.REQUEST_ID).Select(x => x.ROUND_NO).DefaultIfEmpty(0).Max() + 1;
                state.Signs.Add(new CARGO_D_SIGN { REQUEST_ID = r.REQUEST_ID, ROUND_NO = round, STATUS = "pending", SUBMITTED_BY = actor.UserId, SUBMITTED_AT = DateTime.Now,
                    SUMMARY = r.VEHICLE + "／" + (String.IsNullOrEmpty(r.DRIVER1) ? "使用者自駕" : r.DRIVER1 + " " + r.DRIVER2) + "；" + start.ToString("yyyy-MM-dd HH:mm") + "～" + end.ToString("yyyy-MM-dd HH:mm") });
                return "派車結果送簽審";
            }
            Live(r);
            var last = d.Assignments.OrderBy(x => x.LINE_NO).LastOrDefault();
            if (last == null) throw new InvalidOperationException("缺少有效指派區間");
            if (a.Command == "reassign") {
                if (a.SupplementDriver && (!String.IsNullOrEmpty(last.DRIVER1) || !String.IsNullOrEmpty(last.DRIVER2) || r.CONTINUE_MATCHING != 1)) throw new InvalidOperationException("此案件不適用補派司機");
                if (a.SupplementDriver && String.IsNullOrEmpty(a.Driver1)) throw new InvalidOperationException("補派司機至少需選擇司機 1");
                if (!a.EffectiveAt.HasValue || a.EffectiveAt < last.FROM_AT || a.EffectiveAt >= end) throw new InvalidOperationException("生效時間須介於目前指派區間起點與用車結束之間");
                if (a.Vehicle == last.VEHICLE && a.Driver1 == last.DRIVER1 && a.Driver2 == last.DRIVER2) throw new InvalidOperationException("車輛與司機皆未變更");
                if (Drivers(a).Length == 0 && r.SELF_DRIVE != 1) throw new InvalidOperationException("未勾選自駕，至少需一位司機");
                if (String.IsNullOrEmpty(last.DRIVER1) && Drivers(a).Length > 0 && r.CONTINUE_MATCHING != 1) throw new InvalidOperationException("未同意等待駕駛媒合，不可補派司機");
                CheckResources(state, r, a, a.EffectiveAt.Value, end, a.Vehicle != last.VEHICLE);
                var kinds = new List<string>();
                if (a.Vehicle != last.VEHICLE) kinds.Add("換車");
                var previousDrivers = new[] { last.DRIVER1, last.DRIVER2 }.Where(x => !String.IsNullOrEmpty(x)).ToArray();
                if (previousDrivers.Length == 0 && Drivers(a).Length > 0) kinds.Add("補派司機");
                else if (a.Driver1 != last.DRIVER1 || a.Driver2 != last.DRIVER2) kinds.Add(Drivers(a).Length > previousDrivers.Length && previousDrivers.All(x => Drivers(a).Contains(x)) ? "加派司機" : "換司機");
                var kind = String.Join("＋", kinds);
                last.TO_AT = a.EffectiveAt.Value;
                Assign(d, a, a.EffectiveAt.Value, end, actor.UserId, kind);
                Notice(d, kind, a.EffectiveAt.Value.ToString("yyyy-MM-dd HH:mm") + " 起：" + a.Vehicle + "／" + (Drivers(a).Length == 0 ? "使用者自駕" : String.Join("、", Drivers(a))) + (String.IsNullOrEmpty(a.Note) ? "" : "（" + a.Note + "）")); return kind;
            }
            if (a.Command == "extend") {
                if (!a.EndAt.HasValue || a.EndAt <= end) throw new InvalidOperationException("展延時間須晚於目前結束時間");
                var selected = new CARGO_DWorkActionDto { Vehicle = last.VEHICLE, Driver1 = last.DRIVER1, Driver2 = last.DRIVER2 };
                foreach (var id in new[] { selected.Vehicle, selected.Driver1, selected.Driver2 }.Where(x => !String.IsNullOrEmpty(x))) {
                    var busy = Busy(state, id, end, a.EndAt.Value, r.REQUEST_ID);
                    if (busy != null) throw new InvalidOperationException("展延期間資源不可用：" + id + " " + busy);
                }
                last.TO_AT = a.EndAt.Value; r.END_AT = a.EndAt;
                if (r.PENDING_RETURN_AT.HasValue) Log(d, "撤銷提前歸還申請", actor.UserId, "因展延一併撤銷");
                r.PENDING_RETURN_AT = r.RETURN_REQUESTED_AT = null; r.RETURN_REASON = null;
                Notice(d, "展延", end.ToString("yyyy-MM-dd HH:mm") + " → " + a.EndAt.Value.ToString("yyyy-MM-dd HH:mm")); return "展延";
            }
            if (a.Command == "returnApprove" || a.Command == "returnReject") {
                if (!r.PENDING_RETURN_AT.HasValue) throw new InvalidOperationException("沒有待確認的提前歸還");
                if (a.Command == "returnApprove") {
                    var to = r.PENDING_RETURN_AT.Value;
                    if (to <= start || to >= end) throw new InvalidOperationException("提前歸還時間已不適用，請退回重送");
                    r.END_AT = to;
                    d.Assignments.RemoveAll(x => x.FROM_AT >= to);
                    foreach (var segment in d.Assignments.Where(x => x.TO_AT > to)) segment.TO_AT = to;
                    var current = d.Assignments.Last(); r.VEHICLE = current.VEHICLE; r.DRIVER1 = current.DRIVER1; r.DRIVER2 = current.DRIVER2;
                    r.OUTCOME = String.IsNullOrEmpty(current.DRIVER1) ? "selfDrive" : "withDriver";
                }
                r.PENDING_RETURN_AT = r.RETURN_REQUESTED_AT = null; r.RETURN_REASON = null;
                Notice(d, "提前歸還", a.Command == "returnApprove" ? "已確認，新結束時間：" + r.END_AT.Value.ToString("yyyy-MM-dd HH:mm") : "未同意，維持原期間");
                return a.Command == "returnApprove" ? "確認提前歸還" : "退回提前歸還";
            }
            throw new InvalidOperationException("不支援的操作");
        }
        private static void Assign(CARGO_D01DetailDto d, CARGO_DWorkActionDto a, DateTime from, DateTime to, string who, string kind) {
            var r = d.Application.Request; r.VEHICLE = a.Vehicle; r.DRIVER1 = a.Driver1; r.DRIVER2 = a.Driver2;
            r.OUTCOME = Drivers(a).Length == 0 ? "selfDrive" : "withDriver";
            d.Assignments.Add(new CARGO_D_ASSIGNMENT { REQUEST_ID = r.REQUEST_ID, LINE_NO = d.Assignments.Count + 1, FROM_AT = from, TO_AT = to,
                VEHICLE = a.Vehicle, DRIVER1 = a.Driver1, DRIVER2 = a.Driver2, KIND = kind, REASON = a.Note, ACTOR = who });
        }
    }
}
