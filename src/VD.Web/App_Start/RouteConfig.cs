using System.Web.Mvc;
using System.Web.Routing;
namespace VD.Web { public static class RouteConfig {
 public static void RegisterRoutes(RouteCollection routes) {
  routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
  routes.MapRoute("Default","{controller}/{action}/{id}",new {controller="ModuleA",action="Apply",id=UrlParameter.Optional});
 }
} }
