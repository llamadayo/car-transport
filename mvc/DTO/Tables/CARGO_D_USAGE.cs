using System;
namespace CARGO.DTO.Tables {
    public sealed class CARGO_D_USAGE {
        public long REQUEST_ID { get; set; }
        public int REVISION_NO { get; set; }
        public string VEHICLE_TYPE { get; set; }
        public string VEHICLE { get; set; }
        public string DRIVER1 { get; set; }
        public string DRIVER2 { get; set; }
        public decimal START_KM { get; set; }
        public decimal END_KM { get; set; }
        public decimal DISTANCE_KM { get; set; }
        public string RECORDED_BY { get; set; }
        public DateTime RECORDED_AT { get; set; }
    }
}
