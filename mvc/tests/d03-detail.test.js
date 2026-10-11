const {test}=require('node:test'),assert=require('node:assert/strict');
const decide=require('../Web/Scripts/cargo-d03-detail');
const base={freeVehicle:true,freeDriver:true,vehicle:'V1',driver1:'D1',driver2:'',selfDrive:false};
for(const [name,changes,expected] of [
 ['no vehicles',{freeVehicle:false},'無可用車輛'],['no selection',{vehicle:''},'請選擇車輛'],
 ['blocked vehicle',{vehicleBusy:true},'車輛不可用'],['second without first',{driver1:'',driver2:'D2'},'先選第一位'],
 ['duplicate drivers',{driver2:'D1'},'不可重複'],['blocked driver',{driverBusy:true},'司機不可用'],
 ['available driver required even for self drive',{driver1:'',selfDrive:true},'尚有可派司機']]) {
 test(name,()=>assert.match(decide({...base,...changes}).error,new RegExp(expected)));
}
test('single and dual driver outcomes',()=>{
 assert.equal(decide(base).outcome,'派車＋派司機');assert.match(decide({...base,driver2:'D2'}).outcome,/雙駕駛/);
});
test('no drivers: self-drive or no vehicle according to request',()=>{
 assert.equal(decide({...base,freeDriver:false,driver1:'',selfDrive:true}).outcome,'派車（使用者自行駕駛）');
 assert.equal(decide({...base,freeDriver:false,driver1:''}).noVehicle,true);
});

test('reassignment validation requires a driver in supplement mode while allowing a changed vehicle',()=>{
 const base={vehicle:'V2',originalVehicle:'V1',driver1:'',driver2:'',originalDriver1:'',originalDriver2:'',selfDrive:true,supplement:true,effectiveAt:'2030-01-01T10:00'};
 assert.match(decide.reassign(base).error,/補派司機至少/);
 assert.equal(decide.reassign({...base,driver1:'D1'}).valid,true);
 assert.equal(decide.reassign({...base,supplement:false}).valid,true);
 assert.match(decide.reassign({...base,supplement:false,vehicle:'V1'}).error,/皆未變更/);
 assert.match(decide.reassign({...base,driver1:'D1',driver2:'D1'}).error,/不可重複/);
 assert.match(decide.reassign({...base,driver1:'D1',driverBusy:true}).error,/資源不可用/);
});
test('supplement mode and cancellation update preview and confirmation without locking vehicle',()=>{
 const fs=require('node:fs'),vm=require('node:vm');
 const control=(value,original)=>({value,options:[{value:'',textContent:'無'},{value:'V1',textContent:'車一'},{value:'V2',textContent:'車二'},{value:'D1',textContent:'駕駛甲'}],selectedIndex:0,listeners:{},getAttribute:()=>original,addEventListener(event,fn){this.listeners[event]=fn},focus(){this.focused=true}});
 const vehicle=control('V2','V1'),first=control('',''),second=control('',''),time=control('2030-01-01T10:00'),mode=control('false'),button={},status={},preview={},cancel=control(),supplement=control();
 const controls={Vehicle:vehicle,Driver1:first,Driver2:second,EffectiveAt:time,SupplementDriver:mode};
 const attrs={'data-self-drive':'true'};
 const form={querySelector:s=>s==='[value=reassign]'?button:controls[s.match(/name=(.+)\]/)[1]],getAttribute:n=>attrs[n],setAttribute:(n,v)=>attrs[n]=v,parentNode:{querySelector:()=>status},scrollIntoView(){this.scrolled=true}};
 const document={querySelector:s=>s==='[data-resource-url]'?form:null,getElementById:id=>({'d03-reassign-preview':preview,'d03-cancel-supplement':cancel,'d03-supplement':supplement})[id]};
 vm.runInNewContext(fs.readFileSync(require.resolve('../Web/Scripts/cargo-d03-detail'),'utf8'),{document});
 supplement.listeners.click();assert.equal(vehicle.value,'V1');assert.equal(mode.value,'true');assert.equal(button.disabled,true);assert.equal(first.focused,true);assert.equal(form.scrolled,true);assert.equal(vehicle.disabled,undefined);
 vehicle.value='V2';first.value='D1';first.listeners.change();assert.equal(button.disabled,false);assert.match(preview.textContent,/車一 → 車二/);assert.match(attrs['data-confirm'],/駕駛甲/);assert.match(attrs['data-confirm'],/2030-01-01 10:00/);
 cancel.listeners.click();assert.equal(mode.value,'false');assert.equal(cancel.hidden,true);
});
