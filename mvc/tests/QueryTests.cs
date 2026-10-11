using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CARGO.Web.Controllers;
using CARGO.Web.ViewModels.CARGO_D02;
using CARGO.Web.ViewModels.CARGO_D04;
using CARGO.Web.ViewModels.CARGO_D06;
using Kendo.Mvc.UI;

class QueryTests {
 class Context : HttpContextBase {
  readonly IDictionary items=new Hashtable();
  public override IDictionary Items {get{return items;}}
  public override IPrincipal User {get{return new GenericPrincipal(new GenericIdentity("示範申請人"),new[]{"單位主管","運輸主管","業務"});} set{}}
 }
 static T Controller<T>() where T:Controller,new() {
  var c=new T();c.ControllerContext=new ControllerContext(new Context(),new RouteData(),c);return c;
 }
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static CARGO_D06IndexViewModel Bind(CARGO_D06Controller c,NameValueCollection values) {
  return (CARGO_D06IndexViewModel)new DefaultModelBinder().BindModel(c.ControllerContext,new ModelBindingContext {
   ModelMetadata=ModelMetadataProviders.Current.GetMetadataForType(null,typeof(CARGO_D06IndexViewModel)),
   ModelState=c.ModelState,ValueProvider=new NameValueCollectionValueProvider(values,CultureInfo.InvariantCulture)
  });
 }
 static void Main(string[] args) {
  var page=new DataSourceRequest {Page=1,PageSize=20};
  var review=(DataSourceResult)((JsonResult)Controller<CARGO_D02Controller>().Read(page,new CARGO_D02IndexViewModel {Status="submitted"})).Data;
  var row=review.Data.Cast<CARGO_D02RowViewModel>().Single();
  Check(row.Approver=="示範申請人" && row.SelfDrive=="否" && row.Cargo=="0 項／0 件","D02 endpoint includes review summary");
  var sign=(DataSourceResult)((JsonResult)Controller<CARGO_D04Controller>().Read(page,new CARGO_D04IndexViewModel {Keyword="gu000007"})).Data;
  Check(sign.Total==1 && sign.Data.Cast<CARGO_D04RowViewModel>().Single().RequestId==7,"D04 endpoint forwards keyword");
  var c=Controller<CARGO_D06Controller>();
  var criteria=Bind(c,new NameValueCollection());
  Check(criteria.FromDate==DateTime.Today && criteria.ToDate==DateTime.Today.AddDays(14),"D06 GET binding retains initial date defaults");
  var tasks=(CARGO_D06IndexViewModel)((ViewResult)c.Index(criteria)).Model;
  Check(tasks.Tasks.Count==1 && tasks.Drivers.Count==1,"D06 initial GET populates tasks and choices");
  c=Controller<CARGO_D06Controller>();
  criteria=Bind(c,new NameValueCollection { {"FromDate",""},{"ToDate",""},{"DriverId","示範駕駛"} });
  tasks=(CARGO_D06IndexViewModel)((ViewResult)c.Index(criteria)).Model;
  Check(tasks.FromDate==null && tasks.ToDate==null && tasks.Tasks.Count==1,"D06 blank GET dates stay unrestricted");
  c=Controller<CARGO_D06Controller>();
  criteria=Bind(c,new NameValueCollection { {"FromDate","2030-01-02"},{"ToDate","2030-01-01"} });
  tasks=(CARGO_D06IndexViewModel)((ViewResult)c.Index(criteria)).Model;
  Check(!c.ModelState.IsValid && tasks.Tasks.Count==0 && tasks.Drivers.Count==1,"D06 reversed GET dates show errors and retain choices");
  c=Controller<CARGO_D06Controller>();
  criteria=Bind(c,new NameValueCollection { {"FromDate","invalid"},{"ToDate",""} });
  tasks=(CARGO_D06IndexViewModel)((ViewResult)c.Index(criteria)).Model;
  Check(!c.ModelState.IsValid && tasks.Tasks.Count==0,"D06 invalid date does not fall back to unfiltered results");
  // Generate only the changed Razor views without loading application configuration or a web server.
  foreach(var module in new[]{"CARGO_D02","CARGO_D04","CARGO_D06"}) {
   var path=Path.Combine(args[0],"Web","Views",module,"Index.cshtml");
   var host=new System.Web.Mvc.Razor.MvcWebPageRazorHost("~/Views/"+module+"/Index.cshtml",path);
   foreach(var ns in new[]{"System","System.Linq","System.Collections.Generic","System.Web.Mvc","System.Web.Mvc.Html","Kendo.Mvc.UI"}) host.NamespaceImports.Add(ns);
   using(var reader=File.OpenText(path)) {
    var result=new System.Web.Razor.RazorTemplateEngine(host).GenerateCode(reader,module+"Index","QueryViews",path);
    if(!result.Success) throw new Exception(String.Join("; ",result.ParserErrors.Select(x=>x.Message)));
    using(var writer=File.CreateText(Path.Combine(args[1],module+"Index.cs")))
    using(var provider=new Microsoft.CSharp.CSharpCodeProvider()) provider.GenerateCodeFromCompileUnit(result.GeneratedCode,writer,new System.CodeDom.Compiler.CodeGeneratorOptions());
   }
   Console.WriteLine("PASS "+module+" Razor syntax");
  }
 }
}
