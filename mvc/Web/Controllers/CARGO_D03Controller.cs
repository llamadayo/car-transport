using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CARGO.DTO;
using CARGO.Service;
using CARGO.Web.Models;
using CARGO.Web.ViewModels.CARGO_D03;
using Kendo.Mvc.UI;
namespace CARGO.Web.Controllers {
    [Authorize(Roles = "業務")]
    public sealed class CARGO_D03Controller : Controller {
        private readonly CARGO_D03Service service = new CARGO_D03Service();
        private CARGO_DStaffDto Actor() { return new CARGO_DStaffDto { UserId = User.Identity.Name, CanApprove = User.IsInRole("單位主管"), CanDispatch = User.IsInRole("業務"), CanSign = User.IsInRole("運輸主管") }; }
        private void Page() { ViewBag.Module = 3; ViewBag.Title = "派車調度（業務）"; }
        [HttpGet] public ActionResult Index() { Page(); return View(new CARGO_D03IndexViewModel()); }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Read([DataSourceRequest] DataSourceRequest request, [Bind(Prefix = "")] CARGO_D03IndexViewModel criteria) {
            if (!ModelState.IsValid || (request.Filters != null && request.Filters.Count > 0) || (request.Groups != null && request.Groups.Count > 0) || (request.Sorts != null && request.Sorts.Count > 0)) return Json(new DataSourceResult { Errors = "請使用上方查詢條件" });
            var page = service.Query(new CARGO_DWorkQueryDto { Applicant = criteria.Applicant, Status = criteria.Status, UseDate = criteria.UseDate, Page = request.Page, PageSize = request.PageSize }, Actor());
            return Json(new DataSourceResult { Data = page.Items.Select(x => CARGO_D03Mapper.Row(x)), Total = page.Total });
        }
        [HttpGet] public ActionResult Detail(long id) {
            var data = service.Detail(id, Actor()); if (data == null) return HttpNotFound();
            Page(); return View(CARGO_D03Mapper.Detail(data));
        }
        [HttpPost, ValidateAntiForgeryToken] public ActionResult Change(CARGO_D03ActionViewModel model) {
            if (!ModelState.IsValid) { TempData["Error"] = "操作資料格式不正確"; return RedirectToAction("Detail", new { id = model.RequestId }); }
            try { service.Change(new List<CARGO_DWorkActionDto> { CARGO_D03Mapper.Action(model) }, Actor()); TempData["Message"] = "操作完成。"; }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            catch (UnauthorizedAccessException) { return new HttpStatusCodeResult(403); }
            return RedirectToAction("Detail", new { id = model.RequestId });
        }
        [HttpPost, ValidateAntiForgeryToken] public ActionResult Backfill() {
    try { TempData["Message"] = String.Join("；", service.Backfill(Actor())); }
    catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
    return RedirectToAction("Index");
}
    }
}
