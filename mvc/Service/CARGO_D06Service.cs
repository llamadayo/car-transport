using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
namespace CARGO.Service {
 public sealed class CARGO_D06Service {
  public List<CARGO_D06TaskDto> Tasks(CARGO_DUsageActorDto actor) {
   return Query(new CARGO_D06QueryDto(), actor).Items;
  }
  public CARGO_D06PageDto Query(CARGO_D06QueryDto query, CARGO_DUsageActorDto actor) {
   CARGO_D05Rules.Demand(actor,false);
   if(query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate.Value.Date > query.ToDate.Value.Date)
    throw new InvalidOperationException("日期迄不可早於日期起");
   return CARGO_DUseData.Read(state=>{
    var mine=state.DriverUsers.Where(x=>x.USER_ID==actor.UserId && x.ACTIVE==1).Select(x=>x.RESOURCE_ID).ToList();
    Func<string,string> name=id=>{var resource=state.Work.Resources.FirstOrDefault(x=>x.RESOURCE_ID==id);return resource==null?id:resource.NAME;};
    var tasks=new List<CARGO_D06TaskDto>();
    foreach(var d in state.Work.Records.Where(x=>x.Application.Request.STATUS=="dispatched"&&x.Application.Request.SIGN_STATUS=="approved")) foreach(var segment in d.Assignments.Where(x=>x.TO_AT>x.FROM_AT)) {
     var drivers=new[]{segment.DRIVER1,segment.DRIVER2}.Where(x=>!String.IsNullOrEmpty(x)).Distinct().ToList();
     if(drivers.Count==0 && actor.IsBusiness) drivers.Add("SELF");
     foreach(var driver in drivers.Where(x=>actor.IsBusiness || mine.Contains(x))) tasks.Add(new CARGO_D06TaskDto {Request=d.Application.Request,Assignment=segment,DriverId=driver,DriverName=driver=="SELF"?"自駕用車（無派司機）· 車輛交接參考":name(driver),
      Partner=String.Join("、",drivers.Where(x=>x!=driver && x!="SELF").Select(name)),VehicleName=name(segment.VEHICLE),Locations=d.Application.Locations,Cargo=d.Application.Cargo});
    }
    // Options and results both originate from authorized assignments. Keep options when dates yield no tasks.
    var choices=tasks.GroupBy(x=>x.DriverId).Select(x=>new CARGO_D06DriverDto {Id=x.Key,Name=x.First().DriverName})
     .OrderBy(x=>x.Id=="SELF").ThenBy(x=>x.Name).ToList();
    var filtered=tasks.AsEnumerable();
    if(query.FromDate.HasValue) filtered=filtered.Where(x=>x.Assignment.TO_AT>query.FromDate.Value.Date);
    if(query.ToDate.HasValue) filtered=filtered.Where(x=>x.Assignment.FROM_AT.Date<=query.ToDate.Value.Date);
    if(!String.IsNullOrWhiteSpace(query.DriverId)) filtered=filtered.Where(x=>x.DriverId==query.DriverId.Trim());
    return new CARGO_D06PageDto {Drivers=choices,Items=filtered.OrderBy(x=>x.DriverId=="SELF").ThenBy(x=>x.DriverId).ThenBy(x=>x.Assignment.FROM_AT).ThenBy(x=>x.Request.REQUEST_ID).ToList()};
   });
  }
 }
}
