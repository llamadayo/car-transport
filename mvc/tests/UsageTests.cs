using System;
using System.Linq;
using CARGO.DTO;
using CARGO.DTO.Tables;
using CARGO.Service;
class UsageTests {
 static int count;
 static CARGO_DUsageActorDto biz=new CARGO_DUsageActorDto {UserId="示範申請人",IsBusiness=true};
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
 static void Reject(Action run,string name){try{run();}catch(InvalidOperationException){Check(true,name);return;}catch(UnauthorizedAccessException){Check(true,name);return;}throw new Exception("Expected rejection: "+name);}
 static CARGO_D05SaveDto Input(long id=5,int version=1){return new CARGO_D05SaveDto {RequestId=id,Version=version,VehicleType="CAR",Vehicle="測試車 C",Driver1="示範駕駛 C",StartKm=100m,EndKm=125.15m};}
 static void Main(){
 var service=new CARGO_D05Service();var driverService=new CARGO_D06Service();var q=new CARGO_D05QueryDto {Page=1,PageSize=20};
 Check(service.Query(q,biz).Total==1,"only signed dispatch eligible");Check(service.Detail(7,biz)==null,"pending sign not visible");Check(service.Detail(6,biz)==null,"no vehicle not eligible");
 Reject(()=>service.Query(q,new CARGO_DUsageActorDto {UserId="driver",IsDriver=true}),"driver cannot use business listing");
 Reject(()=>service.Save(Input(),new CARGO_DUsageActorDto {UserId="driver",IsDriver=true}),"driver cannot save usage");
 Reject(()=>service.Save(Input(7),biz),"pending sign save rejected");
 var data=Input();data.StartKm=null;Reject(()=>service.Save(data,biz),"missing odometer rejected");data.StartKm=-1;Reject(()=>service.Save(data,biz),"negative odometer rejected");data.StartKm=150;Reject(()=>service.Save(data,biz),"reverse odometer rejected");data.StartKm=100;data.EndKm=125.1234m;Reject(()=>service.Save(data,biz),"unsupported odometer precision rejected");
 data=Input();data.VehicleType="BUS";Reject(()=>service.Save(data,biz),"vehicle type mismatch rejected");
 data=Input();data.Driver2=data.Driver1;Reject(()=>service.Save(data,biz),"duplicate drivers rejected");
 data=Input();data.Driver1="SELF";Reject(()=>service.Save(data,biz),"unapproved self drive rejected");
 data=Input();data.Driver2="SELF";Reject(()=>service.Save(data,biz),"second driver cannot be self");
 data=Input();data.Driver1="unknown";Reject(()=>service.Save(data,biz),"unknown resource rejected");
 service.Save(Input(),biz);var saved=service.Detail(5,biz);
 Check(saved.History.Count==1&&saved.History[0].DISTANCE_KM==25.2m,"distance rounded away from midpoint");
 Check(saved.Application.Application.Request.USAGE_AT.HasValue&&saved.Application.Application.Request.VERSION_NO==2,"usage marks logged and increments version");
 Check(saved.Application.Application.Request.VEHICLE=="測試車 A"&&saved.Application.Assignments[0].VEHICLE=="測試車 A","actual vehicle never changes planned assignments");
 Reject(()=>service.Save(Input(),biz),"stale usage edit rejected");Check(service.Detail(5,biz).History.Count==1,"failed write leaves history unchanged");
 data=Input(5,2);data.EndKm=100;service.Save(data,biz);saved=service.Detail(5,biz);
 Check(saved.History.Count==2&&saved.History[0].DISTANCE_KM==0&&saved.History[1].END_KM==125.15m,"revision history append only and zero distance allowed");
 Check(service.Query(new CARGO_D05QueryDto {Keyword="測試車 C",Status="done",Page=1,PageSize=20},biz).Total==1,"search actual vehicle and done status");
 Check(service.Query(new CARGO_D05QueryDto {Status="todo",Page=1,PageSize=20},biz).Total==0,"pending filter excludes saved usage");
 var mine=new CARGO_DUsageActorDto {UserId="示範申請人",IsDriver=true};
 Check(driverService.Tasks(mine).Count==1&&driverService.Tasks(mine)[0].DriverId=="示範駕駛","driver sees own planned task after actual driver differs");
 Check(driverService.Tasks(new CARGO_DUsageActorDto {UserId="unmapped",IsDriver=true}).Count==0,"unmapped account sees no other tasks");
 Reject(()=>driverService.Tasks(new CARGO_DUsageActorDto {UserId="user"}),"task role required");
 var record=CARGO_D01DemoData.Records[5];var segment=record.Assignments[0];segment.DRIVER2="示範駕駛 B";
 Check(driverService.Tasks(biz).Count==2&&driverService.Tasks(mine).Single().Partner=="示範駕駛 B","dual drivers get own task with partner");
 var cut=segment.FROM_AT.AddHours(2);var end=segment.TO_AT;segment.TO_AT=cut;record.Assignments.Add(new CARGO_D_ASSIGNMENT {REQUEST_ID=5,LINE_NO=2,FROM_AT=cut,TO_AT=end,VEHICLE="測試車 D",DRIVER1="示範駕駛 C",KIND="換車／換司機"});
 Check(driverService.Tasks(mine).Single().Assignment.TO_AT==cut&&driverService.Tasks(biz).Count==3,"reassignment preserves prior driver interval");
 var self=CARGO_D01DemoData.Records[4];self.Application.Request.STATUS="dispatched";self.Application.Request.SIGN_STATUS="approved";self.Application.Request.DISPATCH_NO="GD000004";self.Application.Request.SELF_DRIVE=1;
 self.Assignments.Add(new CARGO_D_ASSIGNMENT {REQUEST_ID=4,LINE_NO=1,FROM_AT=self.Application.Request.START_AT.Value,TO_AT=self.Application.Request.END_AT.Value,VEHICLE="測試車 D"});
 Check(driverService.Tasks(biz).Any(x=>x.DriverId=="SELF")&&!driverService.Tasks(mine).Any(x=>x.DriverId=="SELF"),"self drive handover limited to business overview");
 data=Input(4,1);data.Driver1="SELF";service.Save(data,biz);Check(service.Detail(4,biz).History[0].DRIVER1=="SELF","self drive permitted for eligible request");
 saved.History[0].VEHICLE="tampered";Check(service.Detail(5,biz).History[0].VEHICLE!="tampered","returned detail detached from stored state");
 Console.WriteLine("PASS "+count+" usage/task checks");
 }
}
