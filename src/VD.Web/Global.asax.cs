using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using VD.Services;
namespace VD.Web {
 public class MvcApplication : HttpApplication {
  protected void Application_Start() {
   AppDomain.CurrentDomain.SetData("DataDirectory",Server.MapPath("~/App_Data"));
   AreaRegistration.RegisterAllAreas();RouteConfig.RegisterRoutes(RouteTable.Routes);
   var settings=DatabaseSettings.FromConfiguration();
   if(!settings.IsOracle) new DatabaseSetupService(settings).InitializeSqlite(Server.MapPath("~/App_Data"));
  }
 }
}
