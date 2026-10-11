using System;

namespace CARGO.DTO.Tables
{
    public sealed class CARGO_D_CARGO
    {
        public long REQUEST_ID { get; set; }
        public int LINE_NO { get; set; }
        public string NAME { get; set; }
        public decimal LENGTH_CM { get; set; }
        public decimal WIDTH_CM { get; set; }
        public decimal HEIGHT_CM { get; set; }
        public decimal WEIGHT_KG { get; set; }
        public int QUANTITY { get; set; }
        public string CATEGORY { get; set; }
        public string PACKAGING { get; set; }
        // Legacy storage only; D01-D06 use CARGO_D_REQUEST.HAZARDOUS_CARGO exclusively.
        public int HAZARDOUS { get; set; }
    }
}

