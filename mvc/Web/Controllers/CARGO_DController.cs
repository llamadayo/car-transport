using System.Web.Mvc;
using CARGO.Service;
using CARGO.Web.Models;

namespace CARGO.Web.Controllers
{
    // Independent shared endpoints, not a base controller.
    [Authorize]
    public sealed class CARGO_DController : Controller
    {
        [HttpGet]
        public ActionResult Options()
        { return Json(CARGO_D01Mapper.Options(new CARGO_DService().GetOptions()), JsonRequestBehavior.AllowGet); }
    }
}

