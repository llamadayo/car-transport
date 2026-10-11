using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
#if !LOCAL_DEMO
using System.Data;
using Dapper;
#endif
namespace CARGO.Service {
    // Workflow orchestration and SQL live in Service; no repository or injected dependency.
    internal sealed class CARGO_DWorkflowService {
        internal CARGO_DWorkPageDto Query(CARGO_DWorkQueryDto q, CARGO_DStaffDto actor, int module) {
            CARGO_DWorkflowRules.Demand(actor, module);
            #if !LOCAL_DEMO
            return QueryOracle(q, actor, module);
#else
            return Read(state => {
                var rows = state.Records.Select(x => x.Application.Request).Where(r => CARGO_DWorkflowRules.InScope(r, state, actor, module));
                if (!String.IsNullOrWhiteSpace(q.Applicant)) rows = rows.Where(r => (r.APPLICANT ?? "").IndexOf(q.Applicant.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
                if (module == 4 && !String.IsNullOrWhiteSpace(q.Keyword)) rows = rows.Where(r =>
                    new[] { r.REQUEST_NO, r.DISPATCH_NO, r.APPLICANT }.Any(value => (value ?? "").IndexOf(q.Keyword.Trim(), StringComparison.OrdinalIgnoreCase) >= 0));
                if (q.UseDate.HasValue) rows = rows.Where(r => r.START_AT < q.UseDate.Value.Date.AddDays(1) && r.END_AT > q.UseDate.Value.Date);
                if (!String.IsNullOrEmpty(q.Status)) rows = rows.Where(r => module == 4 ? Latest(state, r.REQUEST_ID).STATUS == q.Status : module == 2 ?
                    (q.Status == "passed" ? r.STATUS != "submitted" && r.STATUS != "rejected" : r.STATUS == q.Status) : q.Status == "work" ?
                    r.STATUS == "approved" || r.PENDING_RETURN_AT.HasValue : CARGO_D01Rules.DisplayStatus(r, DateTime.Now) == q.Status);
                var ordered = rows.OrderByDescending(r => module == 3 && r.CATEGORY == "routine").ThenByDescending(r => r.CREATED_AT).ThenByDescending(r => r.REQUEST_ID).ToList();
                var size = Math.Max(1, Math.Min(100, q.PageSize)); var page = Math.Max(1, Math.Min(1000000, q.Page));
                var items = ordered.Skip((page - 1) * size).Take(size).ToList();
                var info = new Dictionary<long, CARGO_D02ListInfoDto>();
                if (module == 2) foreach (var r in items) {
                    var cargo = state.Records.Single(x => x.Application.Request.REQUEST_ID == r.REQUEST_ID).Application.Cargo;
                    info.Add(r.REQUEST_ID, new CARGO_D02ListInfoDto { REQUEST_ID = r.REQUEST_ID,
                        Approver = ReviewApprover(r, state.Approvers), CargoCount = cargo.Count, CargoQuantity = cargo.Sum(x => (long)x.QUANTITY) });
                }
                return new CARGO_DWorkPageDto { Total = ordered.Count, Items = items, ReviewInfo = info };
            });
#endif
        }
        private static string ReviewApprover(CARGO_D_REQUEST r, IEnumerable<CARGO_D_APPROVER> approvers) {
            if (r.STATUS != "submitted" && !String.IsNullOrWhiteSpace(r.APPROVER_NAME)) return r.APPROVER_NAME;
            return String.Join("、", approvers.Where(x => x.OWNER_ID == r.OWNER_ID && x.ACTIVE == 1)
                .Select(x => x.APPROVER_ID).Distinct().OrderBy(x => x));
        }
        private static CARGO_D_SIGN Latest(CARGO_DWorkState state, long id) { return state.Signs.Where(x => x.REQUEST_ID == id).OrderByDescending(x => x.ROUND_NO).First(); }
        internal CARGO_DWorkDetailDto Detail(long id, CARGO_DStaffDto actor, int module) {
            CARGO_DWorkflowRules.Demand(actor, module);
            return Read(state => {
                var d = state.Records.FirstOrDefault(x => x.Application.Request.REQUEST_ID == id);
                if (d == null || !CARGO_DWorkflowRules.InScope(d.Application.Request, state, actor, module)) return null;
                var r = d.Application.Request;
                return new CARGO_DWorkDetailDto { Detail = d, Signs = state.Signs.Where(x => x.REQUEST_ID == id).OrderBy(x => x.ROUND_NO).ToList(),
                    Candidates = module == 3 ? CARGO_DWorkflowRules.Candidates(state, r, r.START_AT.Value, r.END_AT.Value) : new List<CARGO_DCandidateDto>(),
                    EffectiveAt = module == 3 ? CARGO_DWorkflowRules.DefaultEffectiveAt(d, DateTime.Now) : (DateTime?)null,
                    PriorityRequests = state.Records.Select(x => x.Application.Request).Where(x => x.REQUEST_ID != id && x.CATEGORY == "routine" && x.STATUS == "approved" &&
                        CARGO_DWorkflowRules.Overlap(r.START_AT.Value, r.END_AT.Value, x.START_AT.Value, x.END_AT.Value)).Select(x => x.REQUEST_NO).ToList() };
            });
        }
        internal void Change(List<CARGO_DWorkActionDto> actions, CARGO_DStaffDto actor, int module) {
            CARGO_DWorkflowRules.Demand(actor, module);
            if (actions == null || actions.Count == 0 || actions.Count > 100 || actions.Select(x => x.RequestId).Distinct().Count() != actions.Count) throw new InvalidOperationException("請選擇 1～100 筆不重複的申請");
            if (actions.Count > 1 && (module == 3 || actions.Any(x => x.Command != "approve"))) throw new InvalidOperationException("批次僅支援核准");
            Write(state => { foreach (var a in actions) {
                var d = state.Records.FirstOrDefault(x => x.Application.Request.REQUEST_ID == a.RequestId);
                if (d == null) throw new InvalidOperationException("查無申請");
                CARGO_DWorkflowRules.Apply(state, d, a, actor, module);
            } return actions.Select(x => x.RequestId).ToList(); });
        }
        internal List<string> Backfill(CARGO_DStaffDto actor) {
            CARGO_DWorkflowRules.Demand(actor, 3); var report = new List<string>();
            Write(state => {
                var changed = new List<long>(); var now = DateTime.Now;
                foreach (var d in state.Records.Where(x => x.Application.Request.STATUS == "dispatched" && x.Application.Request.SIGN_STATUS == "approved" && x.Application.Request.OUTCOME == "selfDrive" && x.Application.Request.CONTINUE_MATCHING == 1 && x.Application.Request.END_AT > now)
                    .OrderByDescending(x => x.Application.Request.CATEGORY == "routine").ThenBy(x => x.Application.Request.START_AT)) {
                    var r = d.Application.Request; var last = d.Assignments.LastOrDefault();
                    if (last == null) { report.Add(r.REQUEST_NO + "：缺少指派區間"); continue; }
                    var from = now > last.FROM_AT ? now : last.FROM_AT;
                    var vehicle = state.Resources.FirstOrDefault(x => x.RESOURCE_ID == last.VEHICLE);
                    var driver = CARGO_DWorkflowRules.Candidates(state, r, from, r.END_AT.Value).Where(x => x.Resource.KIND == "DRIVER" && x.UnavailableReason == null)
                        .OrderByDescending(x => vehicle != null && x.Resource.SITE == vehicle.SITE).ThenBy(x => x.Resource.RESOURCE_ID).FirstOrDefault();
                    if (driver == null) { report.Add(r.REQUEST_NO + "：仍無可用司機"); continue; }
                    // Validate before mutating; a failed assignment rolls back the whole transaction.
                    CARGO_DWorkflowRules.Apply(state, d, new CARGO_DWorkActionDto { RequestId = r.REQUEST_ID, Version = r.VERSION_NO, Command = "reassign", Vehicle = last.VEHICLE, Driver1 = driver.Resource.RESOURCE_ID, EffectiveAt = from, Note = "自駕案件補派司機" }, actor, 3);
                    changed.Add(r.REQUEST_ID); report.Add(r.REQUEST_NO + "：已補派 " + driver.Resource.NAME + (vehicle != null && driver.Resource.SITE != vehicle.SITE ? "（跨據點）" : ""));
                }
                return changed;
            });
            if (report.Count == 0) report.Add("目前沒有可補派的自駕案件"); return report;
        }
#if LOCAL_DEMO
        private static T Read<T>(Func<CARGO_DWorkState, T> run) { lock (CARGO_D01DemoData.Sync) return run(CARGO_DWorkflowDemoData.Load()); }
        private static void Write(Func<CARGO_DWorkState, List<long>> run) { lock (CARGO_D01DemoData.Sync) {
            var state = CARGO_DWorkflowDemoData.Load(); var ids = run(state);
            foreach (var id in ids) CARGO_D01DemoData.Records[id] = state.Records.Single(x => x.Application.Request.REQUEST_ID == id);
            CARGO_DWorkflowDemoData.Signs = state.Signs;
        } }
#else
        private static CARGO_DWorkPageDto QueryOracle(CARGO_DWorkQueryDto q, CARGO_DStaffDto actor, int module) {
            var where = module == 2 ? "r.STATUS <> 'draft' AND EXISTS (SELECT 1 FROM CARGO_D_APPROVER a WHERE a.OWNER_ID=r.OWNER_ID AND a.APPROVER_ID=:Actor AND a.ACTIVE=1)" :
                module == 3 ? "r.STATUS IN ('approved','dispatched','noVehicle')" : "EXISTS (SELECT 1 FROM CARGO_D_SIGN s WHERE s.REQUEST_ID=r.REQUEST_ID)";
            if (!String.IsNullOrWhiteSpace(q.Applicant)) where += " AND INSTR(UPPER(r.APPLICANT),UPPER(:Applicant)) > 0";
            if (module == 4 && !String.IsNullOrWhiteSpace(q.Keyword)) where += " AND (INSTR(UPPER(r.REQUEST_NO),UPPER(:Keyword)) > 0 OR INSTR(UPPER(r.DISPATCH_NO),UPPER(:Keyword)) > 0 OR INSTR(UPPER(r.APPLICANT),UPPER(:Keyword)) > 0)";
            if (q.UseDate.HasValue) where += " AND r.START_AT < :NextDay AND r.END_AT > :UseDay";
            if (!String.IsNullOrEmpty(q.Status)) {
                if (module == 2) where += q.Status == "passed" ? " AND r.STATUS NOT IN ('submitted','rejected')" : " AND r.STATUS=:Status";
                else if (module == 4) where += " AND EXISTS (SELECT 1 FROM CARGO_D_SIGN s WHERE s.REQUEST_ID=r.REQUEST_ID AND s.STATUS=:Status AND s.ROUND_NO=(SELECT MAX(t.ROUND_NO) FROM CARGO_D_SIGN t WHERE t.REQUEST_ID=r.REQUEST_ID))";
                else if (q.Status == "work") where += " AND (r.STATUS='approved' OR r.PENDING_RETURN_AT IS NOT NULL)";
                else where += @" AND (CASE WHEN r.STATUS='approved' THEN 'todo' WHEN r.STATUS='noVehicle' THEN 'noCar'
                    WHEN r.SIGN_STATUS='pending' THEN 'signing' WHEN r.USAGE_AT IS NOT NULL THEN 'logged'
                    WHEN r.START_AT <= :NowAt THEN 'departed' ELSE 'ready' END)=:Status";
            }
            var size = Math.Max(1,Math.Min(100,q.PageSize)); var page = Math.Max(1,Math.Min(1000000,q.Page));
            var parameters = CARGO_DDatabase.Params(new { Actor=actor.UserId, Applicant=(q.Applicant ?? "").Trim(), Keyword=(q.Keyword ?? "").Trim(), Status=q.Status,
                UseDay=q.UseDate.HasValue ? q.UseDate.Value.Date : DateTime.Today, NextDay=q.UseDate.HasValue ? q.UseDate.Value.Date.AddDays(1) : DateTime.Today.AddDays(1), NowAt=DateTime.Now, Offset=(page-1)*size, PageSize=size });
            var order = module == 3 ? "CASE WHEN r.CATEGORY='routine' THEN 0 ELSE 1 END, " : "";
            using (var db=CARGO_DDatabase.Open()) using (var tx=db.BeginTransaction()) {
                var total=db.Query<int>("SELECT COUNT(*) FROM CARGO_D_REQUEST r WHERE "+where, parameters,tx).Single();
                var items=db.Query<CARGO_D_REQUEST>("SELECT r.* FROM CARGO_D_REQUEST r WHERE "+where+" ORDER BY "+order+"r.CREATED_AT DESC, r.REQUEST_ID DESC OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY",parameters,tx).ToList();
                var info = new Dictionary<long, CARGO_D02ListInfoDto>();
                if (module == 2 && items.Count > 0) {
                    // Only aggregate the authorized page; bind IDs rather than loading every detail.
                    var keys = new DynamicParameters();
                    var names = items.Select((r, i) => { var key = "Request" + i; keys.Add(key, r.REQUEST_ID); return ":" + key; }).ToArray();
                    var selected = "(" + String.Join(",", names) + ")";
                    var pageKeys = CARGO_DDatabase.Params(keys);
                    info = db.Query<CARGO_D02ListInfoDto>(@"SELECT r.REQUEST_ID,
                        (SELECT COUNT(*) FROM CARGO_D_CARGO c WHERE c.REQUEST_ID=r.REQUEST_ID) CargoCount,
                        (SELECT NVL(SUM(c.QUANTITY),0) FROM CARGO_D_CARGO c WHERE c.REQUEST_ID=r.REQUEST_ID) CargoQuantity
                        FROM CARGO_D_REQUEST r WHERE r.REQUEST_ID IN " + selected, pageKeys, tx).ToDictionary(x => x.REQUEST_ID);
                    var approvers = db.Query<CARGO_D_APPROVER>(@"SELECT a.* FROM CARGO_D_APPROVER a WHERE a.ACTIVE=1 AND EXISTS
                        (SELECT 1 FROM CARGO_D_REQUEST r WHERE r.OWNER_ID=a.OWNER_ID AND r.REQUEST_ID IN " + selected + ")", pageKeys, tx).ToList();
                    foreach (var r in items) info[r.REQUEST_ID].Approver = ReviewApprover(r, approvers);
                }
                tx.Commit(); return new CARGO_DWorkPageDto {Total=total,Items=items,ReviewInfo=info};
            }
        }
        private static T Read<T>(Func<CARGO_DWorkState, T> run) { using (var db = CARGO_DDatabase.Open()) using (var tx = db.BeginTransaction()) {
            var result = run(Load(db, tx)); tx.Commit(); return result;
        } }
        private static void Write(Func<CARGO_DWorkState, List<long>> run) { using (var db = CARGO_DDatabase.Open()) using (var tx = db.BeginTransaction()) {
            // Existing migration scope deliberately excludes cross-request capacity locks.
            // VERSION_NO prevents stale updates to the same request; resource checks are a snapshot.
            var state = Load(db, tx); var versions = state.Records.ToDictionary(x => x.Application.Request.REQUEST_ID, x => x.Application.Request.VERSION_NO);
            foreach (var id in run(state)) Save(db, tx, state, state.Records.Single(x => x.Application.Request.REQUEST_ID == id), versions[id]);
            tx.Commit();
        } }
        internal static CARGO_DWorkState Load(IDbConnection db, IDbTransaction tx) {
            var state = new CARGO_DWorkState {
                Resources = db.Query<CARGO_D_RESOURCE>("SELECT * FROM CARGO_D_RESOURCE", transaction: tx).ToList(),
                Blocks = db.Query<CARGO_D_RESOURCE_BLOCK>("SELECT * FROM CARGO_D_RESOURCE_BLOCK", transaction: tx).ToList(),
                Approvers = db.Query<CARGO_D_APPROVER>("SELECT * FROM CARGO_D_APPROVER WHERE ACTIVE = 1", transaction: tx).ToList(),
                Signs = db.Query<CARGO_D_SIGN>("SELECT * FROM CARGO_D_SIGN", transaction: tx).ToList()
            };
            var locations = db.Query<CARGO_D_LOCATION>("SELECT * FROM CARGO_D_LOCATION ORDER BY LINE_NO", transaction: tx).ToLookup(x => x.REQUEST_ID);
            var drivers = db.Query<CARGO_D_SELF_DRIVER>("SELECT * FROM CARGO_D_SELF_DRIVER ORDER BY LINE_NO", transaction: tx).ToLookup(x => x.REQUEST_ID);
            var cargo = db.Query<CARGO_D_CARGO>("SELECT * FROM CARGO_D_CARGO ORDER BY LINE_NO", transaction: tx).ToLookup(x => x.REQUEST_ID);
            var assignments = db.Query<CARGO_D_ASSIGNMENT>("SELECT * FROM CARGO_D_ASSIGNMENT ORDER BY LINE_NO", transaction: tx).ToLookup(x => x.REQUEST_ID);
            var logs = db.Query<CARGO_D_LOG>("SELECT * FROM CARGO_D_LOG ORDER BY LOG_ID", transaction: tx).ToLookup(x => x.REQUEST_ID);
            var notices = db.Query<CARGO_D_NOTICE>("SELECT * FROM CARGO_D_NOTICE ORDER BY LINE_NO", transaction: tx).ToLookup(x => x.REQUEST_ID);
            state.Records = db.Query<CARGO_D_REQUEST>("SELECT * FROM CARGO_D_REQUEST", transaction: tx).Select(r => new CARGO_D01DetailDto {
                Application = new CARGO_D01SaveDto { Request = r, Locations = locations[r.REQUEST_ID].ToList(), SelfDrivers = drivers[r.REQUEST_ID].ToList(), Cargo = cargo[r.REQUEST_ID].ToList() },
                Assignments = assignments[r.REQUEST_ID].ToList(), History = logs[r.REQUEST_ID].ToList(), Notices = notices[r.REQUEST_ID].ToList()
            }).ToList(); return state;
        }
        private static void Save(IDbConnection db, IDbTransaction tx, CARGO_DWorkState state, CARGO_D01DetailDto d, int version) {
            var r = d.Application.Request;
            if (db.Execute(@"UPDATE CARGO_D_REQUEST SET STATUS=:STATUS, VERSION_NO=:VERSION_NO, UPDATED_AT=:UPDATED_AT,
                REVIEW_NOTE=:REVIEW_NOTE, APPROVER_NAME=:APPROVER_NAME, DISPATCHED_AT=:DISPATCHED_AT, DISPATCHED_BY=:DISPATCHED_BY,
                DISPATCH_NOTE=:DISPATCH_NOTE, OUTCOME=:OUTCOME, SIGN_STATUS=:SIGN_STATUS, DISPATCH_NO=:DISPATCH_NO, VEHICLE=:VEHICLE,
                DRIVER1=:DRIVER1, DRIVER2=:DRIVER2, END_AT=:END_AT, PENDING_RETURN_AT=:PENDING_RETURN_AT, RETURN_REQUESTED_AT=:RETURN_REQUESTED_AT,
                RETURN_REASON=:RETURN_REASON WHERE REQUEST_ID=:REQUEST_ID AND VERSION_NO=:PreviousVersion",
                CARGO_DDatabase.Params(new { r.STATUS, r.VERSION_NO, r.UPDATED_AT, r.REVIEW_NOTE, r.APPROVER_NAME, r.DISPATCHED_AT, r.DISPATCHED_BY, r.DISPATCH_NOTE,
                    r.OUTCOME, r.SIGN_STATUS, r.DISPATCH_NO, r.VEHICLE, r.DRIVER1, r.DRIVER2, r.END_AT, r.PENDING_RETURN_AT, r.RETURN_REQUESTED_AT, r.RETURN_REASON, r.REQUEST_ID, PreviousVersion = version }), tx) != 1)
                throw new InvalidOperationException("資料已更新，請重新開啟後操作");
            var key = CARGO_DDatabase.Params(new { r.REQUEST_ID });
            db.Execute("DELETE FROM CARGO_D_ASSIGNMENT WHERE REQUEST_ID=:REQUEST_ID", key, tx);
            foreach (var x in d.Assignments) db.Execute(@"INSERT INTO CARGO_D_ASSIGNMENT (REQUEST_ID,LINE_NO,FROM_AT,TO_AT,VEHICLE,DRIVER1,DRIVER2,KIND,REASON,ACTOR)
                VALUES (:REQUEST_ID,:LINE_NO,:FROM_AT,:TO_AT,:VEHICLE,:DRIVER1,:DRIVER2,:KIND,:REASON,:ACTOR)", CARGO_DDatabase.Params(x), tx);
            db.Execute("DELETE FROM CARGO_D_SIGN WHERE REQUEST_ID=:REQUEST_ID", key, tx);
            foreach (var x in state.Signs.Where(x => x.REQUEST_ID == r.REQUEST_ID)) db.Execute(@"INSERT INTO CARGO_D_SIGN (REQUEST_ID,ROUND_NO,STATUS,SUBMITTED_BY,SUBMITTED_AT,SUMMARY,DECIDED_BY,DECIDED_AT,NOTE)
                VALUES (:REQUEST_ID,:ROUND_NO,:STATUS,:SUBMITTED_BY,:SUBMITTED_AT,:SUMMARY,:DECIDED_BY,:DECIDED_AT,:NOTE)", CARGO_DDatabase.Params(x), tx);
            foreach (var x in d.History.Where(x => x.LOG_ID == 0)) db.Execute(@"INSERT INTO CARGO_D_LOG (LOG_ID,REQUEST_ID,ACTION,ACTOR,NOTE,CREATED_AT)
                VALUES (CARGO_D_LOG_SEQ.NEXTVAL,:REQUEST_ID,:ACTION,:ACTOR,:NOTE,:CREATED_AT)", CARGO_DDatabase.Params(x), tx);
            var max = db.Query<int>("SELECT NVL(MAX(LINE_NO),0) FROM CARGO_D_NOTICE WHERE REQUEST_ID=:REQUEST_ID", key, tx).Single();
            foreach (var x in d.Notices.Where(x => x.LINE_NO > max)) db.Execute(@"INSERT INTO CARGO_D_NOTICE (REQUEST_ID,LINE_NO,SENT_AT,RECIPIENT,KIND,MESSAGE)
                VALUES (:REQUEST_ID,:LINE_NO,:SENT_AT,:RECIPIENT,:KIND,:MESSAGE)", CARGO_DDatabase.Params(x), tx);
        }
#endif
    }
}
