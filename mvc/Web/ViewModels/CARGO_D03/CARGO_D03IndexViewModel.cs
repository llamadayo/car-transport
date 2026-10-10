using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARGO.Web.ViewModels.CARGO_D01;
using CARGO.Web.ViewModels.Shared;
namespace CARGO.Web.ViewModels.CARGO_D03 {
    public sealed class CARGO_D03IndexViewModel {
        public string Applicant { get; set; }
        public string Status { get; set; }
        public DateTime? UseDate { get; set; }
    }
}
