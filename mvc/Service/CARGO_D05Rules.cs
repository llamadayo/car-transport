using System;
using System.Collections.Generic;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
namespace CARGO.Service {
 internal static class CARGO_D05Rules {
  internal static void Demand(CARGO_DUsageActorDto actor, bool businessOnly) {
   if (actor == null || String.IsNullOrWhiteSpace(actor.UserId) || actor.UserId.Length > 200 || (!actor.IsBusiness && (businessOnly || !actor.IsDriver))) throw new UnauthorizedAccessException("沒有此功能的操作權限");
  }
  internal static bool Eligible(CARGO_D_REQUEST r, bool hasUsage) { return hasUsage || (r.STATUS == "dispatched" && r.SIGN_STATUS == "approved"); }
  internal static CARGO_D_USAGE Validate(CARGO_D_REQUEST r, List<CARGO_D_USAGE> history, List<CARGO_D_RESOURCE> resources, CARGO_D05SaveDto data, CARGO_DUsageActorDto actor) {
   Demand(actor,true);
   if (!Eligible(r,history.Count > 0) || String.IsNullOrEmpty(r.DISPATCH_NO)) throw new InvalidOperationException("派車結果尚未生效，不可實登");
   if (r.VERSION_NO != data.Version) throw new InvalidOperationException("資料已更新，請重新開啟後操作");
   var vehicle=resources.FirstOrDefault(x=>x.RESOURCE_ID==data.Vehicle && x.KIND=="VEHICLE" && x.POOL=="BIZ");
   if (vehicle==null || String.IsNullOrEmpty(data.VehicleType) || vehicle.VEHICLE_TYPE!=data.VehicleType) throw new InvalidOperationException("請選擇商務資源池內相符的車種類型與車號");
   Func<string,bool> driver=id=>resources.Any(x=>x.RESOURCE_ID==id && x.KIND=="DRIVER" && x.POOL=="BIZ");
   if (String.IsNullOrEmpty(data.Driver1) || !(driver(data.Driver1) || data.Driver1=="SELF" && r.SELF_DRIVE==1)) throw new InvalidOperationException("請選擇有效的駕駛人 1；使用者自駕須申請單允許自駕");
   if (!String.IsNullOrEmpty(data.Driver2) && (data.Driver2=="SELF" || !driver(data.Driver2) || data.Driver1==data.Driver2)) throw new InvalidOperationException("駕駛人 2 須為有效且不重複的司機");
   if (!data.StartKm.HasValue || !data.EndKm.HasValue || data.StartKm<0 || data.EndKm<data.StartKm || data.EndKm>999999999m || Decimal.Round(data.StartKm.Value,3)!=data.StartKm || Decimal.Round(data.EndKm.Value,3)!=data.EndKm) throw new InvalidOperationException("里程必填、不得為負數，結束須大於或等於起始；最多 9 位整數與 3 位小數");
   return new CARGO_D_USAGE { REQUEST_ID=r.REQUEST_ID,REVISION_NO=history.Select(x=>x.REVISION_NO).DefaultIfEmpty(0).Max()+1, VEHICLE_TYPE=data.VehicleType,VEHICLE=data.Vehicle,DRIVER1=data.Driver1,DRIVER2=data.Driver2,
    START_KM=data.StartKm.Value,END_KM=data.EndKm.Value,DISTANCE_KM=Decimal.Round(data.EndKm.Value-data.StartKm.Value,1,MidpointRounding.AwayFromZero),RECORDED_BY=actor.UserId,RECORDED_AT=DateTime.Now };
  }
 }
}
