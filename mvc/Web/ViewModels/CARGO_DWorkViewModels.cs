using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARGO.Web.ViewModels.CARGO_D01;
namespace CARGO.Web.ViewModels {
    public sealed class CARGO_DWorkQueryViewModel {
        public string Applicant { get; set; }
        public string Status { get; set; }
        public DateTime? UseDate { get; set; }
    }
    public sealed class CARGO_DWorkActionViewModel {
        public long RequestId { get; set; }
        public int Version { get; set; }
        public string Command { get; set; }
        [StringLength(1000)] public string Note { get; set; }
        public string Vehicle { get; set; }
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        public DateTime? EffectiveAt { get; set; }
        public DateTime? EndAt { get; set; }
    }
    public sealed class CARGO_DWorkRowViewModel {
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
        public bool CanApprove { get; set; }
    }
    public sealed class CARGO_DResourceViewModel {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Kind { get; set; }
        public string Reason { get; set; }
    }
    public sealed class CARGO_DSignViewModel {
        public int Round { get; set; }
        public string Status { get; set; }
        public string SubmittedBy { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Summary { get; set; }
        public string DecidedBy { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string Note { get; set; }
    }
    public sealed class CARGO_DWorkDetailViewModel {
        public CARGO_D01DetailViewModel Application { get; set; }
        public List<CARGO_DResourceViewModel> Resources { get; set; }
        public List<CARGO_DSignViewModel> Signs { get; set; }
        public List<string> PriorityRequests { get; set; }
        public bool CanApprove { get; set; }
        public bool CanDispatch { get; set; }
        public bool CanChange { get; set; }
        public bool CanSign { get; set; }
    }
}
