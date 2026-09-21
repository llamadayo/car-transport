using System.Collections.Generic;
using System.Web.Mvc;
using VD.Core;
using VD.Services;
namespace VD.Web.Controllers {
 public sealed class ModuleAController : BaseController {
  private RegionalQueryService Queries => new RegionalQueryService(Settings);
  private RegionalApplicationService Applications => new RegionalApplicationService(Settings);
  private RegionalDispatchService DispatchService => new RegionalDispatchService(Settings);
  [HttpGet] public ActionResult Apply() => View(Page("區域內物流 · 收貨申請（使用者）"));
  [HttpGet] public ActionResult Tracking() => View(Page("區域內物流 · 車次追蹤（業務單位）"));
  [HttpGet] public ActionResult Dispatch() => View(Page("區域內物流 · 已排定車次異動（業務單位）"));
  [HttpGet] public ActionResult Cards() => View(Page("區域內物流 · 資訊卡試做（Masonry）"));
  [HttpGet] public ActionResult Driver() => View(Page("區域內物流 · 司機任務單（駕駛）"));
  [HttpGet] public JsonResult ReadApplications(ApplicationQuery query) => Result(Queries.ReadApplications(query));
  [HttpGet] public JsonResult GetApplication(long id) {ValidInput();return Result(Queries.GetApplication(id));}
  [HttpPost,ValidateAntiForgeryToken] public JsonResult Submit(ApplicationInput input) {ValidInput();return Result(Applications.Submit(input));}
  [HttpPost,ValidateAntiForgeryToken] public JsonResult Rematch(long id,List<CargoItem> items) {ValidInput();return Result(Applications.UpdateAndRematch(id,items));}
  [HttpPost,ValidateAntiForgeryToken] public JsonResult SeedDemo() => Result(Applications.SeedDemo());
  [HttpPost,ValidateAntiForgeryToken] public JsonResult ReportIncident(long id,string reason) {ValidInput();return Result(Applications.ReportIncident(id,reason));}
  [HttpPost,ValidateAntiForgeryToken] public JsonResult ConfirmDelivery(long id,string by) {ValidInput();return Result(Applications.ConfirmDelivery(id,by));}
  [HttpGet] public JsonResult ReadTrips(TripQuery query) => Result(Queries.ReadTrips(query));
  [HttpGet] public JsonResult GetTrip(string date,string shiftId) => Result(Queries.GetTrip(date,shiftId));
  [HttpGet] public JsonResult ReadAssignableApplications(string date,string shiftId) => Result(Queries.ReadAssignableApplications(date,shiftId));
  [HttpPost,ValidateAntiForgeryToken] public JsonResult SaveTripPlan(string date,string shiftId,string vehicleId,string driverId) => Result(DispatchService.SaveTripPlan(date,shiftId,vehicleId,driverId));
  [HttpPost,ValidateAntiForgeryToken] public JsonResult ReassignShift(long id,string date,string shiftId) {ValidInput();return Result(DispatchService.ReassignShift(id,date,shiftId));}
  [HttpPost,ValidateAntiForgeryToken] public JsonResult RemoveFromShift(long id) {ValidInput();return Result(DispatchService.RemoveFromShift(id));}
  [HttpGet] public JsonResult ReadDriverTasks() => Result(Queries.ReadDriverTasks());
  [HttpGet] public JsonResult ReadRoutes() => Result(Queries.ReadRoutes(BusinessTime.Date(new TaipeiClock().Now)));
 }
}
