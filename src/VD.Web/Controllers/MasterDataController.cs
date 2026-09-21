using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using VD.Core;
using VD.Services;
namespace VD.Web.Controllers {
 public sealed class MasterDataController : BaseController {
  [HttpGet] public ActionResult Index() => View(Page("共用基礎 · 主檔資料"));
  [HttpGet] public ActionResult Engine() => View(Page("共用基礎 · 裝載可行性引擎"));
  [HttpGet] public JsonResult Read() => Result(new MasterDataService(Settings).Read());
  [HttpGet] public JsonResult ReadShared() => Result(new MasterDataService(Settings).ReadSharedTables());
  [HttpPost,ValidateAntiForgeryToken] public JsonResult RefreshFactors() {new MasterDataService(Settings).RefreshFactors();return Result("貨物係數已重新載入。");}
  [HttpPost,ValidateAntiForgeryToken] public JsonResult CheckLoad(string vehicleId,List<CargoItem> items) {
   ValidInput();if(items==null||items.Count==0) throw new ArgumentException("請至少新增一項貨物。");
   var m=new MasterDataService(Settings).Read();var v=m.Vehicles.SingleOrDefault(x=>x.Id==vehicleId);
   if(v==null) throw new ArgumentException("找不到車輛。");return Result(new LoadFeasibilityService(m).Check(items,v));
  }
 }
}
