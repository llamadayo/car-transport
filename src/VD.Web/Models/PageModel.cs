using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using VD.Core;
namespace VD.Web.Models {
 public sealed class PageModel {
  public MasterSnapshot Master { get; set; }
  public DateTime Today { get; set; }
  public string Title { get; set; }
  public IEnumerable<SelectListItem> Sites => Master.Sites.Select(x=>new SelectListItem {Value=x.Id,Text=x.Name});
  public IEnumerable<SelectListItem> Stations => Master.Stations.Select(x=>new SelectListItem {Value=x.Id,Text=Master.Sites.Single(b=>b.Id==x.BranchId).Name+"·"+x.Name});
  public IEnumerable<SelectListItem> Vehicles => Master.Vehicles.Where(x=>x.Pool=="LOGI" && string.IsNullOrEmpty(x.SizeClass)).Select(x=>new SelectListItem {Value=x.Id,Text=x.Id+"（"+x.Name+"）"});
  public IEnumerable<SelectListItem> Drivers => Master.Drivers.Where(x=>x.Pool=="LOGI").Select(x=>new SelectListItem {Value=x.Id,Text=x.Name});
  public IEnumerable<SelectListItem> Shifts => Master.Shifts.Select(x=>new SelectListItem {Value=x.Id,Text=Master.Sites.Single(b=>b.Id==x.BranchId).Name+"·"+x.Label});
  public IEnumerable<SelectListItem> Categories => Master.Categories.Where(x=>x.Active==1).Select(x=>new SelectListItem {Value=x.Code,Text=x.Name});
  public IEnumerable<SelectListItem> Modes => Options("asap","越快越好","exact","指定期望時間");
  public IEnumerable<SelectListItem> Statuses => Options("matched","已排班","unscheduled","未排入·請改期","delivered","已交貨");
  public static IEnumerable<SelectListItem> Options(params string[] pairs) { for(int i=0;i<pairs.Length;i+=2) yield return new SelectListItem {Value=pairs[i],Text=pairs[i+1]}; }
 }
}
