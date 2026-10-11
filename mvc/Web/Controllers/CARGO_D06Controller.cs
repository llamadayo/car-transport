using System;
using System.Linq;
using System.Web.Mvc;
using CARGO.DTO;
using CARGO.Service;
using CARGO.Web.Models;
using CARGO.Web.ViewModels.CARGO_D06;
namespace CARGO.Web.Controllers {
 [Authorize(Roles="駕駛,業務")]
 public sealed class CARGO_D06Controller:Controller {
  private readonly CARGO_D06Service service=new CARGO_D06Service();
  [HttpGet] public ActionResult Detail(long id,long start,string driver,[Bind(Include="FromDate,ToDate,DriverId")] CARGO_D06IndexViewModel criteria=null) {
   if(!ModelState.IsValid) return new HttpStatusCodeResult(400);
   var actor=new CARGO_DUsageActorDto {UserId=User.Identity.Name,IsBusiness=User.IsInRole("業務"),IsDriver=User.IsInRole("駕駛")};
   // Resolve from authorized tasks, never from an unrestricted application lookup.
   var task=service.Tasks(actor).FirstOrDefault(x=>x.Request.REQUEST_ID==id && x.Assignment.FROM_AT.Ticks==start && x.DriverId==driver);
   if(task==null) return HttpNotFound();
   var model=CARGO_D06Mapper.Tasks(new System.Collections.Generic.List<CARGO_D06TaskDto>{task}).Single();
   model.ReturnQuery=criteria??new CARGO_D06IndexViewModel();
   ViewBag.Module=6;ViewBag.Title="司機任務單（駕駛）";
   return View(model);
  }
  [HttpGet] public ActionResult Index([Bind(Include="FromDate,ToDate,DriverId")] CARGO_D06IndexViewModel criteria=null){
   criteria=criteria??new CARGO_D06IndexViewModel();
   ViewBag.Module=6;ViewBag.Title="司機任務單（駕駛）";ViewBag.IsBusiness=User.IsInRole("業務");
   var actor=new CARGO_DUsageActorDto {UserId=User.Identity.Name,IsBusiness=User.IsInRole("業務"),IsDriver=User.IsInRole("駕駛")};
   if(ModelState.IsValid && criteria.FromDate.HasValue && criteria.ToDate.HasValue && criteria.FromDate.Value.Date>criteria.ToDate.Value.Date)
    ModelState.AddModelError("ToDate","日期迄不可早於日期起");
   var query=ModelState.IsValid?new CARGO_D06QueryDto {FromDate=criteria.FromDate,ToDate=criteria.ToDate,DriverId=criteria.DriverId}:new CARGO_D06QueryDto();
   var page=service.Query(query,actor);
   criteria.Drivers=page.Drivers.Select(x=>new SelectListItem {Value=x.Id,Text=x.Name}).ToList();
   criteria.Tasks=ModelState.IsValid?CARGO_D06Mapper.Tasks(page.Items):new System.Collections.Generic.List<CARGO_D06TaskViewModel>();
   return View(criteria);
  }
 }
}
