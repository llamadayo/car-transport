using System;
using System.Linq;
using System.Collections.Generic;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.ViewModels.CARGO_D05;
namespace CARGO.Web.Models {
 public static class CARGO_D05Mapper {
  public static CARGO_D05RowViewModel Row(CARGO_D05RowDto d, IEnumerable<CARGO_D_RESOURCE> resources = null) {var r=d.Request;var u=d.Usage;return new CARGO_D05RowViewModel {RequestId=r.REQUEST_ID,DispatchNo=r.DISPATCH_NO,UseDate=r.START_AT,
   VehicleType=u==null?d.PlannedVehicleType:u.VEHICLE_TYPE,Vehicle=CARGO_D01Mapper.ResourceLabel(resources,"VEHICLE",u==null?r.VEHICLE:u.VEHICLE),Driver1=CARGO_D01Mapper.ResourceLabel(resources,"DRIVER",u==null?(r.DRIVER1??(r.SELF_DRIVE==1?"SELF":null)):u.DRIVER1),Driver2=CARGO_D01Mapper.ResourceLabel(resources,"DRIVER",u==null?r.DRIVER2:u.DRIVER2),
   FlowStatus=CARGO_D01Mapper.Status(CARGO_D01Rules.DisplayStatus(r,DateTime.Now)),UsageStatus=u==null?"待實登":"已實登",Distance=u==null?(decimal?)null:u.DISTANCE_KM};}
  public static CARGO_D05DetailViewModel Detail(CARGO_D05DetailDto d) {
   var r=d.Application.Application.Request;var u=d.History.FirstOrDefault();var app=CARGO_DSharedMapper.Application(d.Application,d.Resources);
   var vehicle=d.Resources.FirstOrDefault(x=>x.RESOURCE_ID==r.VEHICLE);var differences=new List<string>();
   if(u!=null){if(u.VEHICLE!=r.VEHICLE)differences.Add("車號");var planned=new[]{r.DRIVER1,r.DRIVER2}.Where(x=>!String.IsNullOrEmpty(x)).OrderBy(x=>x);var actual=new[]{u.DRIVER1,u.DRIVER2}.Where(x=>!String.IsNullOrEmpty(x)&&x!="SELF").OrderBy(x=>x);if(!planned.SequenceEqual(actual))differences.Add("駕駛人");}
   return new CARGO_D05DetailViewModel {Application=app,Difference=String.Join("、",differences),Form=new CARGO_D05FormViewModel {RequestId=r.REQUEST_ID,Version=r.VERSION_NO,VehicleType=u==null?(vehicle==null?null:vehicle.VEHICLE_TYPE):u.VEHICLE_TYPE,
    Vehicle=u==null?r.VEHICLE:u.VEHICLE,Driver1=u==null?(r.DRIVER1??(r.SELF_DRIVE==1?"SELF":null)):u.DRIVER1,Driver2=u==null?r.DRIVER2:u.DRIVER2,StartKm=u==null?(decimal?)null:u.START_KM,EndKm=u==null?(decimal?)null:u.END_KM},
    Resources=d.Resources.Select(x=>new CARGO_D05ResourceViewModel {Id=x.RESOURCE_ID,Name=x.NAME,Kind=x.KIND,Type=x.VEHICLE_TYPE}).ToList(),
    History=d.History.Select(x=>new CARGO_D05HistoryViewModel {Revision=x.REVISION_NO,VehicleType=x.VEHICLE_TYPE,Vehicle=CARGO_D01Mapper.ResourceLabel(d.Resources,"VEHICLE",x.VEHICLE),Driver1=CARGO_D01Mapper.ResourceLabel(d.Resources,"DRIVER",x.DRIVER1),Driver2=CARGO_D01Mapper.ResourceLabel(d.Resources,"DRIVER",x.DRIVER2),StartKm=x.START_KM,EndKm=x.END_KM,Distance=x.DISTANCE_KM,Actor=x.RECORDED_BY,At=x.RECORDED_AT}).ToList()};
  }
  public static CARGO_D05SaveDto Save(CARGO_D05FormViewModel m){return new CARGO_D05SaveDto {RequestId=m.RequestId,Version=m.Version,VehicleType=m.VehicleType,Vehicle=m.Vehicle,Driver1=m.Driver1,Driver2=m.Driver2,StartKm=m.StartKm,EndKm=m.EndKm};}

 }
}
