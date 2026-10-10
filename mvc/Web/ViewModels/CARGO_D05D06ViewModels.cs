using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CARGO.Web.ViewModels.CARGO_D01;
namespace CARGO.Web.ViewModels {
 public sealed class CARGO_D05IndexViewModel {public string Keyword {get;set;} public string Status {get;set;} }
 public sealed class CARGO_D05FormViewModel {
  public long RequestId {get;set;} public int Version {get;set;}
  [Required] public string VehicleType {get;set;} [Required] public string Vehicle {get;set;} [Required] public string Driver1 {get;set;} public string Driver2 {get;set;}
  [Required] public decimal? StartKm {get;set;} [Required] public decimal? EndKm {get;set;}
 }
 public sealed class CARGO_D05RowViewModel {
  public long RequestId {get;set;} public string DispatchNo {get;set;} public DateTime? UseDate {get;set;} public string VehicleType {get;set;} public string Vehicle {get;set;}
  public string Driver1 {get;set;} public string Driver2 {get;set;} public string FlowStatus {get;set;} public string UsageStatus {get;set;} public decimal? Distance {get;set;}
 }
 public sealed class CARGO_D05HistoryViewModel {
  public int Revision {get;set;} public string VehicleType {get;set;} public string Vehicle {get;set;} public string Driver1 {get;set;} public string Driver2 {get;set;}
  public decimal StartKm {get;set;} public decimal EndKm {get;set;} public decimal Distance {get;set;} public string Actor {get;set;} public DateTime At {get;set;}
 }
 public sealed class CARGO_D05ResourceViewModel {public string Id {get;set;} public string Name {get;set;} public string Kind {get;set;} public string Type {get;set;} }
 public sealed class CARGO_D05DetailViewModel {
  public CARGO_D05FormViewModel Form {get;set;} public CARGO_D01DetailViewModel Application {get;set;} public List<CARGO_D05HistoryViewModel> History {get;set;}
  public List<CARGO_D05ResourceViewModel> Resources {get;set;} public string Difference {get;set;}
 }
 public sealed class CARGO_D06TaskViewModel {
  public string DriverId {get;set;} public string DriverName {get;set;} public string Partner {get;set;} public string Vehicle {get;set;}
  public DateTime From {get;set;} public DateTime To {get;set;} public string Kind {get;set;} public string RequestNo {get;set;} public string Category {get;set;}
  public string Applicant {get;set;} public string Extension {get;set;} public int Passengers {get;set;} public string Campus {get;set;} public string Pickup {get;set;}
  public string Purpose {get;set;} public string PlanCode {get;set;} public string Contact {get;set;} public string Status {get;set;} public bool Waiting {get;set;}
  public List<string> Locations {get;set;} public List<CARGO_D01CargoViewModel> Cargo {get;set;}
 }
}
