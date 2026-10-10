using System;
using System.Linq;
using System.Collections.Generic;
using CARGO.DTO;
using CARGO.Service;
using CARGO.Web.ViewModels;
using CARGO.Web.ViewModels.CARGO_D01;
namespace CARGO.Web.Models {
 public static class CARGO_D05Mapper {
  public static string Driver(string id) {return id=="SELF"?"使用者自駕":id;}
  public static CARGO_D05RowViewModel Row(CARGO_D05RowDto d) {var r=d.Request;var u=d.Usage;return new CARGO_D05RowViewModel {RequestId=r.REQUEST_ID,DispatchNo=r.DISPATCH_NO,UseDate=r.START_AT,
   VehicleType=u==null?d.PlannedVehicleType:u.VEHICLE_TYPE,Vehicle=u==null?r.VEHICLE:u.VEHICLE,Driver1=Driver(u==null?(r.DRIVER1??(r.SELF_DRIVE==1?"SELF":null)):u.DRIVER1),Driver2=Driver(u==null?r.DRIVER2:u.DRIVER2),
   FlowStatus=CARGO_D01Mapper.Status(CARGO_D01Rules.DisplayStatus(r,DateTime.Now)),UsageStatus=u==null?"待實登":"已實登",Distance=u==null?(decimal?)null:u.DISTANCE_KM};}
  public static CARGO_D05DetailViewModel Detail(CARGO_D05DetailDto d) {
   var r=d.Application.Application.Request;var u=d.History.FirstOrDefault();var app=CARGO_D01Mapper.Detail(d.Application);app.Form.Options=CARGO_D01Mapper.Options(new CARGO_DService().GetOptions());
   var vehicle=d.Resources.FirstOrDefault(x=>x.RESOURCE_ID==r.VEHICLE);var differences=new List<string>();
   if(u!=null){if(u.VEHICLE!=r.VEHICLE)differences.Add("車號");var planned=new[]{r.DRIVER1,r.DRIVER2}.Where(x=>!String.IsNullOrEmpty(x)).OrderBy(x=>x);var actual=new[]{u.DRIVER1,u.DRIVER2}.Where(x=>!String.IsNullOrEmpty(x)&&x!="SELF").OrderBy(x=>x);if(!planned.SequenceEqual(actual))differences.Add("駕駛人");}
   return new CARGO_D05DetailViewModel {Application=app,Difference=String.Join("、",differences),Form=new CARGO_D05FormViewModel {RequestId=r.REQUEST_ID,Version=r.VERSION_NO,VehicleType=u==null?(vehicle==null?null:vehicle.VEHICLE_TYPE):u.VEHICLE_TYPE,
    Vehicle=u==null?r.VEHICLE:u.VEHICLE,Driver1=u==null?(r.DRIVER1??(r.SELF_DRIVE==1?"SELF":null)):u.DRIVER1,Driver2=u==null?r.DRIVER2:u.DRIVER2,StartKm=u==null?(decimal?)null:u.START_KM,EndKm=u==null?(decimal?)null:u.END_KM},
    Resources=d.Resources.Select(x=>new CARGO_D05ResourceViewModel {Id=x.RESOURCE_ID,Name=x.NAME,Kind=x.KIND,Type=x.VEHICLE_TYPE}).ToList(),
    History=d.History.Select(x=>new CARGO_D05HistoryViewModel {Revision=x.REVISION_NO,VehicleType=x.VEHICLE_TYPE,Vehicle=x.VEHICLE,Driver1=Driver(x.DRIVER1),Driver2=Driver(x.DRIVER2),StartKm=x.START_KM,EndKm=x.END_KM,Distance=x.DISTANCE_KM,Actor=x.RECORDED_BY,At=x.RECORDED_AT}).ToList()};
  }
  public static CARGO_D05SaveDto Save(CARGO_D05FormViewModel m){return new CARGO_D05SaveDto {RequestId=m.RequestId,Version=m.Version,VehicleType=m.VehicleType,Vehicle=m.Vehicle,Driver1=m.Driver1,Driver2=m.Driver2,StartKm=m.StartKm,EndKm=m.EndKm};}
  public static List<CARGO_D06TaskViewModel> Tasks(List<CARGO_D06TaskDto> tasks) {
   var options=new CARGO_DService().GetOptions();
   return tasks.Select(d=>{var r=d.Request;var campus=options.FirstOrDefault(x=>x.OPTION_TYPE=="CAMPUS"&&x.CODE==r.CAMPUS);return new CARGO_D06TaskViewModel {
    DriverId=d.DriverId,DriverName=d.DriverName,Partner=d.Partner,Vehicle=d.VehicleName,From=d.Assignment.FROM_AT,To=d.Assignment.TO_AT,Kind=d.Assignment.KIND,
    RequestNo=r.REQUEST_NO,Category=r.CATEGORY=="routine"?"例行用車":"一般用車",Applicant=r.APPLICANT,Extension=r.EXT,Passengers=r.PAX,Campus=campus==null?r.CAMPUS:campus.LABEL,Pickup=r.PICKUP_LOCATION,Purpose=r.PURPOSE,PlanCode=r.PLAN_CODE,
    Contact=String.Join("／",new[]{r.LEADER,String.IsNullOrEmpty(r.LEADER_EXT)?null:"分機 "+r.LEADER_EXT,r.LEADER_PHONE}.Where(x=>!String.IsNullOrEmpty(x))),Status=CARGO_D01Mapper.Status(CARGO_D01Rules.DisplayStatus(r,DateTime.Now)),Waiting=r.CONTINUE_MATCHING==1,
    Locations=d.Locations.Select(x=>x.NAME).ToList(),Cargo=d.Cargo.Select(x=>new CARGO_D01CargoViewModel {Name=x.NAME,Length=x.LENGTH_CM,Width=x.WIDTH_CM,Height=x.HEIGHT_CM,Weight=x.WEIGHT_KG,Quantity=x.QUANTITY,Hazardous=x.HAZARDOUS==1}).ToList()};}).ToList();
  }
 }
}
