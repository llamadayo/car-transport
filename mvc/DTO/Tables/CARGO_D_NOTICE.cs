using System;

namespace CARGO.DTO.Tables
{
    public sealed class CARGO_D_NOTICE
    {
        public long REQUEST_ID { get; set; }
        public int LINE_NO { get; set; }
        public DateTime? SENT_AT { get; set; }
        public string RECIPIENT { get; set; }
        public string KIND { get; set; }
        public string MESSAGE { get; set; }
    }
}

