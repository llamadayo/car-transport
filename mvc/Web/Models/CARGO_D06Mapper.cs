using System;
using System.Linq;
using System.Collections.Generic;
using CARGO.DTO;
using CARGO.Service;
using CARGO.Web.ViewModels.CARGO_D06;
using CARGO.Web.ViewModels.CARGO_D01;
namespace CARGO.Web.Models {
 public static class CARGO_D06Mapper {
  public static List<CARGO_D06TaskViewModel> Tasks(List<CARGO_D06TaskDto> tasks) {
   var options=new CARGO_DService().GetOptions();
   return tasks.Select(d=>{var r=d.Request;var campus=options.FirstOrDefault(x=>x.OPTION_TYPE=="CAMPUS"&&x.CODE==r.CAMPUS);return new CARGO_D06TaskViewModel {
    RequestId=r.REQUEST_ID,DriverId=d.DriverId,DriverName=d.DriverId=="SELF"?"自駕":d.DriverName,Partner=d.Partner,Vehicle=d.VehicleName,From=d.Assignment.FROM_AT,To=d.Assignment.TO_AT,Kind=d.Assignment.KIND,
    RequestNo=r.REQUEST_NO,Category=r.CATEGORY=="routine"?"例行用車":"一般用車",Applicant=r.APPLICANT,Extension=r.EXT,Passengers=r.PAX,Campus=campus==null?r.CAMPUS:campus.LABEL,Pickup=r.PICKUP_LOCATION,Purpose=r.PURPOSE,PlanCode=r.PLAN_CODE,
    Contact=String.Join("／",new[]{r.LEADER,String.IsNullOrEmpty(r.LEADER_EXT)?null:"分機 "+r.LEADER_EXT,r.LEADER_PHONE}.Where(x=>!String.IsNullOrEmpty(x))),Status=CARGO_D01Mapper.Status(CARGO_D01Rules.DisplayStatus(r,DateTime.Now)),Waiting=r.CONTINUE_MATCHING==1,HazardousCargo=r.HAZARDOUS_CARGO==1,
    Locations=d.Locations.Select(x=>x.NAME).ToList(),Cargo=d.Cargo.Select(x=>new CARGO_D01CargoViewModel {Name=x.NAME,Length=x.LENGTH_CM,Width=x.WIDTH_CM,Height=x.HEIGHT_CM,Weight=x.WEIGHT_KG,Quantity=x.QUANTITY}).ToList()};}).ToList();
  }
 }
}
