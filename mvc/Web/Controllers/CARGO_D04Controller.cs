using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using CARGO.DTO;
using CARGO.Service;
using CARGO.Web.Models;
using CARGO.Web.ViewModels.CARGO_D04;
using Kendo.Mvc.UI;
namespace CARGO.Web.Controllers {
    [Authorize(Roles = "運輸主管")]
    public sealed class CARGO_D04Controller : Controller {
        private readonly CARGO_D04Service service = new CARGO_D04Service();
        private CARGO_DStaffDto Actor() { return new CARGO_DStaffDto { UserId = User.Identity.Name, CanApprove = User.IsInRole("單位主管"), CanDispatch = User.IsInRole("業務"), CanSign = User.IsInRole("運輸主管") }; }
        private void Page() { ViewBag.Module = 4; ViewBag.Title = "運輸主管簽審（運輸主管）"; }
        [HttpGet] public ActionResult Index() { Page(); return View(new CARGO_D04IndexViewModel()); }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Read([DataSourceRequest] DataSourceRequest request, [Bind(Prefix = "")] CARGO_D04IndexViewModel criteria) {
            if (!ModelState.IsValid || (request.Filters != null && request.Filters.Count > 0) || (request.Groups != null && request.Groups.Count > 0) || (request.Sorts != null && request.Sorts.Count > 0)) return Json(new DataSourceResult { Errors = "請使用上方查詢條件" });
            var page = service.Query(new CARGO_DWorkQueryDto { Keyword = criteria.Keyword, Status = criteria.Status, UseDate = criteria.UseDate, Page = request.Page, PageSize = request.PageSize }, Actor());
            return Json(new DataSourceResult { Data = page.Items.Select(x => CARGO_D04Mapper.Row(x)), Total = page.Total });
        }
        [HttpGet] public ActionResult Detail(long id) {
            var data = service.Detail(id, Actor()); if (data == null) return HttpNotFound();
            Page(); return View(CARGO_D04Mapper.Detail(data));
        }
        [HttpPost, ValidateAntiForgeryToken] public ActionResult Change(CARGO_D04ActionViewModel model) {
            if (!ModelState.IsValid) { TempData["Error"] = "操作資料格式不正確"; return RedirectToAction("Detail", new { id = model.RequestId }); }
            try { service.Change(new List<CARGO_DWorkActionDto> { CARGO_D04Mapper.Action(model) }, Actor()); TempData["Message"] = "操作完成。"; }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            catch (UnauthorizedAccessException) { return new HttpStatusCodeResult(403); }
            return RedirectToAction("Detail", new { id = model.RequestId });
        }
        [HttpPost, ValidateAntiForgeryToken] public ActionResult Batch(List<CARGO_D04ActionViewModel> items) {
    if (!ModelState.IsValid || items == null) { TempData["Error"] = "請先查詢並選取待審資料"; return RedirectToAction("Index"); }
    try { var actions = items.Select(CARGO_D04Mapper.Action).ToList(); foreach (var a in actions) a.Command = "approve";
        service.Change(actions, Actor()); TempData["Message"] = "已核准 " + actions.Count + " 筆。"; }
    catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
    catch (UnauthorizedAccessException) { return new HttpStatusCodeResult(403); }
    return RedirectToAction("Index");
}
    }
}
