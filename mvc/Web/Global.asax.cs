using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace CARGO.Web
{
    public class MvcApplication : HttpApplication
    {
#if LOCAL_DEMO
        protected void Application_BeginRequest()
        {
            if (System.Configuration.ConfigurationManager.AppSettings["CARGO.LocalDemo"] != "true"
                || !Request.IsLocal || !Request.Url.IsLoopback)
                throw new HttpException(403, "Local demo is restricted to loopback requests and its staged configuration.");
        }
        protected void Application_PostAuthenticateRequest()
        {
            var principal = new System.Security.Principal.GenericPrincipal(
                new System.Security.Principal.GenericIdentity("示範申請人", "CARGO.LocalDemo"),
                new[] { "部長秘書", "單位主管", "業務", "運輸主管" });
            Context.User = principal;
            System.Threading.Thread.CurrentPrincipal = principal;
        }
#endif
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.MapRoute("Default", "{controller}/{action}/{id}",
                new { controller = "CARGO_D01", action = "Index", id = UrlParameter.Optional });
        }
    }
}
