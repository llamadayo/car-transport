using System;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CARGO.Web.Controllers;
using CARGO.Web.ViewModels.CARGO_D01;
class PreviewRuntimeTests {
 class Context : HttpContextBase {
  public override IPrincipal User { get { return new GenericPrincipal(new GenericIdentity("示範申請人"),new[]{"部長秘書","單位主管","業務","運輸主管"}); } set {} }
 }
 static T Controller<T>() where T:Controller,new() {
  var c=new T(); c.ControllerContext=new ControllerContext(new Context(),new RouteData(),c);return c;
 }
 static void Check(ActionResult result,string name) {
  var v=result as ViewResult;if(v==null || v.Model==null)throw new Exception(name+" did not return a populated view");
  Console.WriteLine("PASS "+name);
 }
 static void Main() {
  var d=Controller<CARGO_D01Controller>();
  Check(d.Detail(7),"D01 Detail/7 (reported failure)");
  Check(d.Create(),"D01 Create");Check(d.Edit(1),"D01 Edit/1");
  d.ModelState.AddModelError("Applicant","required");
  var create=d.Save(new CARGO_D01FormViewModel(),"draft") as ViewResult;
  if(create==null||create.ViewName!="Create")throw new Exception("Invalid create must return Create");
  var edit=d.Save(new CARGO_D01FormViewModel{RequestId=1},"draft") as ViewResult;
  if(edit==null||edit.ViewName!="Edit")throw new Exception("Invalid edit must return Edit");
  Console.WriteLine("PASS invalid create/edit return matching views");
  Check(Controller<CARGO_D02Controller>().Detail(2),"D02 Detail/2");
  Check(Controller<CARGO_D03Controller>().Detail(4),"D03 Detail/4");
  Check(Controller<CARGO_D04Controller>().Detail(7),"D04 Detail/7");
  Check(Controller<CARGO_D05Controller>().Detail(5),"D05 Detail/5");
  Check(Controller<CARGO_D06Controller>().Index(),"D06 Index");
 }
}
