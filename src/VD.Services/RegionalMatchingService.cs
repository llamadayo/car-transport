using System;
using System.Collections.Generic;
using System.Linq;
using VD.Core;

namespace VD.Services
{
    // Pure matching: reads a snapshot provided by the application service, never mutates stored orders.
    public sealed class RegionalMatchingService
    {
        public MatchResult Match(RegionalApplication app, MasterSnapshot master, IList<RegionalApplication> existing, IList<Trip> trips, DateTime now)
        {
            var result = new MatchResult();
            var today = BusinessTime.Date(now);
            Func<string,string,MatchResult> fail = (reason,message) => { result.Reason=reason; result.Message=message; result.Trace.Add(new TraceStep { Code=reason, Message=message }); return result; };
            if (string.CompareOrdinal(app.ServiceDate,today)<0) return fail("past","日期已過：請選擇今天或未來日期。");
            int cutoff=app.ServiceDate==today ? now.Hour*60+now.Minute : -1;
            var drop=master.Stations.Single(s=>s.Id==app.DropStationId);
            var pick=master.Stations.FirstOrDefault(s=>s.Id==app.PickStationId);
            int from=pick!=null && pick.SortOrder<drop.SortOrder ? pick.SortOrder : 0;
            var engine=new LoadFeasibilityService(master);
            var shifts=master.Shifts.Where(s=>s.BranchId==app.BranchId).OrderBy(s=>s.SortOrder).ToList();
            shifts = app.RecvMode=="exact" && app.ExpectedMinute.HasValue
                ? shifts.OrderBy(s=>Math.Abs(s.DepartMinute+drop.SortOrder*master.InterStationMin-app.ExpectedMinute.Value)).ToList()
                : shifts.OrderBy(s=>s.DepartMinute).ToList();
            bool usable=false, fitsEmpty=false;
            foreach(var shift in shifts)
            {
                var plan=trips.FirstOrDefault(t=>t.ServiceDate==app.ServiceDate && t.ShiftId==shift.Id);
                var vehicle=master.Vehicles.Single(v=>v.Id==(plan?.VehicleId ?? shift.VehicleId));
                int arrival=shift.DepartMinute+drop.SortOrder*master.InterStationMin;
                result.Trace.Add(new TraceStep { Code="SHIFT", Message=$"嘗試 {shift.Label}／{vehicle.Id}，到站約 {BusinessTime.Display(arrival)}" });
                if(shift.DepartMinute<=cutoff) { result.Trace.Add(new TraceStep { Code="DEPARTED",Message="本班已發車，不再接受新單。" }); continue; }
                usable=true;
                var empty=engine.Check(app.Items,vehicle);
                fitsEmpty |= empty.Ok;
                var assigned=existing.Where(a=>a.Id!=app.Id && a.BranchId==app.BranchId && a.ServiceDate==app.ServiceDate && a.ShiftId==shift.Id && (a.Status=="matched" || a.Status=="delivered")).OrderBy(a=>a.SubmitSeq).ToList();
                int used=assigned.Sum(a=>a.HandleMin);
                if(app.HandleMin>master.HandleBudget-used) { result.Trace.Add(new TraceStep { Code="HANDLE",Message=$"本班已用 {used} 分，加上本單 {app.HandleMin} 分超過 {master.HandleBudget} 分，順延。" }); continue; }
                bool ok=true;
                for(int s=from;s<drop.SortOrder;s++)
                {
                    var load=new LoadValue();
                    foreach(var a in assigned)
                    {
                        int end=master.Stations.Single(x=>x.Id==a.DropStationId).SortOrder;
                        int start=master.Stations.FirstOrDefault(x=>x.Id==a.PickStationId)?.SortOrder ?? 0;
                        if(start>=end) start=0;
                        if(s>=start && s<end) { var e=engine.Effective(a.Items); load.Volume+=e.Volume; load.Weight+=e.Weight; load.Floor+=e.Floor; }
                    }
                    var checkedLoad=engine.Check(app.Items,vehicle,load);
                    result.Trace.Add(new TraceStep { Code="SEGMENT",Message=$"站序 {s} 駛離時淨負載：{load.Volume:0.###}L／{load.Weight:0.###}kg" });
                    result.Trace.AddRange(checkedLoad.Trace);
                    if(!checkedLoad.Ok) { ok=false; break; }
                }
                if(!ok) continue;
                result.Ok=true; result.ShiftId=shift.Id; result.VehicleId=vehicle.Id; result.ArrivalMinute=arrival;
                result.ExpectDiffMin=app.RecvMode=="exact" && app.ExpectedMinute.HasValue ? (int?)(arrival-app.ExpectedMinute.Value) : null;
                result.Message=$"已自動媒合：{shift.Label}／車 {vehicle.Id}／到站約 {BusinessTime.Display(arrival)}";
                result.Trace.Add(new TraceStep { Code="MATCHED", Message=result.Message });
                return result;
            }
            if(!usable) return fail("past","今日班次皆已發車，請改指定未來日期。");
            if(!fitsEmpty) return fail("toobig","貨物太大：超過任何一班車輛的尺寸或容量，無法承運。");
            return fail("full","今天已滿：各班次容量或時間額度皆不足，請改期。");
        }
    }
}
