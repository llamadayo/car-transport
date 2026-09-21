/* Capture expected results from the unchanged JavaScript prototype; C# consumes this file. */
const fs = require('fs');
const path = require('path');
const {fresh} = require('../tests/loader');
const target = path.join(__dirname, '../tests/fixtures/module-a/parity.json');
const ctx = fresh(), db = ctx.DB;
const item = x => ({ApplicationId:0,LineNo:1,Name:x.name,LengthCm:x.l,WidthCm:x.w,HeightCm:x.h,Category:x.category,Qty:x.qty,Weight:x.weight});
const master = {
 Sites:db.branches.map((b,i)=>({Id:b.id,Name:b.name,SortOrder:i})),
 Stations:db.stations.map(s=>({Id:s.id,Name:s.name,BranchId:s.branch,SortOrder:s.order})),
 Vehicles:db.vehicles.map(v=>({Id:v.id,Name:v.name,LengthCm:v.dims?.l||0,WidthCm:v.dims?.w||0,HeightCm:v.dims?.h||0,Volume:v.volume,WeightLimit:v.weight})),
 Shifts:db.regionalShifts.map((s,i)=>({Id:s.id,BranchId:s.branch,Label:s.label,DepartMinute:ctx.hhmmToMin(s.depart),SortOrder:i,VehicleId:s.vehicle,DriverId:ctx.ModuleA.defaultDriverFor(s.vehicle)})),
 Categories:db.wasteFactors.map(c=>({Code:c.code,Name:c.name,Factor:c.factor,Active:c.active?1:0})),
 Config:{WasteDefault:db.wasteDefault,InterStationMin:3,ShiftHandleBudget:60}
};
const cargo = (over={}) => ({name:'測試箱',l:50,w:40,h:30,qty:1,weight:10,category:'BOX',...over});
const cases=[];
function appDto(a) {return {Id:Number(a.id.slice(2)),SubmitSeq:a.submitSeq,Applicant:a.applicant,BranchId:a.branch,PickStationId:a.pickStation,DropStationId:a.station,RecvMode:a.recvMode,ServiceDate:a.serviceDate,ExpectedMinute:a.deliverTime?ctx.hhmmToMin(a.deliverTime):null,LoadMin:a.loadMin,UnloadMin:a.unloadMin,Status:a.status,ShiftId:a.assignedShift,Items:a.items.map(item)};}
function add(name,now='2026-09-21T07:00:00',before=[],last={}) {
 const c=fresh();c.ModuleA.now=()=>new Date(now);
 const basic={applicant:'測試人',branch:'D1',pickStation:'D1-100',station:'D1-900',recvMode:'asap',serviceDate:'2026-09-21',loadMin:10,unloadMin:5,items:[cargo()]};
 before.forEach(x=>c.ModuleA.submit({...basic,...x}));
 const existing=c.ModuleA.applications.map(appDto);
 const a=c.ModuleA.createApp({...basic,...last});const candidate=appDto(a);const r=c.ModuleA.match(a);
 cases.push({Name:name,Now:now,Application:candidate,Existing:existing,Expected:{Ok:r.ok,Reason:r.reason||null,ShiftId:r.shift?r.shift.id:null,ArrivalMinute:r.arrival?c.hhmmToMin(r.arrival):null,ExpectDiffMin:r.expectDiffMin??null}});
}
add('asap first departure');add('today departure equals now skips','2026-09-21T08:00:00');add('last departure cutoff','2026-09-21T18:00:00');
add('future date full timetable','2026-09-21T20:00:00',[],{recvMode:'exact',serviceDate:'2026-09-22'});
add('past date rejected','2026-09-21T07:00:00',[],{serviceDate:'2026-09-20'});
add('exact nearest','2026-09-21T07:00:00',[],{recvMode:'exact',deliverTime:'14:00'});
add('exact tie keeps timetable order','2026-09-21T07:00:00',[],{recvMode:'exact',deliverTime:'08:57'});
add('exact earlier departure skipped','2026-09-21T14:00:00',[],{recvMode:'exact',deliverTime:'08:00'});
add('exact blank time earliest','2026-09-21T07:00:00',[],{recvMode:'exact'});
add('sixty minute budget equality','2026-09-21T07:00:00',[{loadMin:45,unloadMin:0}]);
add('sixty minute budget exceeded','2026-09-21T07:00:00',[{loadMin:46,unloadMin:0}]);
add('single over budget','2026-09-21T07:00:00',[],{loadMin:61,unloadMin:0});
add('all shifts full','2026-09-21T07:00:00',Array.from({length:11},()=>({loadMin:60,unloadMin:0})));
add('different dates isolated','2026-09-21T07:00:00',[{loadMin:60,unloadMin:0}],{recvMode:'exact',serviceDate:'2026-09-22'});
add('different branches isolated','2026-09-21T07:00:00',[{loadMin:60,unloadMin:0}],{branch:'D2',pickStation:'D2-100',station:'D2-900'});
add('too big all empty vehicles','2026-09-21T07:00:00',[],{items:[cargo({l:480,w:25,h:25})]});
add('unknown category fallback','2026-09-21T07:00:00',[],{items:[cargo({category:'UNKNOWN'})]});
const heavy=cargo({weight:2000});
add('overlapping weight moves shift','2026-09-21T07:00:00',[{station:'D1-600',items:[heavy]}],{pickStation:'D1-300',items:[heavy]});
add('unload releases capacity at same station','2026-09-21T07:00:00',[{station:'D1-600',items:[heavy]}],{pickStation:'D1-600',items:[heavy]});
const loads=[];
for(const [name,items] of [ ['normal',[cargo()]],['unknown',[cargo({category:'UNKNOWN'})]],['aspect exactly two',[cargo({l:60,w:30,h:30})]],['aspect above two',[cargo({l:60.01,w:30,h:30})]],['aspect exactly three',[cargo({l:90,w:30,h:30})]],['aspect above three',[cargo({l:90.01,w:30,h:30})]],['rotation required',[cargo({l:180,w:400,h:180})]],['weight equality',[cargo({weight:3000})]],['weight exceeded',[cargo({weight:3000.001})]],['quantity floor overflow',[cargo({l:200,w:150,h:150,qty:4})]],['mixed',[cargo(),cargo({category:'PALLET',l:100,w:90,h:80,qty:2})]] ]) {
 const v=db.vehicles.find(x=>x.id==='V-L01');const r=ctx.checkLoad(items,v);const effective=ctx.effectiveLoad(items);
 loads.push({Name:name,Items:items.map(item),VehicleId:v.id,Expected:{Ok:r.ok,Volume:effective.volume,Weight:effective.weight,Floor:effective.floor}});
}
fs.mkdirSync(path.dirname(target),{recursive:true});fs.writeFileSync(target,JSON.stringify({Master:master,Matches:cases,Loads:loads},null,2)+'\n');
console.log(`Captured ${cases.length} matching and ${loads.length} load cases from original JS.`);
