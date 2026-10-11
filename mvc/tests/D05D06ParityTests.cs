using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CARGO.DTO.Tables;
using CARGO.Service;
using CARGO.Web.Controllers;
using CARGO.Web.ViewModels.CARGO_D05;
class D05D06ParityTests {
 static int count;
 static void Check(bool value,string name){if(!value)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 class Context:HttpContextBase {
  readonly IDictionary items=new Hashtable();
  public override IDictionary Items {get{return items;}}
  public override IPrincipal User {get{return new GenericPrincipal(new GenericIdentity("示範申請人"),new[]{"業務"});} set{}}
 }
 static CARGO_D05Controller Controller(){var c=new CARGO_D05Controller();c.ControllerContext=new ControllerContext(new Context(),new RouteData(),c);return c;}
 static void Main(){try{Run();}catch(Exception ex){Console.WriteLine(ex.GetType().Name+": "+ex.Message);Environment.Exit(1);}}
 static void Run(){
  var query=new CARGO_D05IndexViewModel {Keyword="GU000005 & <測試>",Status="todo"};
  var controller=Controller();var detail=(CARGO_D05DetailViewModel)((ViewResult)controller.Detail(5,query)).Model;
  var first=detail.DriverOptions(true);var second=detail.DriverOptions(false);first.Single(x=>x.Value==detail.Form.Driver1).Selected=true;
  Check(second.All(x=>!x.Selected)&&String.IsNullOrEmpty(detail.Form.Driver2),"single-driver default does not leak selection into Driver2");
  Check(first.Zip(second,(a,b)=>!Object.ReferenceEquals(a,b)).All(x=>x),"driver selects use independent option instances");
  detail.Application.Form.SelfDrive=true;
  Check(detail.DriverOptions(true).Any(x=>x.Value=="SELF")&&!detail.DriverOptions(false).Any(x=>x.Value=="SELF"),"self drive is available only as first driver");
  Check(detail.ReturnQuery.Keyword==query.Keyword&&detail.ReturnQuery.Status=="todo","detail retains keyword and status including reserved characters");
  var index=(CARGO_D05IndexViewModel)((ViewResult)Controller().Index(query)).Model;
  Check(index.Keyword==query.Keyword&&index.Status==query.Status,"return list restores original criteria");
  var form=detail.Form;form.Driver2="示範駕駛 B";
  var invalid=Controller();invalid.ModelState.AddModelError("Form.StartKm","required");
  var failed=(CARGO_D05DetailViewModel)((ViewResult)invalid.Save(form,query)).Model;
  Check(failed.Form.Driver2=="示範駕駛 B"&&failed.ReturnQuery.Keyword==query.Keyword,"validation failure retains actual second driver and return criteria");
  form.Driver2=null;form.StartKm=100;form.EndKm=120;
  var saved=(RedirectToRouteResult)Controller().Save(form,query);
  Check((string)saved.RouteValues["action"]=="Detail"&&(string)saved.RouteValues["Keyword"]==query.Keyword&&(string)saved.RouteValues["Status"]=="todo","successful save uses fixed detail route and preserves criteria");
  var actual=(CARGO_D05DetailViewModel)((ViewResult)Controller().Detail(5,query)).Model;
  Check(actual.Form.Driver2==null&&actual.Form.StartKm==100&&actual.Form.EndKm==120,"single-driver usage saves and reloads without duplicate driver");
  var labels=typeof(CARGO_D06Service).GetMethod("VehicleLabel",BindingFlags.NonPublic|BindingFlags.Static);
  var resources=new List<CARGO_D_RESOURCE>{new CARGO_D_RESOURCE {RESOURCE_ID="ABC-123",NAME="廂型車",KIND="VEHICLE"},new CARGO_D_RESOURCE {RESOURCE_ID="ABC-456",NAME="廂型車",KIND="VEHICLE"},new CARGO_D_RESOURCE {RESOURCE_ID="same",NAME="same",KIND="VEHICLE"}};
  Func<string,string> label=id=>(string)labels.Invoke(null,new object[]{resources,id});
  Check(label("ABC-123")=="ABC-123（廂型車）"&&label("ABC-456")=="ABC-456（廂型車）","D06 distinguishes same-name vehicles using vehicle IDs");
  Check(label("same")=="same"&&label("missing")=="missing"&&label(null)==null,"D06 vehicle labels preserve unknown/null IDs and avoid duplicate names");
  Console.WriteLine("PASS "+count+" D05/D06 parity checks");
 }
}
