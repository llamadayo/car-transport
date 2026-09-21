using System.Collections.Generic;
namespace VD.Core
{
    public class ApplicationRow
    {
        public long Id { get; set; } public string AppNo { get; set; } public string Applicant { get; set; }
        public string Destination { get; set; } public string Pickup { get; set; } public string BranchName { get; set; }
        public string ServiceDate { get; set; } public string ModeLabel { get; set; } public string ShiftLabel { get; set; }
        public string VehicleId { get; set; } public string VehicleName { get; set; } public string DriverName { get; set; }
        public string Status { get; set; } public string StatusLabel { get; set; } public string Arrival { get; set; } public string CreatedAt { get; set; }
        public string CargoSummary { get; set; } public RegionalApplication Application { get; set; }
    }
    public class TripRow : Trip
    {
        public string BranchName { get; set; } public string ShiftLabel { get; set; }
        public string VehicleName { get; set; } public string DriverName { get; set; } public int Count { get; set; }
        public List<ApplicationRow> Applications { get; set; } = new List<ApplicationRow>();
    }
    public class DriverTask : TripRow
    {
        public double RawVolume { get; set; }
        public List<TaskStop> Stops { get; set; } = new List<TaskStop>();
    }
    public class TaskStop
    {
        public int SortOrder { get; set; } public string Name { get; set; } public string Arrival { get; set; }
        public List<ApplicationRow> Picks { get; set; } = new List<ApplicationRow>();
        public List<ApplicationRow> Drops { get; set; } = new List<ApplicationRow>();
    }
}
