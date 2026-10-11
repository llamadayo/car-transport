using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARGO.Web.ViewModels.CARGO_D01;
using CARGO.Web.ViewModels.Shared;
namespace CARGO.Web.ViewModels.CARGO_D02 {
    public sealed class CARGO_D02RowViewModel {
        public long RequestId { get; set; }
        public int Version { get; set; }
        public string RequestNo { get; set; }
        public string Category { get; set; }
        public string Applicant { get; set; }
        public string Approver { get; set; }
        public string SelfDrive { get; set; }
        public string Cargo { get; set; }
        public bool HazardousCargo { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public int Passengers { get; set; }
        public string Status { get; set; }
        public bool CanApprove { get; set; }
    }
}
