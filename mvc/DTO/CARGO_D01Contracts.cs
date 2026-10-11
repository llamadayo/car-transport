using System;
using System.Collections.Generic;
using CARGO.DTO.Tables;

namespace CARGO.DTO
{
    public sealed class CARGO_DActorDto
    {
        public string UserId { get; set; }
        public bool CanChooseRoutine { get; set; }
    }
    public sealed class CARGO_D01SaveDto
    {
        public CARGO_D_REQUEST Request { get; set; }
        public List<CARGO_D_LOCATION> Locations { get; set; }
        public List<CARGO_D_SELF_DRIVER> SelfDrivers { get; set; }
        public List<CARGO_D_CARGO> Cargo { get; set; }
        public CARGO_D01SaveDto()
        {
            Request = new CARGO_D_REQUEST();
            Locations = new List<CARGO_D_LOCATION>();
            SelfDrivers = new List<CARGO_D_SELF_DRIVER>();
            Cargo = new List<CARGO_D_CARGO>();
        }
    }
    public sealed class CARGO_D01QueryDto
    {
        public string Applicant { get; set; }
        public DateTime? UseDate { get; set; }
        public string Status { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string SortField { get; set; }
        public bool Descending { get; set; }
    }
    public sealed class CARGO_D01ListItemDto
    {
        public long REQUEST_ID { get; set; }
        public string REQUEST_NO { get; set; }
        public string CATEGORY { get; set; }
        public string APPLICANT { get; set; }
        public DateTime START_AT { get; set; }
        public DateTime END_AT { get; set; }
        public int PAX { get; set; }
        public int SELF_DRIVE { get; set; }
        public string DISPLAY_STATUS { get; set; }
        public string SIGN_STATUS { get; set; }
        public DateTime? PENDING_RETURN_AT { get; set; }
        public int HAZARDOUS_CARGO { get; set; }
        public string OUTCOME { get; set; }
        public DateTime CREATED_AT { get; set; }
        public int CARGO_COUNT { get; set; }
        public int CARGO_QUANTITY { get; set; }
    }
    public sealed class CARGO_D01PageDto
    {
        public List<CARGO_D01ListItemDto> Items { get; set; }
        public int Total { get; set; }
    }
    public sealed class CARGO_D01DetailDto
    {
        public string ReviewApprover { get; set; }
        public CARGO_D01SaveDto Application { get; set; }
        public List<CARGO_D_LOG> History { get; set; }
        public List<CARGO_D_ASSIGNMENT> Assignments { get; set; }
        public List<CARGO_D_NOTICE> Notices { get; set; }
    }
    public sealed class CARGO_D01ActionDto
    {
        public long RequestId { get; set; }
        public int Version { get; set; }
        public DateTime? ReturnAt { get; set; }
        public string Reason { get; set; }
    }
}

