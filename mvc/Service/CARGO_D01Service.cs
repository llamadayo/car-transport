#if !LOCAL_DEMO
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using Dapper;

namespace CARGO.Service
{
    public sealed class CARGO_D01Service
    {
        private const string DisplayStatusSql = @"CASE
          WHEN r.STATUS = 'draft' THEN 'draft'
          WHEN r.STATUS = 'submitted' THEN 'review'
          WHEN r.STATUS = 'rejected' THEN 'revise'
          WHEN r.STATUS = 'approved' THEN 'todo'
          WHEN r.STATUS = 'noVehicle' THEN 'noCar'
          WHEN r.STATUS = 'dispatched' AND NVL(r.SIGN_STATUS, 'pending') <> 'approved' THEN 'signing'
          WHEN r.STATUS = 'dispatched' AND r.USAGE_AT IS NOT NULL THEN 'logged'
          WHEN r.STATUS = 'dispatched' AND r.START_AT <= :NowAt THEN 'departed'
          WHEN r.STATUS = 'dispatched' THEN 'ready' ELSE 'unknown' END";
        private static void CheckActor(CARGO_DActorDto actor)
        {
            if (actor == null || String.IsNullOrWhiteSpace(actor.UserId) || actor.UserId.Length > 200)
                throw new UnauthorizedAccessException("請由公司登入機制提供有效使用者身分");
        }
        public CARGO_D01PageDto Query(CARGO_D01QueryDto query, CARGO_DActorDto actor)
        {
            CheckActor(actor);
            query = query ?? new CARGO_D01QueryDto();
            var page = Math.Max(1, query.Page);
            var size = Math.Min(100, Math.Max(1, query.PageSize));
            var columns = new Dictionary<string, string> {
                { "RequestNo", "r.REQUEST_NO" }, { "Applicant", "r.APPLICANT" },
                { "StartAt", "r.START_AT" }, { "EndAt", "r.END_AT" },
                { "CreatedAt", "r.CREATED_AT" }, { "Passengers", "r.PAX" }
            };
            string sort;
            if (!columns.TryGetValue(query.SortField ?? "", out sort)) sort = "r.CREATED_AT";
            var where = @" WHERE r.OWNER_ID = :OwnerId
                AND (:Applicant IS NULL OR INSTR(r.APPLICANT, :Applicant) > 0)
                AND (:UseDate IS NULL OR (r.START_AT < :DayAfter AND r.END_AT >= :UseDate))";
            if (!String.IsNullOrEmpty(query.Status)) where += " AND (" + DisplayStatusSql + ") = :Status";
            var args = new { OwnerId = actor.UserId, Applicant = query.Applicant, UseDate = query.UseDate.HasValue ? query.UseDate.Value.Date : (DateTime?)null,
                DayAfter = query.UseDate.HasValue ? query.UseDate.Value.Date.AddDays(1) : (DateTime?)null,
                Status = query.Status, NowAt = DateTime.Now, Skip = ((long)page - 1) * size, Take = size };
            using (var db = CARGO_DDatabase.Open())
            {
                var count = db.Query<int>("SELECT COUNT(*) FROM CARGO_D_REQUEST r" + where, CARGO_DDatabase.Params(args)).Single();
                var sql = @"SELECT r.REQUEST_ID, r.REQUEST_NO, r.CATEGORY, r.APPLICANT, r.START_AT, r.END_AT, r.PAX, r.SELF_DRIVE,
                    r.OUTCOME, r.CREATED_AT, r.SIGN_STATUS, r.PENDING_RETURN_AT, r.HAZARDOUS_CARGO, (" + DisplayStatusSql + @") DISPLAY_STATUS,
                    (SELECT COUNT(*) FROM CARGO_D_CARGO c WHERE c.REQUEST_ID = r.REQUEST_ID) CARGO_COUNT,
                    (SELECT NVL(SUM(c.QUANTITY),0) FROM CARGO_D_CARGO c WHERE c.REQUEST_ID = r.REQUEST_ID) CARGO_QUANTITY
                    FROM CARGO_D_REQUEST r" + where + " ORDER BY " + sort + (query.Descending ? " DESC" : " ASC") +
                    ", r.REQUEST_ID DESC OFFSET :Skip ROWS FETCH NEXT :Take ROWS ONLY";
                return new CARGO_D01PageDto { Total = count, Items = db.Query<CARGO_D01ListItemDto>(sql, CARGO_DDatabase.Params(args)).ToList() };
            }
        }
        private static CARGO_D_REQUEST Read(IDbConnection db, long id, string owner, IDbTransaction tx = null)
        {
            return db.Query<CARGO_D_REQUEST>("SELECT * FROM CARGO_D_REQUEST WHERE REQUEST_ID = :Id AND OWNER_ID = :OwnerId",
                CARGO_DDatabase.Params(new { Id = id, OwnerId = owner }), tx).SingleOrDefault();
        }
        public CARGO_D01DetailDto Detail(long id, CARGO_DActorDto actor)
        {
            CheckActor(actor);
            using (var db = CARGO_DDatabase.Open())
            {
                var r = Read(db, id, actor.UserId);
                if (r == null) return null;
                var args = new { Id = id };
                return new CARGO_D01DetailDto {
                    ReviewApprover = r.STATUS == "submitted" ? String.Join("、", db.Query<string>("SELECT DISTINCT APPROVER_ID FROM CARGO_D_APPROVER WHERE OWNER_ID=:OwnerId AND ACTIVE=1 ORDER BY APPROVER_ID", CARGO_DDatabase.Params(new { OwnerId = r.OWNER_ID }))) : r.APPROVER_NAME,
                    Application = new CARGO_D01SaveDto {
                        Request = r,
                        Locations = db.Query<CARGO_D_LOCATION>("SELECT * FROM CARGO_D_LOCATION WHERE REQUEST_ID = :Id ORDER BY LINE_NO", CARGO_DDatabase.Params(args)).ToList(),
                        SelfDrivers = db.Query<CARGO_D_SELF_DRIVER>("SELECT * FROM CARGO_D_SELF_DRIVER WHERE REQUEST_ID = :Id ORDER BY LINE_NO", CARGO_DDatabase.Params(args)).ToList(),
                        Cargo = db.Query<CARGO_D_CARGO>("SELECT * FROM CARGO_D_CARGO WHERE REQUEST_ID = :Id ORDER BY LINE_NO", CARGO_DDatabase.Params(args)).ToList()
                    },
                    History = db.Query<CARGO_D_LOG>("SELECT * FROM CARGO_D_LOG WHERE REQUEST_ID = :Id ORDER BY LOG_ID", CARGO_DDatabase.Params(args)).ToList(),
                    Assignments = db.Query<CARGO_D_ASSIGNMENT>("SELECT * FROM CARGO_D_ASSIGNMENT WHERE REQUEST_ID = :Id ORDER BY LINE_NO", CARGO_DDatabase.Params(args)).ToList(),
                    Notices = db.Query<CARGO_D_NOTICE>("SELECT * FROM CARGO_D_NOTICE WHERE REQUEST_ID = :Id ORDER BY LINE_NO", CARGO_DDatabase.Params(args)).ToList()
                };
            }
        }
        public long Save(CARGO_D01SaveDto data, CARGO_DActorDto actor, bool submit)
        {
            CheckActor(actor);
            CARGO_D01Rules.Validate(data, actor, new CARGO_DService().GetOptions(), submit);
            var r = data.Request;
            r.CATEGORY = r.CATEGORY == null ? null : r.CATEGORY.Trim();
            r.TRANSPORT_TYPE = r.TRANSPORT_TYPE == null ? null : r.TRANSPORT_TYPE.Trim();
            r.APPLICANT = r.APPLICANT == null ? null : r.APPLICANT.Trim();
            r.DEPT = r.DEPT == null ? null : r.DEPT.Trim();
            r.EXT = r.EXT == null ? null : r.EXT.Trim();
            r.PLAN_CODE = r.PLAN_CODE == null ? null : r.PLAN_CODE.Trim();
            r.CAMPUS = r.CAMPUS == null ? null : r.CAMPUS.Trim();
            r.PREFERENCE1 = r.PREFERENCE1 == null ? null : r.PREFERENCE1.Trim();
            r.PREFERENCE2 = r.PREFERENCE2 == null ? null : r.PREFERENCE2.Trim();
            r.PREFERENCE3 = r.PREFERENCE3 == null ? null : r.PREFERENCE3.Trim();
            r.EXERCISE_PLAN = r.EXERCISE_PLAN == null ? null : r.EXERCISE_PLAN.Trim();
            r.LEADER = r.LEADER == null ? null : r.LEADER.Trim();
            r.LEADER_EXT = r.LEADER_EXT == null ? null : r.LEADER_EXT.Trim();
            r.LEADER_PHONE = r.LEADER_PHONE == null ? null : r.LEADER_PHONE.Trim();
            r.PICKUP_LOCATION = r.PICKUP_LOCATION == null ? null : r.PICKUP_LOCATION.Trim();
            r.PURPOSE = r.PURPOSE == null ? null : r.PURPOSE.Trim();
            r.TRIPLICATE_NO = r.TRIPLICATE_NO == null ? null : r.TRIPLICATE_NO.Trim();
            r.ESCORT_NO = r.ESCORT_NO == null ? null : r.ESCORT_NO.Trim();
            r.CHANGE_REASON = r.CHANGE_REASON == null ? null : r.CHANGE_REASON.Trim();
            r.REMARKS = r.REMARKS == null ? null : r.REMARKS.Trim();
            using (var db = CARGO_DDatabase.Open())
            using (var tx = db.BeginTransaction())
            {
                var isNew = r.REQUEST_ID == 0;
                var previous = isNew ? null : Read(db, r.REQUEST_ID, actor.UserId, tx);
                if (!isNew && (previous == null || !CARGO_D01Rules.CanEdit(previous)))
                    throw new InvalidOperationException("查無可編輯的申請單");
                if (!isNew && !submit && previous.STATUS != "draft")
                    throw new InvalidOperationException("退回修編請修改後重新送出，不可暫存");
                r.OWNER_ID = actor.UserId;
                r.STATUS = submit ? "submitted" : "draft";
                r.UPDATED_AT = DateTime.Now;
                if (isNew)
                {
                    r.REQUEST_ID = db.Query<long>("SELECT CARGO_D_REQUEST_SEQ.NEXTVAL FROM DUAL", transaction: tx).Single();
                    r.REQUEST_NO = "GU" + r.REQUEST_ID.ToString("D6");
                    r.CREATED_AT = r.UPDATED_AT; r.VERSION_NO = 1;
                    db.Execute(@"INSERT INTO CARGO_D_REQUEST
                        (REQUEST_ID, REQUEST_NO, OWNER_ID, STATUS, VERSION_NO, CREATED_AT, UPDATED_AT, CATEGORY, TRANSPORT_TYPE, APPLICANT, DEPT, EXT, PLAN_CODE, CAMPUS, PREFERENCE1, PREFERENCE2, PREFERENCE3, EXERCISE_PLAN, LEADER, LEADER_EXT, LEADER_PHONE, PICKUP_LOCATION, PURPOSE, TRIPLICATE_NO, ESCORT_NO, CHANGE_REASON, REMARKS, START_AT, END_AT, PAX, SELF_DRIVE, NEED_K, NEED_P, ONE_WAY, ALLOW_MERGE, NIGHT_OT, HOLIDAY_OT, CONTINUE_MATCHING, WILLING_SELF_DRIVE, QUALIFIED_LICENSE, ENTER_TAIPEI, CROSS_CAMPUS, HAS_CARGO, HAZARDOUS_CARGO, PYROTECHNIC_CARGO, NEEDS_HOISTING, REQUEST_REVIEW)
                        VALUES (:REQUEST_ID, :REQUEST_NO, :OWNER_ID, :STATUS, :VERSION_NO, :CREATED_AT, :UPDATED_AT, :CATEGORY, :TRANSPORT_TYPE, :APPLICANT, :DEPT, :EXT, :PLAN_CODE, :CAMPUS, :PREFERENCE1, :PREFERENCE2, :PREFERENCE3, :EXERCISE_PLAN, :LEADER, :LEADER_EXT, :LEADER_PHONE, :PICKUP_LOCATION, :PURPOSE, :TRIPLICATE_NO, :ESCORT_NO, :CHANGE_REASON, :REMARKS, :START_AT, :END_AT, :PAX, :SELF_DRIVE, :NEED_K, :NEED_P, :ONE_WAY, :ALLOW_MERGE, :NIGHT_OT, :HOLIDAY_OT, :CONTINUE_MATCHING, :WILLING_SELF_DRIVE, :QUALIFIED_LICENSE, :ENTER_TAIPEI, :CROSS_CAMPUS, :HAS_CARGO, :HAZARDOUS_CARGO, :PYROTECHNIC_CARGO, :NEEDS_HOISTING, :REQUEST_REVIEW)",
                        CARGO_DDatabase.Params(r), tx);
                }
                else
                {
                    var count = db.Execute(@"UPDATE CARGO_D_REQUEST SET
                        CATEGORY = :CATEGORY,
                        TRANSPORT_TYPE = :TRANSPORT_TYPE,
                        APPLICANT = :APPLICANT,
                        DEPT = :DEPT,
                        EXT = :EXT,
                        PLAN_CODE = :PLAN_CODE,
                        CAMPUS = :CAMPUS,
                        PREFERENCE1 = :PREFERENCE1,
                        PREFERENCE2 = :PREFERENCE2,
                        PREFERENCE3 = :PREFERENCE3,
                        EXERCISE_PLAN = :EXERCISE_PLAN,
                        LEADER = :LEADER,
                        LEADER_EXT = :LEADER_EXT,
                        LEADER_PHONE = :LEADER_PHONE,
                        PICKUP_LOCATION = :PICKUP_LOCATION,
                        PURPOSE = :PURPOSE,
                        TRIPLICATE_NO = :TRIPLICATE_NO,
                        ESCORT_NO = :ESCORT_NO,
                        CHANGE_REASON = :CHANGE_REASON,
                        REMARKS = :REMARKS,
                        START_AT = :START_AT,
                        END_AT = :END_AT,
                        PAX = :PAX,
                        SELF_DRIVE = :SELF_DRIVE,
                        NEED_K = :NEED_K,
                        NEED_P = :NEED_P,
                        ONE_WAY = :ONE_WAY,
                        ALLOW_MERGE = :ALLOW_MERGE,
                        NIGHT_OT = :NIGHT_OT,
                        HOLIDAY_OT = :HOLIDAY_OT,
                        CONTINUE_MATCHING = :CONTINUE_MATCHING,
                        WILLING_SELF_DRIVE = :WILLING_SELF_DRIVE,
                        QUALIFIED_LICENSE = :QUALIFIED_LICENSE,
                        ENTER_TAIPEI = :ENTER_TAIPEI,
                        CROSS_CAMPUS = :CROSS_CAMPUS,
                        HAS_CARGO = :HAS_CARGO,
                        HAZARDOUS_CARGO = :HAZARDOUS_CARGO,
                        PYROTECHNIC_CARGO = :PYROTECHNIC_CARGO,
                        NEEDS_HOISTING = :NEEDS_HOISTING,
                        REQUEST_REVIEW = :REQUEST_REVIEW,
                        STATUS = :STATUS, UPDATED_AT = :UPDATED_AT, REVIEW_NOTE = NULL, VERSION_NO = VERSION_NO + 1
                        WHERE REQUEST_ID = :REQUEST_ID AND OWNER_ID = :OWNER_ID AND VERSION_NO = :VERSION_NO
                        AND STATUS IN ('draft','rejected') AND DISPATCHED_AT IS NULL",
                        CARGO_DDatabase.Params(r), tx);
                    if (count != 1) throw new InvalidOperationException("申請單已被更新，請重新開啟後再操作");
                }
                ReplaceChildren(db, tx, r.REQUEST_ID, data);
                var action = submit ? (isNew ? "送出申請" : "修改後重新送出") : (isNew ? "暫存申請" : "暫存修改");
                if (previous != null && previous.STATUS == "rejected" && !String.IsNullOrEmpty(previous.REVIEW_NOTE))
                    Log(db, tx, r.REQUEST_ID, actor.UserId, "重送前退回修編意見", previous.REVIEW_NOTE);
                Log(db, tx, r.REQUEST_ID, actor.UserId, action, r.CHANGE_REASON);
                tx.Commit();
                return r.REQUEST_ID;
            }
        }
        private static void ReplaceChildren(IDbConnection db, IDbTransaction tx, long id, CARGO_D01SaveDto data)
        {

            db.Execute("DELETE FROM CARGO_D_LOCATION WHERE REQUEST_ID = :Id", CARGO_DDatabase.Params(new { Id = id }), tx);
            for (var i = 0; i < data.Locations.Count; i++)
            {
                var row = data.Locations[i]; row.REQUEST_ID = id; row.LINE_NO = i + 1;
                db.Execute("INSERT INTO CARGO_D_LOCATION (REQUEST_ID, LINE_NO, NAME) VALUES (:REQUEST_ID, :LINE_NO, :NAME)", CARGO_DDatabase.Params(row), tx);
            }

            db.Execute("DELETE FROM CARGO_D_SELF_DRIVER WHERE REQUEST_ID = :Id", CARGO_DDatabase.Params(new { Id = id }), tx);
            for (var i = 0; i < data.SelfDrivers.Count; i++)
            {
                var row = data.SelfDrivers[i]; row.REQUEST_ID = id; row.LINE_NO = i + 1;
                db.Execute("INSERT INTO CARGO_D_SELF_DRIVER (REQUEST_ID, LINE_NO, NAME, EXT, PHONE) VALUES (:REQUEST_ID, :LINE_NO, :NAME, :EXT, :PHONE)", CARGO_DDatabase.Params(row), tx);
            }

            db.Execute("DELETE FROM CARGO_D_CARGO WHERE REQUEST_ID = :Id", CARGO_DDatabase.Params(new { Id = id }), tx);
            for (var i = 0; i < data.Cargo.Count; i++)
            {
                var row = data.Cargo[i]; row.REQUEST_ID = id; row.LINE_NO = i + 1;
                db.Execute("INSERT INTO CARGO_D_CARGO (REQUEST_ID, LINE_NO, NAME, LENGTH_CM, WIDTH_CM, HEIGHT_CM, WEIGHT_KG, QUANTITY, CATEGORY, PACKAGING, HAZARDOUS) VALUES (:REQUEST_ID, :LINE_NO, :NAME, :LENGTH_CM, :WIDTH_CM, :HEIGHT_CM, :WEIGHT_KG, :QUANTITY, :CATEGORY, :PACKAGING, :HAZARDOUS)", CARGO_DDatabase.Params(row), tx);
            }
        }
        public void Withdraw(CARGO_D01ActionDto input, CARGO_DActorDto actor)
        {
            CheckActor(actor);
            using (var db = CARGO_DDatabase.Open())
            using (var tx = db.BeginTransaction())
            {
                var r = Read(db, input.RequestId, actor.UserId, tx);
                if (r == null || !CARGO_D01Rules.CanWithdraw(r)) throw new InvalidOperationException("僅調度確認前的申請可撤回修改");
                var count = db.Execute(@"UPDATE CARGO_D_REQUEST SET STATUS = 'draft', REVIEW_NOTE = NULL,
                    VERSION_NO = VERSION_NO + 1, UPDATED_AT = :NowAt
                    WHERE REQUEST_ID = :Id AND OWNER_ID = :OwnerId AND VERSION_NO = :Version
                    AND STATUS IN ('submitted','approved') AND DISPATCHED_AT IS NULL",
                    CARGO_DDatabase.Params(new { Id = input.RequestId, OwnerId = actor.UserId, Version = input.Version, NowAt = DateTime.Now }), tx);
                if (count != 1) throw new InvalidOperationException("申請單已變更，請重新整理");
                Log(db, tx, r.REQUEST_ID, actor.UserId, "撤回修改", "原狀態：" + r.STATUS);
                tx.Commit();
            }
        }
        public void RequestEarlyReturn(CARGO_D01ActionDto input, CARGO_DActorDto actor)
        {
            CheckActor(actor);
            using (var db = CARGO_DDatabase.Open())
            using (var tx = db.BeginTransaction())
            {
                var r = Read(db, input.RequestId, actor.UserId, tx);
                if (r == null) throw new InvalidOperationException("查無申請單");
                CARGO_D01Rules.ValidateReturn(r, input.ReturnAt);
                if (input.Reason != null && input.Reason.Length > 1000) throw new InvalidOperationException("歸還原因長度不可超過 1000 字");
                var count = db.Execute(@"UPDATE CARGO_D_REQUEST SET PENDING_RETURN_AT = :ReturnAt,
                    RETURN_REQUESTED_AT = :NowAt, RETURN_REASON = :Reason, VERSION_NO = VERSION_NO + 1, UPDATED_AT = :NowAt
                    WHERE REQUEST_ID = :Id AND OWNER_ID = :OwnerId AND VERSION_NO = :Version
                    AND STATUS = 'dispatched' AND SIGN_STATUS = 'approved' AND PENDING_RETURN_AT IS NULL
                    AND :ReturnAt > START_AT AND :ReturnAt < END_AT",
                    CARGO_DDatabase.Params(new { Id = input.RequestId, OwnerId = actor.UserId, Version = input.Version,
                        ReturnAt = input.ReturnAt, Reason = input.Reason, NowAt = DateTime.Now }), tx);
                if (count != 1) throw new InvalidOperationException("申請單已變更或已有待確認的提前歸還");
                Log(db, tx, r.REQUEST_ID, actor.UserId, "提出提前歸還", "新結束時間：" + input.ReturnAt.Value.ToString("yyyy-MM-dd HH:mm") + "；" + input.Reason);
                // END_AT remains unchanged until the dispatch feature confirms the request.
                tx.Commit();
            }
        }
        private static void Log(IDbConnection db, IDbTransaction tx, long id, string actor, string action, string note)
        {
            if (note != null && note.Length > 2000) throw new InvalidOperationException("異動紀錄超過可儲存長度");
            db.Execute(@"INSERT INTO CARGO_D_LOG (LOG_ID, REQUEST_ID, ACTION, ACTOR, NOTE, CREATED_AT)
                VALUES (CARGO_D_LOG_SEQ.NEXTVAL, :Id, :Action, :Actor, :Note, :NowAt)",
                CARGO_DDatabase.Params(new { Id = id, Action = action, Actor = actor, Note = note, NowAt = DateTime.Now }), tx);
        }
    }
}
#endif
