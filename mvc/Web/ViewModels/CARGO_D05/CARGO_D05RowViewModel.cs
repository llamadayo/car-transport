using System;
namespace CARGO.Web.ViewModels.CARGO_D05 {
    public sealed class CARGO_D05RowViewModel {
        public long RequestId { get; set; }
        public string DispatchNo { get; set; }
        public DateTime? UseDate { get; set; }
        public string VehicleType { get; set; }
        public string Vehicle { get; set; }
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        public string FlowStatus { get; set; }
        public string UsageStatus { get; set; }
        public decimal? Distance { get; set; }
    }
}
