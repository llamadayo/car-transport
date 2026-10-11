using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARGO.Web.ViewModels.CARGO_D01;
using CARGO.Web.ViewModels.Shared;
namespace CARGO.Web.ViewModels.CARGO_D04 {
    public sealed class CARGO_D04RowViewModel {
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
        public int Round { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string SubmittedBy { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string DecidedBy { get; set; }
        public string SignSummary { get; set; }
        public bool CanApprove { get; set; }
    }
}
