using System;

namespace CARGO.DTO.Tables
{
    public sealed class CARGO_D_ASSIGNMENT
    {
        public long REQUEST_ID { get; set; }
        public int LINE_NO { get; set; }
        public DateTime FROM_AT { get; set; }
        public DateTime TO_AT { get; set; }
        public string VEHICLE { get; set; }
        public string DRIVER1 { get; set; }
        public string DRIVER2 { get; set; }
        public string KIND { get; set; }
        public string REASON { get; set; }
        public string ACTOR { get; set; }
    }
}

