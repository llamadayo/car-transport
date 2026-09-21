using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using Dapper;
using VD.Core;

namespace VD.Services
{
    public sealed class RegionalApplicationService : ServiceBase
    {
        private readonly IClock clock;
        public RegionalApplicationService(DatabaseSettings settings,IClock clock=null) : base(settings) { this.clock=clock ?? new TaipeiClock(); }
        private static void ValidateObject(object input)
        {
            if(input==null) throw new ArgumentException("請提供完整資料。");
            var errors=new List<ValidationResult>();
            if(!Validator.TryValidateObject(input,new ValidationContext(input),errors,true)) throw new ArgumentException(string.Join("；",errors.Select(e=>e.ErrorMessage)));
        }
        internal static void ValidateItems(IList<CargoItem> items)
        {
            if(items==null||items.Count==0) throw new ArgumentException("請至少新增一項貨物。");
            foreach(var item in items) { ValidateObject(item); if(double.IsNaN(item.LengthCm+item.WidthCm+item.HeightCm+item.Weight)||double.IsInfinity(item.LengthCm+item.WidthCm+item.HeightCm+item.Weight)) throw new ArgumentException("貨物數值格式不正確。"); }
        }
        private void Validate(ApplicationInput input,MasterSnapshot m)
        {
            ValidateObject(input); ValidateItems(input.Items);
            if(input.RecvMode!="asap"&&input.RecvMode!="exact") throw new ArgumentException("收貨模式不正確。");
            var pick=m.Stations.SingleOrDefault(s=>s.Id==input.PickStationId && s.BranchId==input.BranchId);
            var drop=m.Stations.SingleOrDefault(s=>s.Id==input.DropStationId && s.BranchId==input.BranchId);
            if(pick==null||drop==null) throw new ArgumentException("收送貨站點須屬於所選分公司。");
            if(pick.SortOrder>=drop.SortOrder) throw new ArgumentException("收貨站須在送貨站之前（路線行進方向）。");
            if(input.RecvMode=="asap") { input.ServiceDate=BusinessTime.Date(clock.Now); input.ExpectedTime=null; }
            else {
                if(BusinessTime.ParseDate(input.ServiceDate)<clock.Now.Date) throw new ArgumentException("期望日期不可早於今天。");
                if(!string.IsNullOrEmpty(input.ExpectedTime)) BusinessTime.Minute(input.ExpectedTime);
            }
        }
        public ApplicationRow Submit(ApplicationInput input)
        {
            using(var c=OpenConnection()) using(var tx=BeginWrite(c))
            {
                bool committed=false;
                try {
                    var m=new MasterDataService(Settings).Read(c,tx); Validate(input,m); LockBranch(c,tx,input.BranchId);
                    long id;
                    if(Settings.IsOracle) id=c.QuerySingle<long>("SELECT VD_REG_APP_SEQ.NEXTVAL FROM DUAL",transaction:tx);
                    else { c.Execute("UPDATE VD_COUNTER SET NEXT_VAL=NEXT_VAL+1 WHERE KEY_NAME='REG_APP'",transaction:tx); id=c.QuerySingle<long>("SELECT NEXT_VAL-1 FROM VD_COUNTER WHERE KEY_NAME='REG_APP'",transaction:tx); }
                    var app=new RegionalApplication { Id=id,AppNo="LA"+id.ToString("000"),SubmitSeq=id,Applicant=input.Applicant,BranchId=input.BranchId,
                        PickStationId=input.PickStationId,DropStationId=input.DropStationId,PickBuilding=input.PickBuilding,DropBuilding=input.DropBuilding,
                        RecvMode=input.RecvMode,ServiceDate=input.ServiceDate,ExpectedTime=input.ExpectedTime,
                        ExpectedMinute=string.IsNullOrEmpty(input.ExpectedTime)?(int?)null:BusinessTime.Minute(input.ExpectedTime),
                        LoadMin=input.LoadMin,UnloadMin=input.UnloadMin,RecipientUnit=input.RecipientUnit,RecipientName=input.RecipientName,RecipientPhone=input.RecipientPhone,
                        AgentName=input.AgentName,AgentPhone=input.AgentPhone,Items=input.Items,Status="submitted",CreatedAt=clock.Now.ToString("yyyy-MM-ddTHH:mm:ss"),MatchAttempt=0 };
                    ApplyMatch(c,tx,app,m);
                    c.Execute(@"INSERT INTO VD_REG_APP (ID,APP_NO,SUBMIT_SEQ,APPLICANT,BRANCH_ID,PICK_STATION_ID,DROP_STATION_ID,PICK_BUILDING,DROP_BUILDING,
                        RECV_MODE,SERVICE_DATE,EXPECTED_MINUTE,LOAD_MIN,UNLOAD_MIN,RECIPIENT_UNIT,RECIPIENT_NAME,RECIPIENT_PHONE,AGENT_NAME,AGENT_PHONE,
                        STATUS,SHIFT_ID,ARRIVAL_MINUTE,EXPECT_DIFF_MIN,NOTE,FAILURE_REASON,CREATED_AT,MATCH_ATTEMPT)
                        VALUES (:Id,:AppNo,:SubmitSeq,:Applicant,:BranchId,:PickStationId,:DropStationId,:PickBuilding,:DropBuilding,
                        :RecvMode,:ServiceDate,:ExpectedMinute,:LoadMin,:UnloadMin,:RecipientUnit,:RecipientName,:RecipientPhone,:AgentName,:AgentPhone,
                        :Status,:ShiftId,:ArrivalMinute,:ExpectDiffMin,:Note,:FailureReason,:CreatedAt,:MatchAttempt)",Parameters(app),tx);
                    SaveItems(c,tx,app); SaveTrace(c,tx,app); tx.Commit(); committed=true;
                    return new RegionalQueryService(Settings).GetApplication(id);
                } catch { if(!committed && tx.Connection!=null) tx.Rollback(); throw; }
            }
        }
        private DynamicParameters Parameters(RegionalApplication app)
        {
            var p=new DynamicParameters(app);
            p.Add("ServiceDate",DateValue(app.ServiceDate)); p.Add("CreatedAt",TimestampValue(DateTime.ParseExact(app.CreatedAt,"yyyy-MM-ddTHH:mm:ss",System.Globalization.CultureInfo.InvariantCulture)));
            return p;
        }
        private void ApplyMatch(IDbConnection c,IDbTransaction tx,RegionalApplication app,MasterSnapshot master)
        {
            var queries=new RegionalQueryService(Settings);
            var result=new RegionalMatchingService().Match(app,master,queries.ReadApplications(c,tx),queries.ReadPlans(c,tx),clock.Now);
            app.Status=result.Ok?"matched":"unscheduled"; app.ShiftId=result.ShiftId; app.ArrivalMinute=result.ArrivalMinute; app.ExpectDiffMin=result.ExpectDiffMin;
            app.Note=result.Ok?"":result.Message; app.FailureReason=result.Reason; app.Trace=result.Trace; app.MatchAttempt++;
            if(result.Ok) RegionalDispatchService.EnsureTrip(c,tx,Settings,app.ServiceDate,master.Shifts.Single(s=>s.Id==result.ShiftId));
        }
        private static void SaveItems(IDbConnection c,IDbTransaction tx,RegionalApplication app)
        {
            c.Execute("DELETE FROM VD_REG_ITEM WHERE APPLICATION_ID=:Id",new {app.Id},tx);
            for(int i=0;i<app.Items.Count;i++) { app.Items[i].ApplicationId=app.Id; app.Items[i].LineNo=i+1; }
            c.Execute(@"INSERT INTO VD_REG_ITEM (APPLICATION_ID,LINE_NO,NAME,LENGTH_CM,WIDTH_CM,HEIGHT_CM,CATEGORY,QTY,WEIGHT)
                VALUES (:ApplicationId,:LineNo,:Name,:LengthCm,:WidthCm,:HeightCm,:Category,:Qty,:Weight)",app.Items,tx);
        }
        private static void SaveTrace(IDbConnection c,IDbTransaction tx,RegionalApplication app)
        {
            c.Execute(@"INSERT INTO VD_REG_MATCH_STEP (APPLICATION_ID,ATTEMPT,STEP_NO,CODE,MESSAGE) VALUES (:Id,:Attempt,:StepNo,:Code,:Message)",
                app.Trace.Select((s,i)=>new {app.Id,Attempt=app.MatchAttempt,StepNo=i+1,s.Code,s.Message}),tx);
        }
        public ApplicationRow UpdateAndRematch(long id,List<CargoItem> items)
        {
            ValidateItems(items);
            using(var c=OpenConnection()) using(var tx=BeginWrite(c))
            {
                bool committed=false;
                try {
                    var app=new RegionalQueryService(Settings).Get(c,tx,id);
                    LockBranch(c,tx,app.BranchId); app=new RegionalQueryService(Settings).Get(c,tx,id);
                    if(app.Status!="unscheduled") throw new ArgumentException("僅未排入的申請單可編輯貨物並重新媒合。");
                    app.Items=items; app.ShiftId=null; app.ArrivalMinute=null; app.ExpectDiffMin=null;
                    ApplyMatch(c,tx,app,new MasterDataService(Settings).Read(c,tx));
                    c.Execute(@"UPDATE VD_REG_APP SET STATUS=:Status,SHIFT_ID=:ShiftId,ARRIVAL_MINUTE=:ArrivalMinute,EXPECT_DIFF_MIN=:ExpectDiffMin,
                        NOTE=:Note,FAILURE_REASON=:FailureReason,MATCH_ATTEMPT=:MatchAttempt WHERE ID=:Id",app,tx);
                    SaveItems(c,tx,app); SaveTrace(c,tx,app); tx.Commit(); committed=true; return new RegionalQueryService(Settings).GetApplication(id);
                } catch { if(!committed && tx.Connection!=null) tx.Rollback(); throw; }
            }
        }
        public object ReportIncident(long id,string reason)
        {
            reason=reason ?? "";
            if(reason!=""&&reason!="使用者不準時"&&reason!="使用者沒出現") throw new ArgumentException("異常原因不正確。");
            RegionalApplication app;
            using(var c=OpenConnection()) using(var tx=BeginWrite(c))
            {
                bool committed=false;
                try {
                    app=new RegionalQueryService(Settings).Get(c,tx,id);
                    if(app.Status!="matched"&&app.Status!="delivered") throw new ArgumentException("僅已排班申請可回報異常。");
                    c.Execute("UPDATE VD_REG_APP SET INCIDENT=:Reason WHERE ID=:Id",new {Reason=string.IsNullOrEmpty(reason)?null:reason,Id=id},tx); tx.Commit(); committed=true;
                } catch { if(!committed && tx.Connection!=null) tx.Rollback(); throw; }
            }
            return new IncidentNotificationService().Report(app,reason);
        }
        public ApplicationRow ConfirmDelivery(long id,string by)
        {
            using(var c=OpenConnection())
            {
                var count=c.Execute("UPDATE VD_REG_APP SET STATUS='delivered',DELIVERED_AT=:NowAt,DELIVERED_BY=:ByName WHERE ID=:Id AND STATUS='matched'",
                    new {NowAt=TimestampValue(clock.Now),ByName=by??"調度室",Id=id});
                if(count!=1) throw new ArgumentException("僅已排班申請可確認交貨。");
            }
            return new RegionalQueryService(Settings).GetApplication(id);
        }
        public int SeedDemo()
        {
            for(int i=0;i<3;i++) Submit(new ApplicationInput {
                Applicant="業務部-周雅婷",BranchId=i==2?"D6":"D10",PickStationId=i==2?"D6-300":"D10-200",DropStationId=i==0?"D10-300":i==1?"D10-600":"D6-900",
                PickBuilding=i==2?"300":"200",DropBuilding=i==0?"300":i==1?"600":"900",RecvMode=i==1?"exact":"asap",ServiceDate=BusinessTime.Date(clock.Now),ExpectedTime=i==1?"14:00":null,
                LoadMin=i==0?10:i==1?12:15,UnloadMin=i==0?5:i==1?8:10,
                RecipientUnit=i==0?"生產部":i==1?"倉儲課":"工務組",RecipientName=i==0?"林建志":i==1?"黃美玲":"吳志豪",RecipientPhone=i==0?"03-1234567#210":i==1?"03-2345678#118":"03-3456789#305",
                AgentName=i==0?"陳怡君":i==2?"李國華":null,AgentPhone=i==0?"0912-345-678":i==2?"0922-111-222":null,
                Items=new List<CargoItem> { new CargoItem { Name=i==0?"零件箱":i==1?"棧板":"長料",LengthCm=i==0?50:i==1?110:480,WidthCm=i==0?40:i==1?90:25,HeightCm=i==0?30:i==1?120:25,Qty=i==0?6:i==1?1:3,Weight=i==0?12:i==1?200:30,Category=i==0?"BOX":i==1?"PALLET":"LONG" } }
            });
            return 3;
        }
    }
}
