using System;
namespace CARGO.DTO.Tables { public sealed class CARGO_D_SIGN {
    public long REQUEST_ID { get; set; }
    public int ROUND_NO { get; set; }
    public string STATUS { get; set; }
    public string SUBMITTED_BY { get; set; }
    public DateTime SUBMITTED_AT { get; set; }
    public string SUMMARY { get; set; }
    public string DECIDED_BY { get; set; }
    public DateTime? DECIDED_AT { get; set; }
    public string NOTE { get; set; }
} }
