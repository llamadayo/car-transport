using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using VD.Core;

namespace VD.Services
{
    public sealed class RegionalQueryService : ServiceBase
    {
        public RegionalQueryService(DatabaseSettings settings) : base(settings) { }
        internal List<RegionalApplication> ReadApplications(IDbConnection c, IDbTransaction tx, ApplicationQuery q=null)
        {
            q=q ?? new ApplicationQuery();
            string sql=@"SELECT a.ID, a.APP_NO, a.SUBMIT_SEQ, a.APPLICANT, a.BRANCH_ID,
                a.PICK_STATION_ID, a.DROP_STATION_ID, a.PICK_BUILDING, a.DROP_BUILDING, a.RECV_MODE,
                a.EXPECTED_MINUTE, a.LOAD_MIN, a.UNLOAD_MIN, a.RECIPIENT_UNIT, a.RECIPIENT_NAME,
                a.RECIPIENT_PHONE, a.AGENT_NAME, a.AGENT_PHONE, a.STATUS, a.SHIFT_ID,
                a.ARRIVAL_MINUTE, a.EXPECT_DIFF_MIN, a.NOTE, a.FAILURE_REASON, a.INCIDENT,
                a.DELIVERED_BY, a.MATCH_ATTEMPT, " + DateColumn("a.SERVICE_DATE","ServiceDate") + ", " +
                TimestampColumn("a.CREATED_AT","CreatedAt") + ", " + TimestampColumn("a.DELIVERED_AT","DeliveredAt") +
                " FROM VD_REG_APP a WHERE 1=1";
            var p=new DynamicParameters();
            if(!string.IsNullOrWhiteSpace(q.Applicant)) { sql+=" AND INSTR(a.APPLICANT, :Applicant)>0"; p.Add("Applicant",q.Applicant.Trim()); }
            if(!string.IsNullOrEmpty(q.BranchId)) { sql+=" AND a.BRANCH_ID=:BranchId"; p.Add("BranchId",q.BranchId); }
            if(!string.IsNullOrEmpty(q.StationId)) { sql+=" AND a.DROP_STATION_ID=:StationId"; p.Add("StationId",q.StationId); }
            if(!string.IsNullOrEmpty(q.Status)) { sql+=" AND a.STATUS=:Status"; p.Add("Status",q.Status); }
            if(!string.IsNullOrEmpty(q.Mode)) { sql+=" AND a.RECV_MODE=:Mode"; p.Add("Mode",q.Mode); }
            var apps=c.Query<RegionalApplication>(sql+" ORDER BY a.SUBMIT_SEQ",p,tx).ToList();
            // One query for all cargo avoids N+1 reads; only attach items belonging to selected applications.
            var items=c.Query<CargoItem>("SELECT * FROM VD_REG_ITEM ORDER BY APPLICATION_ID, LINE_NO",transaction:tx).ToLookup(x=>x.ApplicationId);
            foreach(var a in apps) { a.Items=items[a.Id].ToList(); a.ExpectedTime=BusinessTime.Display(a.ExpectedMinute); }
            return apps;
        }
        internal List<Trip> ReadPlans(IDbConnection c, IDbTransaction tx) => c.Query<Trip>("SELECT "+DateColumn("SERVICE_DATE","ServiceDate")+", SHIFT_ID, VEHICLE_ID, DRIVER_ID FROM VD_REG_TRIP",transaction:tx).ToList();
        internal RegionalApplication Get(IDbConnection c, IDbTransaction tx, long id)
        {
            var a=ReadApplications(c,tx).SingleOrDefault(x=>x.Id==id);
            if(a==null) throw new ArgumentException("找不到此申請單。");
            a.Trace=c.Query<TraceStep>("SELECT CODE, MESSAGE FROM VD_REG_MATCH_STEP WHERE APPLICATION_ID=:Id AND ATTEMPT=:Attempt ORDER BY STEP_NO",new { Id=id, Attempt=a.MatchAttempt },tx).ToList();
            return a;
        }
        internal static Trip EffectivePlan(string date, Shift shift, IList<Trip> plans) => plans.FirstOrDefault(p=>p.ServiceDate==date && p.ShiftId==shift.Id) ?? new Trip { ServiceDate=date, ShiftId=shift.Id, VehicleId=shift.VehicleId, DriverId=shift.DriverId };
        public static ApplicationRow Display(RegionalApplication a, MasterSnapshot m, IList<Trip> plans)
        {
            var shift=m.Shifts.FirstOrDefault(s=>s.Id==a.ShiftId);
            var plan=shift==null ? null : EffectivePlan(a.ServiceDate,shift,plans);
            var vehicle=m.Vehicles.FirstOrDefault(v=>v.Id==plan?.VehicleId);
            var drop=m.Stations.Single(s=>s.Id==a.DropStationId);
            var pick=m.Stations.Single(s=>s.Id==a.PickStationId);
            var branch=m.Sites.Single(s=>s.Id==a.BranchId).Name;
            return new ApplicationRow {
                Id=a.Id, AppNo=a.AppNo, Applicant=a.Applicant, BranchName=branch,
                Destination=branch+"·"+drop.Name+" / "+a.DropBuilding, Pickup=pick.Name+" / "+a.PickBuilding,
                ServiceDate=a.ServiceDate, ModeLabel=a.RecvMode=="exact" ? "指定期望時間" : "越快越好",
                ShiftLabel=shift?.Label ?? "—", VehicleId=vehicle?.Id ?? "—", VehicleName=vehicle?.Name ?? "—",
                DriverName=m.Drivers.FirstOrDefault(d=>d.Id==plan?.DriverId)?.Name ?? "—",
                Status=a.Status, StatusLabel=a.Status=="matched" ? "已排班" : a.Status=="delivered" ? "已交貨" : "未排入·請改期",
                Arrival=a.Arrival, CreatedAt=a.CreatedAt, Application=a, CargoSummary=string.Join("、",a.Items.Select(i=>i.Name+"×"+i.Qty))
            };
        }
        public List<ApplicationRow> ReadApplications(ApplicationQuery q=null)
        {
            using(var c=OpenConnection()) { var m=new MasterDataService(Settings).Read(c,null); var plans=ReadPlans(c,null); return ReadApplications(c,null,q).Select(a=>Display(a,m,plans)).ToList(); }
        }
        public ApplicationRow GetApplication(long id)
        {
            using(var c=OpenConnection()) return Display(Get(c,null,id),new MasterDataService(Settings).Read(c,null),ReadPlans(c,null));
        }
        internal static TripRow Group(string date, Shift shift, IList<RegionalApplication> apps, MasterSnapshot m, IList<Trip> plans)
        {
            var p=EffectivePlan(date,shift,plans);
            return new TripRow { ServiceDate=date, ShiftId=shift.Id, VehicleId=p.VehicleId, DriverId=p.DriverId,
                VehicleName=m.Vehicles.Single(v=>v.Id==p.VehicleId).Name, DriverName=m.Drivers.Single(d=>d.Id==p.DriverId).Name,
                BranchName=m.Sites.Single(b=>b.Id==shift.BranchId).Name, ShiftLabel=shift.Label,
                Count=apps.Count, Applications=apps.Select(a=>Display(a,m,plans)).ToList() };
        }
        public List<TripRow> ReadTrips(TripQuery query=null)
        {
            var q=query ?? new TripQuery();
            using(var c=OpenConnection())
            {
                var m=new MasterDataService(Settings).Read(c,null); var plans=ReadPlans(c,null);
                return ReadApplications(c,null).Where(a=>a.Status=="matched").GroupBy(a=>new { a.ServiceDate,a.ShiftId })
                    .OrderBy(g=>g.Key.ServiceDate).ThenBy(g=>m.Shifts.Single(s=>s.Id==g.Key.ShiftId).SortOrder)
                    .Select(g=>Group(g.Key.ServiceDate,m.Shifts.Single(s=>s.Id==g.Key.ShiftId),g.ToList(),m,plans))
                    .Where(t=>(string.IsNullOrEmpty(q.ServiceDate)||t.ServiceDate==q.ServiceDate) && (string.IsNullOrEmpty(q.ShiftId)||t.ShiftId==q.ShiftId) && (string.IsNullOrEmpty(q.VehicleId)||t.VehicleId==q.VehicleId) && (string.IsNullOrEmpty(q.DriverId)||t.DriverId==q.DriverId)).ToList();
            }
        }
        public TripRow GetTrip(string date,string shiftId)
        {
            BusinessTime.ParseDate(date);
            using(var c=OpenConnection())
            {
                var m=new MasterDataService(Settings).Read(c,null); var shift=m.Shifts.SingleOrDefault(s=>s.Id==shiftId);
                if(shift==null) throw new ArgumentException("找不到此班次。");
                return Group(date,shift,ReadApplications(c,null).Where(a=>a.ServiceDate==date && a.ShiftId==shiftId && a.Status=="matched").ToList(),m,ReadPlans(c,null));
            }
        }
        public List<ApplicationRow> ReadAssignableApplications(string date,string shiftId)
        {
            var trip=GetTrip(date,shiftId); var m=new MasterDataService(Settings).Read(); var branch=m.Shifts.Single(s=>s.Id==shiftId).BranchId;
            return ReadApplications().Where(r=>r.ServiceDate==date && r.Application.BranchId==branch && r.Application.ShiftId!=shiftId && (r.Status=="matched"||r.Status=="unscheduled")).ToList();
        }
        public object ReadRoutes(string date)
        {
            using(var c=OpenConnection()) {
                var m=new MasterDataService(Settings).Read(c,null); var plans=ReadPlans(c,null);
                return m.Sites.Select(b=>new { b.Id,b.Name,Stations=m.Stations.Where(s=>s.BranchId==b.Id).OrderBy(s=>s.SortOrder).Select(s=>new {s.Name,Buildings=m.Buildings.Where(x=>x.StationId==s.Id).Select(x=>x.Name).ToList()}).ToList(),
                    Shifts=m.Shifts.Where(s=>s.BranchId==b.Id).Select(s=>{var plan=EffectivePlan(date,s,plans);var v=m.Vehicles.Single(x=>x.Id==plan.VehicleId);return new {s.Label,s.Depart,VehicleId=v.Id,VehicleName=v.Name,v.Volume,v.WeightLimit,DriverName=m.Drivers.Single(d=>d.Id==plan.DriverId).Name};}).ToList() }).ToList();
            }
        }
        public List<DriverTask> ReadDriverTasks()
        {
            using(var c=OpenConnection())
            {
                var m=new MasterDataService(Settings).Read(c,null); var plans=ReadPlans(c,null);
                var groups=ReadApplications(c,null).Where(a=>a.Status=="matched"||a.Status=="delivered")
                    .GroupBy(a=>new {a.ServiceDate,a.ShiftId}).OrderBy(g=>g.Key.ServiceDate).ThenBy(g=>m.Shifts.Single(s=>s.Id==g.Key.ShiftId).SortOrder);
                var result=new List<DriverTask>();
                foreach(var g in groups)
                {
                    var shift=m.Shifts.Single(s=>s.Id==g.Key.ShiftId); var t=Group(g.Key.ServiceDate,shift,g.ToList(),m,plans);
                    var task=new DriverTask { ServiceDate=t.ServiceDate, ShiftId=t.ShiftId, VehicleId=t.VehicleId,DriverId=t.DriverId,VehicleName=t.VehicleName,DriverName=t.DriverName,BranchName=t.BranchName,ShiftLabel=t.ShiftLabel,Count=t.Count,Applications=t.Applications,
                        RawVolume=g.Sum(a=>a.Items.Sum(i=>i.LengthCm*i.WidthCm*i.HeightCm/1000*i.Qty)) };
                    foreach(var station in m.Stations.Where(s=>s.BranchId==shift.BranchId).OrderBy(s=>s.SortOrder))
                    {
                        var picks=t.Applications.Where(a=>a.Application.PickStationId==station.Id).ToList();
                        var drops=t.Applications.Where(a=>a.Application.DropStationId==station.Id).ToList();
                        if(picks.Count+drops.Count>0) task.Stops.Add(new TaskStop { Name=station.Name,SortOrder=station.SortOrder,Arrival=BusinessTime.Display(shift.DepartMinute+station.SortOrder*m.InterStationMin),Picks=picks,Drops=drops });
                    }
                    result.Add(task);
                }
                return result;
            }
        }
    }
}
