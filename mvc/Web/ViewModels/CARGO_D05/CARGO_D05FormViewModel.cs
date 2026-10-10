using System.ComponentModel.DataAnnotations;
namespace CARGO.Web.ViewModels.CARGO_D05 {
    public sealed class CARGO_D05FormViewModel {
        public long RequestId { get; set; }
        public int Version { get; set; }
        [Required] public string VehicleType { get; set; }
        [Required] public string Vehicle { get; set; }
        [Required] public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        [Required] public decimal? StartKm { get; set; }
        [Required] public decimal? EndKm { get; set; }
    }
}
