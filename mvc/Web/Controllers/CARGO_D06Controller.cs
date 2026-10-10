using System;
using System.Linq;
using System.Web.Mvc;
using CARGO.DTO;
using CARGO.Service;
using CARGO.Web.Models;
using CARGO.Web.ViewModels.CARGO_D06;
using Kendo.Mvc.UI;
namespace CARGO.Web.Controllers {
 [Authorize(Roles="駕駛,業務")]
 public sealed class CARGO_D06Controller:Controller {
  private readonly CARGO_D06Service service=new CARGO_D06Service();
  [HttpGet] public ActionResult Index(){ViewBag.Module=6;ViewBag.Title="司機任務單（駕駛）";ViewBag.IsBusiness=User.IsInRole("業務");return View(CARGO_D06Mapper.Tasks(service.Tasks(new CARGO_DUsageActorDto {UserId=User.Identity.Name,IsBusiness=User.IsInRole("業務"),IsDriver=User.IsInRole("駕駛")})));}
 }
}
