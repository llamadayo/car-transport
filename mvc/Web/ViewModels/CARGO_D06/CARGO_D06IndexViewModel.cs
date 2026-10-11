using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace CARGO.Web.ViewModels.CARGO_D06 {
 public sealed class CARGO_D06IndexViewModel : IValidatableObject {
  [Display(Name="日期起")] public DateTime? FromDate {get;set;}
  [Display(Name="日期迄")] public DateTime? ToDate {get;set;}
  [Display(Name="司機")] public string DriverId {get;set;}
  public List<SelectListItem> Drivers {get;set;}
  public List<CARGO_D06TaskViewModel> Tasks {get;set;}
  public CARGO_D06IndexViewModel() {
   FromDate=DateTime.Today; ToDate=DateTime.Today.AddDays(14);
   Drivers=new List<SelectListItem>(); Tasks=new List<CARGO_D06TaskViewModel>();
  }
  public IEnumerable<ValidationResult> Validate(ValidationContext context) {
   if(FromDate.HasValue && ToDate.HasValue && FromDate.Value.Date>ToDate.Value.Date)
    yield return new ValidationResult("日期迄不可早於日期起",new[]{"ToDate"});
  }
 }
}
