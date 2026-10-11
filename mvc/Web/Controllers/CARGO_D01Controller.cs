using System;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using CARGO.DTO;
using CARGO.Service;
using CARGO.Web.Models;
using CARGO.Web.ViewModels.CARGO_D01;
using Kendo.Mvc.UI;

namespace CARGO.Web.Controllers
{
    [Authorize]
    public sealed class CARGO_D01Controller : Controller
    {
        private readonly CARGO_D01Service service = new CARGO_D01Service();
        private CARGO_DActorDto Actor()
        {
            // Supplied by the host application. No login scheme or demo identity is introduced here.
            return new CARGO_DActorDto { UserId = User.Identity.Name,
                CanChooseRoutine = User.IsInRole("總經理") || User.IsInRole("部長秘書") };
        }
        private void Populate(CARGO_D01FormViewModel model)
        {
            model.CanChooseRoutine = Actor().CanChooseRoutine;
            model.Options = CARGO_D01Mapper.Options(new CARGO_DService().GetOptions());
        }
        [HttpGet]
        public ActionResult Index() { return View(new CARGO_D01IndexViewModel()); }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Read([DataSourceRequest] DataSourceRequest request, [Bind(Prefix = "")] CARGO_D01IndexViewModel criteria)
        {
            if (!ModelState.IsValid) return Json(new DataSourceResult { Errors = "查詢條件格式不正確" });
            // Filtering uses the explicit query form. Do not silently accept unsupported grid descriptors.
            if ((request.Filters != null && request.Filters.Count > 0) || (request.Groups != null && request.Groups.Count > 0))
                return Json(new DataSourceResult { Errors = "請使用上方查詢條件" });
            var sort = request.Sorts == null ? null : request.Sorts.FirstOrDefault();
            var page = service.Query(new CARGO_D01QueryDto { Applicant = criteria.Applicant, UseDate = criteria.UseDate, Status = criteria.Status,
                Page = request.Page, PageSize = request.PageSize, SortField = sort == null ? "CreatedAt" : sort.Member,
                Descending = sort == null || sort.SortDirection == ListSortDirection.Descending }, Actor());
            return Json(new DataSourceResult { Data = page.Items.Select(CARGO_D01Mapper.Row), Total = page.Total });
        }
        [HttpGet]
        public ActionResult Create()
        {
            var model = new CARGO_D01FormViewModel { Applicant = User.Identity.Name,
                StartAt = DateTime.Today.AddDays(1).AddHours(9), EndAt = DateTime.Today.AddDays(1).AddHours(12) };
            Populate(model); return View(model);
        }
        [HttpGet]
        public ActionResult Edit(long id)
        {
            var detail = service.Detail(id, Actor());
            if (detail == null) return HttpNotFound();
            if (!CARGO_D01Rules.CanEdit(detail.Application.Request)) return new HttpStatusCodeResult(409, "Application is not editable");
            var model = CARGO_D01Mapper.Form(detail.Application); Populate(model); return View(model);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Save(CARGO_D01FormViewModel model, string command)
        {
            if (command != "draft" && command != "submit") ModelState.AddModelError("", "不支援的申請操作");
            if (ModelState.IsValid)
            {
                try
                {
                    var id = service.Save(CARGO_D01Mapper.ToDto(model), Actor(), command == "submit");
                    TempData["Message"] = command == "submit" ? "已送出申請，待二級審。" : "已暫存，尚未送審。";
                    return RedirectToAction("Detail", new { id });
                }
                catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
            }
            // Redisplay only server-owned metadata, never trust posted status or routine permissions.
            if (model.RequestId != 0)
            {
                var existing = service.Detail(model.RequestId, Actor());
                if (existing == null) return HttpNotFound();
                model.Status = existing.Application.Request.STATUS;
                model.RequestNo = existing.Application.Request.REQUEST_NO;
                model.ReviewNote = existing.Application.Request.REVIEW_NOTE;
            }
            Populate(model); return View(model.RequestId == 0 ? "Create" : "Edit", model);
        }
        [HttpGet]
        public ActionResult Detail(long id)
        {
            var detail = service.Detail(id, Actor());
            if (detail == null) return HttpNotFound();
            var resources = new CARGO_DService().GetResourceLabels(CARGO_D01Mapper.ResourceIds(detail));
            var model = CARGO_D01Mapper.Detail(detail, resources);
            Populate(model.Form);
            return View(model);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Withdraw(CARGO_D01ActionViewModel model)
        { return Change(model, false); }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EarlyReturn(CARGO_D01ActionViewModel model)
        { return Change(model, true); }
        private ActionResult Change(CARGO_D01ActionViewModel model, bool earlyReturn)
        {
            if (!ModelState.IsValid) { TempData["Error"] = "操作資料格式不正確"; return RedirectToAction("Detail", new { id = model.RequestId }); }
            try
            {
                var data = new CARGO_D01ActionDto { RequestId = model.RequestId, Version = model.Version, ReturnAt = model.ReturnAt, Reason = model.Reason };
                if (earlyReturn) service.RequestEarlyReturn(data, Actor()); else service.Withdraw(data, Actor());
                TempData["Message"] = earlyReturn ? "已提出提前歸還，原用車期間保留至調度確認。" : "已撤回為申請中，重送須重新審核。";
            }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction("Detail", new { id = model.RequestId });
        }
    }
}
