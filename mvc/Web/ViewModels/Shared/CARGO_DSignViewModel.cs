using System;
namespace CARGO.Web.ViewModels.Shared {
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
}
