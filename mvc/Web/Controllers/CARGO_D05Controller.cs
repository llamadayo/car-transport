using System;
using System.Linq;
using System.Web.Mvc;
using CARGO.DTO;
using CARGO.Service;
using CARGO.Web.Models;
using CARGO.Web.ViewModels.CARGO_D05;
using Kendo.Mvc.UI;
namespace CARGO.Web.Controllers {
 [Authorize(Roles="業務")]
 public sealed class CARGO_D05Controller:Controller {
  private readonly CARGO_D05Service service=new CARGO_D05Service();
  private CARGO_DUsageActorDto Actor(){return new CARGO_DUsageActorDto {UserId=User.Identity.Name,IsBusiness=User.IsInRole("業務")};}
  private void Page(){ViewBag.Module=5;ViewBag.Title="車輛使用實登（業務）";}
  [HttpGet] public ActionResult Index(CARGO_D05IndexViewModel query=null){Page();return View(query??new CARGO_D05IndexViewModel());}
  [HttpPost,ValidateAntiForgeryToken] public ActionResult Read([DataSourceRequest] DataSourceRequest request,[Bind(Prefix="")] CARGO_D05IndexViewModel criteria){
   if(!ModelState.IsValid||(request.Filters!=null&&request.Filters.Count>0)||(request.Sorts!=null&&request.Sorts.Count>0)||(request.Groups!=null&&request.Groups.Count>0))return Json(new DataSourceResult {Errors="請使用上方查詢條件"});
   var page=service.Query(new CARGO_D05QueryDto {Keyword=criteria.Keyword,Status=criteria.Status,Page=request.Page,PageSize=request.PageSize},Actor());
   var ids=page.Items.SelectMany(x=>x.Usage==null?new[]{x.Request.VEHICLE,x.Request.DRIVER1,x.Request.DRIVER2}:new[]{x.Usage.VEHICLE,x.Usage.DRIVER1,x.Usage.DRIVER2});
   var resources=new CARGO_DService().GetResourceLabels(ids);
   return Json(new {Data=page.Items.Select(x=>CARGO_D05Mapper.Row(x,resources)),Total=page.Total,Pending=page.Pending});
  }
  [HttpGet] public ActionResult Detail(long id,CARGO_D05IndexViewModel query=null){var d=service.Detail(id,Actor());if(d==null)return HttpNotFound();var page=CARGO_D05Mapper.Detail(d);page.ReturnQuery=query??new CARGO_D05IndexViewModel();Page();return View(page);}
  [HttpPost,ValidateAntiForgeryToken] public ActionResult Save([Bind(Prefix="Form")] CARGO_D05FormViewModel model,[Bind(Prefix="ReturnQuery")] CARGO_D05IndexViewModel query=null){
   query=query??new CARGO_D05IndexViewModel();
   if(ModelState.IsValid){try {service.Save(CARGO_D05Mapper.Save(model),Actor());TempData["Message"]="實登已儲存，前次紀錄保留於歷程。";return RedirectToAction("Detail",new {id=model.RequestId,Keyword=query.Keyword,Status=query.Status});}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);}}
   var d=service.Detail(model.RequestId,Actor());if(d==null)return HttpNotFound();var page=CARGO_D05Mapper.Detail(d);page.Form=model;page.ReturnQuery=query;Page();return View("Detail",page);
  }
 }
}
