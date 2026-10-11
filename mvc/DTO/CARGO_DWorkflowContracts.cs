using System;
using System.Collections.Generic;
using CARGO.DTO.Tables;
namespace CARGO.DTO {
    public sealed class CARGO_DStaffDto {
        public string UserId { get; set; }
        public bool CanApprove { get; set; }
        public bool CanDispatch { get; set; }
        public bool CanSign { get; set; }
    }
    public sealed class CARGO_DWorkQueryDto {
        public string Keyword { get; set; }
        public string Applicant { get; set; }
        public string Status { get; set; }
        public DateTime? UseDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
    public sealed class CARGO_DWorkPageDto {
        public List<CARGO_D_REQUEST> Items { get; set; }
        public int Total { get; set; }
        public Dictionary<long, CARGO_D02ListInfoDto> ReviewInfo { get; set; }
        public Dictionary<long, CARGO_D_SIGN> SignInfo { get; set; }
    }
    public sealed class CARGO_D02ListInfoDto {
        public long REQUEST_ID { get; set; }
        public string Approver { get; set; }
        public int CargoCount { get; set; }
        public long CargoQuantity { get; set; }
    }
    public sealed class CARGO_DWorkActionDto {
        public long RequestId { get; set; }
        public int Version { get; set; }
        public string Command { get; set; }
        public bool SupplementDriver { get; set; }
        public string Note { get; set; }
        public string Vehicle { get; set; }
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        public DateTime? EffectiveAt { get; set; }
        public DateTime? EndAt { get; set; }
    }
    public sealed class CARGO_DCandidateDto {
        public CARGO_D_RESOURCE Resource { get; set; }
        public string UnavailableReason { get; set; }
    }
    public sealed class CARGO_DWorkDetailDto {
        public CARGO_D01DetailDto Detail { get; set; }
        public List<CARGO_D_SIGN> Signs { get; set; }
        public List<CARGO_DCandidateDto> Candidates { get; set; }
        public DateTime? EffectiveAt { get; set; }
        public List<string> PriorityRequests { get; set; }
    }
}
