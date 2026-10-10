using System;
using System.Collections.Generic;
using CARGO.Web.ViewModels.CARGO_D01;
namespace CARGO.Web.ViewModels.CARGO_D06 {
    public sealed class CARGO_D06TaskViewModel {
        public string DriverId { get; set; }
        public string DriverName { get; set; }
        public string Partner { get; set; }
        public string Vehicle { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string Kind { get; set; }
        public string RequestNo { get; set; }
        public string Category { get; set; }
        public string Applicant { get; set; }
        public string Extension { get; set; }
        public int Passengers { get; set; }
        public string Campus { get; set; }
        public string Pickup { get; set; }
        public string Purpose { get; set; }
        public string PlanCode { get; set; }
        public string Contact { get; set; }
        public string Status { get; set; }
        public bool Waiting { get; set; }
        public List<string> Locations { get; set; }
        public List<CARGO_D01CargoViewModel> Cargo { get; set; }
    }
}
