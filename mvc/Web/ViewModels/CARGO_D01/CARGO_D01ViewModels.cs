using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace CARGO.Web.ViewModels.CARGO_D01
{
    public sealed class CARGO_D01FormViewModel
    {
        public long RequestId { get; set; }
        public int Version { get; set; }
        public string RequestNo { get; set; }
        public string Status { get; set; }
        public string ReviewNote { get; set; }
        public bool CanChooseRoutine { get; set; }
        public List<CARGO_DOptionViewModel> Options { get; set; }
        [Display(Name = "用車類別")]
        [StringLength(20)]
        public string Category { get; set; }
        [Display(Name = "運輸類別")]
        [StringLength(20)]
        public string TransportType { get; set; }
        [Display(Name = "申請人")]
        [StringLength(100)]
        [Required(ErrorMessage = "請填寫申請人")]
        public string Applicant { get; set; }
        [Display(Name = "部門")]
        [StringLength(100)]
        public string Department { get; set; }
        [Display(Name = "分機")]
        [StringLength(30)]
        public string Extension { get; set; }
        [Display(Name = "計畫代號")]
        [StringLength(100)]
        public string PlanCode { get; set; }
        [Display(Name = "車屬院區")]
        [StringLength(50)]
        public string Campus { get; set; }
        [Display(Name = "車種順位 1")]
        [StringLength(50)]
        [Required(ErrorMessage = "請填寫車種順位 1")]
        public string Preference1 { get; set; }
        [Display(Name = "車種順位 2")]
        [StringLength(50)]
        public string Preference2 { get; set; }
        [Display(Name = "車種順位 3")]
        [StringLength(50)]
        public string Preference3 { get; set; }
        [Display(Name = "演訓計畫名稱")]
        [StringLength(200)]
        public string ExercisePlan { get; set; }
        [Display(Name = "車長")]
        [StringLength(100)]
        public string Leader { get; set; }
        [Display(Name = "車長分機")]
        [StringLength(30)]
        public string LeaderExtension { get; set; }
        [Display(Name = "車長手機")]
        [StringLength(30)]
        public string LeaderPhone { get; set; }
        [Display(Name = "上車地點")]
        [StringLength(300)]
        public string PickupLocation { get; set; }
        [Display(Name = "事由")]
        [StringLength(1000)]
        public string Purpose { get; set; }
        [Display(Name = "三聯單表單編號")]
        [StringLength(100)]
        public string TriplicateNo { get; set; }
        [Display(Name = "護運單號")]
        [StringLength(100)]
        public string EscortNo { get; set; }
        [Display(Name = "異動事由")]
        [StringLength(1000)]
        public string ChangeReason { get; set; }
        [Display(Name = "備註")]
        [StringLength(1000)]
        public string Remarks { get; set; }
        [Display(Name = "用車起時")]
        [Required(ErrorMessage = "請填寫用車起時")]
        public DateTime? StartAt { get; set; }
        [Display(Name = "用車迄時")]
        [Required(ErrorMessage = "請填寫用車迄時")]
        public DateTime? EndAt { get; set; }
        [Display(Name = "乘客數")]
        [Range(1, 999999, ErrorMessage = "乘客數至少 1 人")]
        public int Passengers { get; set; }
        [Display(Name = "是否自駕")]
        [Required(ErrorMessage = "請填寫是否自駕")]
        public bool? SelfDrive { get; set; }
        [Display(Name = "需 K 證")]
        public bool NeedK { get; set; }
        [Display(Name = "需 P 證")]
        public bool NeedP { get; set; }
        [Display(Name = "是否單程運輸")]
        public bool OneWay { get; set; }
        [Display(Name = "是否同意併車")]
        public bool AllowMerge { get; set; }
        [Display(Name = "需要夜間加班")]
        public bool NightOvertime { get; set; }
        [Display(Name = "需要假日加班")]
        public bool HolidayOvertime { get; set; }
        [Display(Name = "願意持續媒合駕駛")]
        public bool ContinueMatching { get; set; }
        [Display(Name = "無駕駛時願意自駕")]
        public bool WillingSelfDrive { get; set; }
        [Display(Name = "自駕人擁有合格駕駛證照")]
        public bool QualifiedLicense { get; set; }
        [Display(Name = "進入台北市")]
        public bool EnterTaipei { get; set; }
        [Display(Name = "跨院區")]
        public bool CrossCampus { get; set; }
        [Display(Name = "有載運品")]
        public bool HasCargo { get; set; }
        [Display(Name = "危險品")]
        public bool HazardousCargo { get; set; }
        [Display(Name = "火工品")]
        public bool PyrotechnicCargo { get; set; }
        [Display(Name = "需吊掛")]
        public bool NeedsHoisting { get; set; }
        [Display(Name = "是否送審")]
        public bool RequestReview { get; set; }
        public List<CARGO_D01LocationViewModel> Locations { get; set; }
        public List<CARGO_D01SelfDriverViewModel> SelfDrivers { get; set; }
        public List<CARGO_D01CargoViewModel> Cargo { get; set; }
        public CARGO_D01FormViewModel()
        {
            Category = "general"; TransportType = "一般"; Passengers = 1; RequestReview = true;
            Options = new List<CARGO_DOptionViewModel>();
            Locations = new List<CARGO_D01LocationViewModel>();
            SelfDrivers = new List<CARGO_D01SelfDriverViewModel>();
            Cargo = new List<CARGO_D01CargoViewModel>();
        }
    }
    public sealed class CARGO_D01LocationViewModel
    {
        [Display(Name = "地點")]
        [StringLength(300)]
        public string Name { get; set; }
    }
    public sealed class CARGO_D01SelfDriverViewModel
    {
        [Display(Name = "姓名")]
        [StringLength(100)]
        public string Name { get; set; }
        [Display(Name = "分機")]
        [StringLength(30)]
        public string Extension { get; set; }
        [Display(Name = "手機")]
        [StringLength(30)]
        public string Phone { get; set; }
    }
    public sealed class CARGO_D01CargoViewModel
    {
        [Display(Name = "品名")]
        [StringLength(200)]
        public string Name { get; set; }
        [Display(Name = "長 cm")]
        public decimal Length { get; set; }
        [Display(Name = "寬 cm")]
        public decimal Width { get; set; }
        [Display(Name = "高 cm")]
        public decimal Height { get; set; }
        [Display(Name = "重量 kg")]
        public decimal Weight { get; set; }
        [Display(Name = "件數")]
        [Range(1, 5000, ErrorMessage = "每筆貨物件數需為 1 至 5000 的整數")]
        public int Quantity { get; set; }
        [Display(Name = "品類")]
        [StringLength(50)]
        public string Category { get; set; }
        [Display(Name = "外包裝")]
        [StringLength(200)]
        public string Packaging { get; set; }
    }
    public sealed class CARGO_DOptionViewModel
    {
        public string Type { get; set; }
        public string Code { get; set; }
        public string Label { get; set; }
    }
    public sealed class CARGO_D01IndexViewModel
    {
        public string Applicant { get; set; }
        public DateTime? UseDate { get; set; }
        public string Status { get; set; }
    }
    public sealed class CARGO_D01RowViewModel
    {
        public long RequestId { get; set; }
        public string RequestNo { get; set; }
        public string Category { get; set; }
        public string Applicant { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int Passengers { get; set; }
        public string SelfDrive { get; set; }
        public string Cargo { get; set; }
        public bool HazardousCargo { get; set; }
        public bool PendingReturn { get; set; }
        public string SignStatus { get; set; }
        public string Status { get; set; }
        public string Outcome { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public sealed class CARGO_D01EventViewModel
    {
        public DateTime? At { get; set; }
        public string Kind { get; set; }
        public string Actor { get; set; }
        public string Text { get; set; }
    }
    public sealed class CARGO_D01AssignmentViewModel
    {
        public string Actor { get; set; }
        public bool IsSelfDrive { get; set; }
        public bool IsEmpty { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string Vehicle { get; set; }
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        public string VehicleLabel { get; set; }
        public string Driver1Label { get; set; }
        public string Driver2Label { get; set; }
        public string Kind { get; set; }
        public string Reason { get; set; }
    }
    public sealed class CARGO_D01DetailViewModel
    {
        public CARGO_D01FormViewModel Form { get; set; }
        public string StatusLabel { get; set; }
        public string Outcome { get; set; }
        public string Approver { get; set; }
        public string Vehicle { get; set; }
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        public string VehicleLabel { get; set; }
        public string Driver1Label { get; set; }
        public string Driver2Label { get; set; }
        public string DispatchNo { get; set; }
        public string DispatchNote { get; set; }
        public string DispatchedBy { get; set; }
        public DateTime? DispatchedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PendingReturnAt { get; set; }
        public DateTime? ReturnRequestedAt { get; set; }
        public string ReturnReason { get; set; }
        public bool CanEdit { get; set; }
        public bool CanWithdraw { get; set; }
        public bool CanReturn { get; set; }
        public List<CARGO_D01EventViewModel> History { get; set; }
        public List<CARGO_D01EventViewModel> Notices { get; set; }
        public List<CARGO_D01AssignmentViewModel> Assignments { get; set; }
    }
    public sealed class CARGO_D01ActionViewModel
    {
        [Range(1, long.MaxValue)] public long RequestId { get; set; }
        [Range(1, int.MaxValue)] public int Version { get; set; }
        public DateTime? ReturnAt { get; set; }
        [StringLength(1000)] public string Reason { get; set; }
    }
}

