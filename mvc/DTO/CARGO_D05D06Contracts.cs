using System;
using System.Collections.Generic;
using CARGO.DTO.Tables;
namespace CARGO.DTO {
 public sealed class CARGO_DUsageActorDto { public string UserId {get;set;} public bool IsBusiness {get;set;} public bool IsDriver {get;set;} }
 public sealed class CARGO_D05QueryDto { public string Keyword {get;set;} public string Status {get;set;} public int Page {get;set;} public int PageSize {get;set;} }
 public sealed class CARGO_D05SaveDto {
  public long RequestId {get;set;} public int Version {get;set;} public string VehicleType {get;set;} public string Vehicle {get;set;}
  public string Driver1 {get;set;} public string Driver2 {get;set;} public decimal? StartKm {get;set;} public decimal? EndKm {get;set;}
 }
 public sealed class CARGO_D05RowDto { public CARGO_D_REQUEST Request {get;set;} public CARGO_D_USAGE Usage {get;set;} public string PlannedVehicleType {get;set;} }
 public sealed class CARGO_D05PageDto { public List<CARGO_D05RowDto> Items {get;set;} public int Total {get;set;} public int Pending {get;set;} }
 public sealed class CARGO_D05DetailDto {
  public CARGO_D01DetailDto Application {get;set;} public List<CARGO_D_USAGE> History {get;set;} public List<CARGO_D_RESOURCE> Resources {get;set;}
 }
 public sealed class CARGO_D06QueryDto {
  public DateTime? FromDate {get;set;} public DateTime? ToDate {get;set;} public string DriverId {get;set;}
 }
 public sealed class CARGO_D06DriverDto { public string Id {get;set;} public string Name {get;set;} }
 public sealed class CARGO_D06PageDto {
  public List<CARGO_D06TaskDto> Items {get;set;} public List<CARGO_D06DriverDto> Drivers {get;set;}
 }
 public sealed class CARGO_D06TaskDto {
  public CARGO_D_REQUEST Request {get;set;} public CARGO_D_ASSIGNMENT Assignment {get;set;} public string DriverId {get;set;} public string DriverName {get;set;}
  public string Partner {get;set;} public string VehicleName {get;set;} public List<CARGO_D_LOCATION> Locations {get;set;} public List<CARGO_D_CARGO> Cargo {get;set;}
 }
}
