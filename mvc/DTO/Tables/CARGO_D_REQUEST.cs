using System;

namespace CARGO.DTO.Tables
{
    public sealed class CARGO_D_REQUEST
    {
        public long REQUEST_ID { get; set; }
        public string REQUEST_NO { get; set; }
        public string OWNER_ID { get; set; }
        public string STATUS { get; set; }
        public int VERSION_NO { get; set; }
        public DateTime CREATED_AT { get; set; }
        public DateTime UPDATED_AT { get; set; }
        public string REVIEW_NOTE { get; set; }
        public string APPROVER_NAME { get; set; }
        public DateTime? DISPATCHED_AT { get; set; }
        public string DISPATCHED_BY { get; set; }
        public string DISPATCH_NOTE { get; set; }
        public string OUTCOME { get; set; }
        public string SIGN_STATUS { get; set; }
        public string DISPATCH_NO { get; set; }
        public string VEHICLE { get; set; }
        public string DRIVER1 { get; set; }
        public string DRIVER2 { get; set; }
        public DateTime? USAGE_AT { get; set; }
        public DateTime? PENDING_RETURN_AT { get; set; }
        public DateTime? RETURN_REQUESTED_AT { get; set; }
        public string RETURN_REASON { get; set; }
        public string CATEGORY { get; set; }
        public string TRANSPORT_TYPE { get; set; }
        public string APPLICANT { get; set; }
        public string DEPT { get; set; }
        public string EXT { get; set; }
        public string PLAN_CODE { get; set; }
        public string CAMPUS { get; set; }
        public string PREFERENCE1 { get; set; }
        public string PREFERENCE2 { get; set; }
        public string PREFERENCE3 { get; set; }
        public string EXERCISE_PLAN { get; set; }
        public string LEADER { get; set; }
        public string LEADER_EXT { get; set; }
        public string LEADER_PHONE { get; set; }
        public string PICKUP_LOCATION { get; set; }
        public string PURPOSE { get; set; }
        public string TRIPLICATE_NO { get; set; }
        public string ESCORT_NO { get; set; }
        public string CHANGE_REASON { get; set; }
        public string REMARKS { get; set; }
        public DateTime? START_AT { get; set; }
        public DateTime? END_AT { get; set; }
        public int PAX { get; set; }
        public int? SELF_DRIVE { get; set; }
        public int NEED_K { get; set; }
        public int NEED_P { get; set; }
        public int ONE_WAY { get; set; }
        public int ALLOW_MERGE { get; set; }
        public int NIGHT_OT { get; set; }
        public int HOLIDAY_OT { get; set; }
        public int CONTINUE_MATCHING { get; set; }
        public int WILLING_SELF_DRIVE { get; set; }
        public int QUALIFIED_LICENSE { get; set; }
        public int ENTER_TAIPEI { get; set; }
        public int CROSS_CAMPUS { get; set; }
        public int HAS_CARGO { get; set; }
        public int HAZARDOUS_CARGO { get; set; }
        public int PYROTECHNIC_CARGO { get; set; }
        public int NEEDS_HOISTING { get; set; }
        public int REQUEST_REVIEW { get; set; }
    }
}

