using System;
namespace CARGO.DTO.Tables { public sealed class CARGO_D_RESOURCE_BLOCK {
    public long BLOCK_ID { get; set; }
    public string RESOURCE_ID { get; set; }
    public DateTime FROM_AT { get; set; }
    public DateTime TO_AT { get; set; }
    public string SOURCE { get; set; }
    public string REASON { get; set; }
} }
