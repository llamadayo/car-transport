using System;
using System.Linq;
using System.Web.Mvc;
using VD.Core;
using VD.Services;
using VD.Web.Models;
namespace VD.Web.Controllers {
 public abstract class BaseController : Controller {
  protected DatabaseSettings Settings => DatabaseSettings.FromConfiguration();
  protected PageModel Page(string title) => new PageModel { Title=title,Today=new TaipeiClock().Now.Date,Master=new MasterDataService(Settings).Read() };
  protected JsonResult Result(object value) => new JsonResult { Data=new {success=true,data=value},JsonRequestBehavior=JsonRequestBehavior.AllowGet,MaxJsonLength=int.MaxValue };
  protected void ValidInput() { if(!ModelState.IsValid) throw new ArgumentException(string.Join("；",ModelState.Values.SelectMany(v=>v.Errors).Select(e=>string.IsNullOrEmpty(e.ErrorMessage)?"欄位格式不正確。":e.ErrorMessage))); }
  protected override void OnException(ExceptionContext context) {
   if(!Request.IsAjaxRequest()) { base.OnException(context);return; }
   bool input=context.Exception is ArgumentException || context.Exception is System.Web.Mvc.HttpAntiForgeryException;
   if(!input) System.Diagnostics.Trace.TraceError(context.Exception.ToString());
   Response.StatusCode=input?400:500;Response.TrySkipIisCustomErrors=true;
   context.Result=new JsonResult {Data=new {success=false,message=input?context.Exception.Message:"處理失敗，請稍後重試或聯絡管理員。"},JsonRequestBehavior=JsonRequestBehavior.AllowGet};
   context.ExceptionHandled=true;
  }
 }
}
