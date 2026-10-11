using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARGO.Web.ViewModels.CARGO_D01;
using CARGO.Web.ViewModels.Shared;
namespace CARGO.Web.ViewModels.CARGO_D03 {
    public sealed class CARGO_D03RowViewModel {
        public long RequestId { get; set; }
        public int Version { get; set; }
        public string RequestNo { get; set; }
        public string Category { get; set; }
        public string Applicant { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public int Passengers { get; set; }
        public string Status { get; set; }
        public string Outcome { get; set; }
        public string SignStatus { get; set; }
        public string Vehicle { get; set; }
        public string Driver { get; set; }
        public bool SelfDrive { get; set; }
        public string Requirements { get; set; }
        public bool HazardousCargo { get; set; }
        public bool HasPendingReturn { get; set; }
        public string ActionLabel { get; set; }
    }
}
