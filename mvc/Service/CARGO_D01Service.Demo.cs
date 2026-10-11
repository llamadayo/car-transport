#if LOCAL_DEMO
using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;

namespace CARGO.Service
{
    // Same public contract as the Oracle service; only included in the explicit demo build.
    public sealed class CARGO_D01Service
    {
        private static void CheckActor(CARGO_DActorDto actor)
        {
            if (actor == null || String.IsNullOrWhiteSpace(actor.UserId)) throw new UnauthorizedAccessException("缺少使用者身分");
        }
        private static CARGO_D01DetailDto Find(long id, CARGO_DActorDto actor)
        {
            CARGO_D01DetailDto item;
            return CARGO_D01DemoData.Records.TryGetValue(id, out item) && item.Application.Request.OWNER_ID == actor.UserId ? item : null;
        }
        public CARGO_D01PageDto Query(CARGO_D01QueryDto query, CARGO_DActorDto actor)
        {
            CheckActor(actor); query = query ?? new CARGO_D01QueryDto();
            lock (CARGO_D01DemoData.Sync)
            {
                var rows = CARGO_D01DemoData.Records.Values.Where(d => d.Application.Request.OWNER_ID == actor.UserId)
                    .Select(d => new CARGO_D01ListItemDto {
                        REQUEST_ID = d.Application.Request.REQUEST_ID, REQUEST_NO = d.Application.Request.REQUEST_NO,
                        CATEGORY = d.Application.Request.CATEGORY, APPLICANT = d.Application.Request.APPLICANT,
                        START_AT = d.Application.Request.START_AT.Value, END_AT = d.Application.Request.END_AT.Value,
                        PAX = d.Application.Request.PAX, SELF_DRIVE = d.Application.Request.SELF_DRIVE.Value,
                        CREATED_AT = d.Application.Request.CREATED_AT, OUTCOME = d.Application.Request.OUTCOME,
                        DISPLAY_STATUS = CARGO_D01Rules.DisplayStatus(d.Application.Request, DateTime.Now),
                        SIGN_STATUS = d.Application.Request.SIGN_STATUS, PENDING_RETURN_AT = d.Application.Request.PENDING_RETURN_AT, HAZARDOUS_CARGO = d.Application.Request.HAZARDOUS_CARGO,
                        CARGO_COUNT = d.Application.Cargo.Count, CARGO_QUANTITY = d.Application.Cargo.Sum(c => c.QUANTITY)
                    }).Where(r => (String.IsNullOrEmpty(query.Applicant) || r.APPLICANT.Contains(query.Applicant))
                        && (!query.UseDate.HasValue || (r.START_AT < query.UseDate.Value.Date.AddDays(1) && r.END_AT >= query.UseDate.Value.Date))
                        && (String.IsNullOrEmpty(query.Status) || r.DISPLAY_STATUS == query.Status)).ToList();
                Func<CARGO_D01ListItemDto, object> key;
                switch (query.SortField) {
                    case "RequestNo": key = r => r.REQUEST_NO; break;
                    case "Applicant": key = r => r.APPLICANT; break;
                    case "StartAt": key = r => r.START_AT; break;
                    case "EndAt": key = r => r.END_AT; break;
                    case "Passengers": key = r => r.PAX; break;
                    default: key = r => r.CREATED_AT; break;
                }
                var sorted = query.Descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
                var size = Math.Min(100, Math.Max(1, query.PageSize));
                var skip = Math.Min(Int32.MaxValue, ((long)Math.Max(1, query.Page) - 1) * size);
                return new CARGO_D01PageDto { Total = rows.Count, Items = sorted.ThenByDescending(r => r.REQUEST_ID).Skip((int)skip).Take(size).ToList() };
            }
        }
        public CARGO_D01DetailDto Detail(long id, CARGO_DActorDto actor)
        {
            CheckActor(actor);
            lock (CARGO_D01DemoData.Sync) {
                var item = Find(id, actor);
                if (item == null) return null;
                var detail = CARGO_D01DemoData.Copy(item);
                var r = detail.Application.Request;
                detail.ReviewApprover = r.STATUS == "submitted" ? String.Join("、", CARGO_D01DemoData.Approvers()
                    .Where(x => x.OWNER_ID == r.OWNER_ID && x.ACTIVE == 1).Select(x => x.APPROVER_ID).Distinct().OrderBy(x => x)) : r.APPROVER_NAME;
                return detail;
            }
        }
        public long Save(CARGO_D01SaveDto data, CARGO_DActorDto actor, bool submit)
        {
            CheckActor(actor);
            var input = CARGO_D01DemoData.Copy(data);
            CARGO_D01Rules.Validate(input, actor, CARGO_D01DemoData.Options(), submit);
            lock (CARGO_D01DemoData.Sync)
            {
                var r = input.Request;
                var isNew = r.REQUEST_ID == 0;
                var old = isNew ? null : Find(r.REQUEST_ID, actor);
                if (!isNew && (old == null || !CARGO_D01Rules.CanEdit(old.Application.Request))) throw new InvalidOperationException("查無可編輯的申請單");
                if (old != null && old.Application.Request.VERSION_NO != r.VERSION_NO) throw new InvalidOperationException("申請單已被更新，請重新開啟後再操作");
                if (old != null && !submit && old.Application.Request.STATUS != "draft") throw new InvalidOperationException("退回修編請修改後重新送出，不可暫存");
                if (old != null && old.Application.Request.DISPATCHED_AT.HasValue) throw new InvalidOperationException("調度已確認，不可修改");
                var oldRequest = old == null ? null : old.Application.Request;
                r.REQUEST_ID = isNew ? ++CARGO_D01DemoData.Sequence : r.REQUEST_ID;
                r.REQUEST_NO = isNew ? "GU" + r.REQUEST_ID.ToString("D6") : oldRequest.REQUEST_NO;
                r.OWNER_ID = actor.UserId;
                r.CREATED_AT = isNew ? DateTime.Now : oldRequest.CREATED_AT;
                r.UPDATED_AT = DateTime.Now; r.VERSION_NO = isNew ? 1 : oldRequest.VERSION_NO + 1;
                r.STATUS = submit ? "submitted" : "draft";
                r.REVIEW_NOTE = null; r.DISPATCHED_AT = null; r.SIGN_STATUS = null; r.OUTCOME = null;
                r.PENDING_RETURN_AT = null; r.RETURN_REQUESTED_AT = null; r.RETURN_REASON = null;
                r.USAGE_AT = null; r.VEHICLE = null; r.DRIVER1 = null; r.DRIVER2 = null; r.DISPATCH_NO = null;
                r.DISPATCH_NOTE = null; r.DISPATCHED_BY = null; r.APPROVER_NAME = oldRequest == null ? null : oldRequest.APPROVER_NAME;
                for (var i = 0; i < input.Locations.Count; i++) { input.Locations[i].REQUEST_ID = r.REQUEST_ID; input.Locations[i].LINE_NO = i + 1; }
                for (var i = 0; i < input.SelfDrivers.Count; i++) { input.SelfDrivers[i].REQUEST_ID = r.REQUEST_ID; input.SelfDrivers[i].LINE_NO = i + 1; }
                for (var i = 0; i < input.Cargo.Count; i++) { input.Cargo[i].REQUEST_ID = r.REQUEST_ID; input.Cargo[i].LINE_NO = i + 1; }
                var item = old == null ? CARGO_D01DemoData.NewDetail(input) : CARGO_D01DemoData.Copy(old);
                item.Application = input;
                if (oldRequest != null && oldRequest.STATUS == "rejected" && !String.IsNullOrEmpty(oldRequest.REVIEW_NOTE))
                    CARGO_D01DemoData.AddLog(item, "重送前退回修編意見", actor.UserId, oldRequest.REVIEW_NOTE);
                CARGO_D01DemoData.AddLog(item, submit ? (isNew ? "送出申請" : "修改後重新送出") : (isNew ? "暫存申請" : "暫存修改"), actor.UserId, r.CHANGE_REASON);
                CARGO_D01DemoData.Records[r.REQUEST_ID] = item;
                return r.REQUEST_ID;
            }
        }
        public void Withdraw(CARGO_D01ActionDto input, CARGO_DActorDto actor)
        {
            CheckActor(actor);
            lock (CARGO_D01DemoData.Sync) {
                var item = Find(input.RequestId, actor);
                if (item == null || !CARGO_D01Rules.CanWithdraw(item.Application.Request)) throw new InvalidOperationException("僅調度確認前的申請可撤回修改");
                var r = item.Application.Request;
                if (r.VERSION_NO != input.Version) throw new InvalidOperationException("申請單已變更，請重新整理");
                var original = r.STATUS;
                r.STATUS = "draft"; r.REVIEW_NOTE = null; r.VERSION_NO++; r.UPDATED_AT = DateTime.Now;
                CARGO_D01DemoData.AddLog(item, "撤回修改", actor.UserId, "原狀態：" + original);
            }
        }
        public void RequestEarlyReturn(CARGO_D01ActionDto input, CARGO_DActorDto actor)
        {
            CheckActor(actor);
            lock (CARGO_D01DemoData.Sync) {
                var item = Find(input.RequestId, actor);
                if (item == null) throw new InvalidOperationException("查無申請單");
                var r = item.Application.Request;
                CARGO_D01Rules.ValidateReturn(r, input.ReturnAt);
                if (r.VERSION_NO != input.Version) throw new InvalidOperationException("申請單已變更，請重新整理");
                if (input.Reason != null && input.Reason.Length > 1000) throw new InvalidOperationException("歸還原因長度不可超過 1000 字");
                r.PENDING_RETURN_AT = input.ReturnAt; r.RETURN_REASON = input.Reason; r.RETURN_REQUESTED_AT = DateTime.Now;
                r.VERSION_NO++; r.UPDATED_AT = DateTime.Now;
                CARGO_D01DemoData.AddLog(item, "提出提前歸還", actor.UserId, "新結束時間：" + input.ReturnAt.Value.ToString("yyyy-MM-dd HH:mm") + "；" + input.Reason);
            }
        }
    }
}
#endif
