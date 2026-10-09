using System;

namespace CARGO.DTO.Tables
{
    public sealed class CARGO_D_LOG
    {
        public long LOG_ID { get; set; }
        public long REQUEST_ID { get; set; }
        public string ACTION { get; set; }
        public string ACTOR { get; set; }
        public string NOTE { get; set; }
        public DateTime CREATED_AT { get; set; }
    }
}

