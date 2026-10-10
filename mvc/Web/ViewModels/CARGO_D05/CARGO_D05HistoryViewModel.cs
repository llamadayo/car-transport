using System;
namespace CARGO.Web.ViewModels.CARGO_D05 {
    public sealed class CARGO_D05HistoryViewModel {
        public int Revision { get; set; }
        public string VehicleType { get; set; }
        public string Vehicle { get; set; }
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        public decimal StartKm { get; set; }
        public decimal EndKm { get; set; }
        public decimal Distance { get; set; }
        public string Actor { get; set; }
        public DateTime At { get; set; }
    }
}
