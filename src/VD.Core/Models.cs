using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace VD.Core
{
    public interface IClock { DateTime Now { get; } }
    public sealed class TaipeiClock : IClock
    {
        public DateTime Now => DateTime.UtcNow.AddHours(8); // Taiwan has no DST; independent of IIS local timezone.
    }
    public static class BusinessTime
    {
        public static string Date(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static DateTime ParseDate(string value)
        {
            DateTime date;
            if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                throw new ArgumentException("日期格式須為 yyyy-MM-dd。");
            return date;
        }
        public static int Minute(string value)
        {
            DateTime t;
            if (!DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out t))
                throw new ArgumentException("時間格式須為 HH:mm。");
            return t.Hour * 60 + t.Minute;
        }
        public static string Display(int? value) => value.HasValue ? (value.Value / 60).ToString("00") + ":" + (value.Value % 60).ToString("00") : "";
    }
    public class Site { public string Id { get; set; } public string Name { get; set; } public int SortOrder { get; set; } }
    public class Station : Site { public string BranchId { get; set; } }
    public class Building { public string Id { get; set; } public string SiteId { get; set; } public string StationId { get; set; } public string Name { get; set; } }
    public class Vehicle : Site
    {
        public string Pool { get; set; } public string SizeClass { get; set; }
        public string HomeSite { get; set; } public string CurrentSite { get; set; }
        public double LengthCm { get; set; } public double WidthCm { get; set; } public double HeightCm { get; set; }
        public double Volume { get; set; } public double WeightLimit { get; set; } public int Seats { get; set; }
    }
    public class Driver : Site { public string Pool { get; set; } public string HomeSite { get; set; } public string CurrentSite { get; set; } }
    public class CargoCategory { public string Code { get; set; } public string Name { get; set; } public double Factor { get; set; } public int Active { get; set; } public int SortOrder { get; set; } }
    public class Shift
    {
        public string Id { get; set; } public string BranchId { get; set; } public string Label { get; set; }
        public int DepartMinute { get; set; } public int SortOrder { get; set; }
        public string VehicleId { get; set; } public string DriverId { get; set; }
        public string Depart => BusinessTime.Display(DepartMinute);
    }
    public class Trip
    {
        public string ServiceDate { get; set; } public string ShiftId { get; set; }
        public string VehicleId { get; set; } public string DriverId { get; set; }
    }
    public class CargoItem
    {
        public long ApplicationId { get; set; } public int LineNo { get; set; }
        [Required, StringLength(200)] public string Name { get; set; }
        [Range(0.001, 1000000)] public double LengthCm { get; set; }
        [Range(0.001, 1000000)] public double WidthCm { get; set; }
        [Range(0.001, 1000000)] public double HeightCm { get; set; }
        [Required, StringLength(30)] public string Category { get; set; }
        [Range(1, 1000000)] public int Qty { get; set; }
        [Range(0, 1000000000)] public double Weight { get; set; }
    }
    public class ApplicationInput
    {
        [Required, StringLength(200)] public string Applicant { get; set; }
        [Required, StringLength(30)] public string BranchId { get; set; }
        [Required, StringLength(30)] public string PickStationId { get; set; }
        [Required, StringLength(30)] public string DropStationId { get; set; }
        [StringLength(200)] public string PickBuilding { get; set; }
        [StringLength(200)] public string DropBuilding { get; set; }
        [Required] public string RecvMode { get; set; }
        public string ServiceDate { get; set; } public string ExpectedTime { get; set; }
        [Range(0, 100000)] public int LoadMin { get; set; }
        [Range(0, 100000)] public int UnloadMin { get; set; }
        [StringLength(200)] public string RecipientUnit { get; set; }
        [StringLength(100)] public string RecipientName { get; set; }
        [StringLength(100)] public string RecipientPhone { get; set; }
        [StringLength(100)] public string AgentName { get; set; }
        [StringLength(100)] public string AgentPhone { get; set; }
        public List<CargoItem> Items { get; set; } = new List<CargoItem>();
    }
    public class RegionalApplication : ApplicationInput
    {
        public long Id { get; set; } public string AppNo { get; set; } public long SubmitSeq { get; set; }
        public string Status { get; set; } public string ShiftId { get; set; }
        public int? ArrivalMinute { get; set; } public int? ExpectedMinute { get; set; } public int? ExpectDiffMin { get; set; }
        public string Note { get; set; } public string FailureReason { get; set; } public string Incident { get; set; }
        public string CreatedAt { get; set; } public string DeliveredAt { get; set; } public string DeliveredBy { get; set; }
        public int MatchAttempt { get; set; }
        public int HandleMin => LoadMin + UnloadMin;
        public string Arrival => BusinessTime.Display(ArrivalMinute);
        public List<TraceStep> Trace { get; set; } = new List<TraceStep>();
    }
    public class TraceStep { public string Code { get; set; } public string Message { get; set; } }
    public class LoadValue { public double Volume { get; set; } public double Weight { get; set; } public double Floor { get; set; } }
    public class LoadResult
    {
        public bool Ok => Reasons.Count == 0;
        public List<TraceStep> Reasons { get; set; } = new List<TraceStep>();
        public List<TraceStep> Trace { get; set; } = new List<TraceStep>();
        public LoadValue Used { get; set; }
    }
    public class MatchResult
    {
        public bool Ok { get; set; } public string Reason { get; set; } public string Message { get; set; }
        public string ShiftId { get; set; } public string VehicleId { get; set; }
        public int? ArrivalMinute { get; set; } public int? ExpectDiffMin { get; set; }
        public List<TraceStep> Trace { get; set; } = new List<TraceStep>();
    }
    public class MasterSnapshot
    {
        public List<Site> Sites { get; set; } = new List<Site>();
        public List<Station> Stations { get; set; } = new List<Station>();
        public List<Building> Buildings { get; set; } = new List<Building>();
        public List<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public List<Driver> Drivers { get; set; } = new List<Driver>();
        public List<Shift> Shifts { get; set; } = new List<Shift>();
        public List<CargoCategory> Categories { get; set; } = new List<CargoCategory>();
        public Dictionary<string, double> Config { get; set; } = new Dictionary<string, double>();
        public double DefaultFactor => Config["WasteDefault"];
        public int InterStationMin => (int)Config["InterStationMin"];
        public int HandleBudget => (int)Config["ShiftHandleBudget"];
    }
    public class ApplicationQuery
    {
        public string Applicant { get; set; } public string BranchId { get; set; }
        public string StationId { get; set; } public string Status { get; set; } public string Mode { get; set; }
    }
    public class TripQuery
    {
        public string ServiceDate { get; set; } public string ShiftId { get; set; }
        public string VehicleId { get; set; } public string DriverId { get; set; }
    }
}
