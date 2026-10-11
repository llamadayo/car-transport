const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm'),path=require('node:path');
const read=n=>fs.readFileSync(path.join(__dirname,'../Web/Scripts',n),'utf8');
function field(name,value='',type='text',required=false){
 return {name,value,type,required,disabled:false,textContent:'',listeners:{},labels:[{textContent:name}],
 getAttribute(){return null},setAttribute(){},removeAttribute(){},focus(){},scrollIntoView(){},appendChild(){},
 addEventListener(n,fn){this.listeners[n]=fn},setCustomValidity(v){this.error=v},checkValidity(){return !this.error},get validationMessage(){return this.error||''}};
}
function setup(controls,attrs={}){
 let confirms=0,posts=0,callback;const messages=[],handlers={};
 const form={querySelectorAll:s=>s==='button[type=submit]'?[]:controls,
 querySelector:s=>{const name=/name=([^\]]+)/.exec(s)?.[1];return controls.find(c=>c.name===name && (!s.includes('[type=hidden]') || c.type==='hidden') && (!s.includes('[type=radio]') || c.type==='radio') && (!s.includes(':checked') || c.checked))||null;},
 getAttribute:n=>attrs[n]||'',addEventListener:(n,fn)=>handlers[n]=fn,appendChild(){}};
 const context={window:{cargoToast:m=>messages.push(m),cargoConfirm:(m,fn)=>{confirms++;callback=fn;}},
 HTMLFormElement:{prototype:{submit(){posts++;}}},jQuery(){},document:{querySelectorAll:s=>s==='.workflow-action'?[form]:[],createElement:()=>field(''),
 getElementById:id=>id==='usage-form'?form:controls.find(c=>c.id===id)}};
 vm.runInNewContext(read('cargo-validation.js'),context);
 return {context,form,messages,run:file=>vm.runInNewContext(read(file),context),
 submit:command=>handlers.submit({preventDefault(){},submitter:command?{name:'Command',value:command}:null}),
 accept:()=>callback(),get confirms(){return confirms},get posts(){return posts}};
}
test('D02/D04 reject needs note; approve remains optional and no stale error after reject',()=>{
 const note=field('Note',' '),ui=setup([note]);ui.run('cargo-workflow.js');
 ui.submit('reject');assert.equal(ui.confirms,0);assert.equal(ui.messages.length,1);
 ui.submit('approve');assert.equal(ui.confirms,1);assert.equal(ui.posts,0);ui.accept();assert.equal(ui.posts,1);
});
test('D03 dispatch vehicle required; noVehicle does not require vehicle or driver',()=>{
 const ui=setup([field('Vehicle'),field('Driver1'),field('Driver2')]);ui.run('cargo-workflow.js');
 ui.submit('dispatch');assert.equal(ui.confirms,0);ui.submit('noVehicle');assert.equal(ui.confirms,1);
});
test('D03 self drive remains allowed; driver 2 requires driver 1',()=>{
 const d2=field('Driver2'),ui=setup([field('Vehicle','V1'),field('Driver1'),d2]);ui.run('cargo-workflow.js');
 ui.submit('dispatch');assert.equal(ui.confirms,1);d2.value='D2';ui.submit('dispatch');assert.equal(ui.confirms,1);
});
test('D03 reassign respects self-drive and effective date requirements',()=>{
 const d1=field('Driver1'),date=field('EffectiveAt'),ui=setup([field('Vehicle','V'),d1,field('Driver2'),date],{'data-self-drive':'false'});
 ui.run('cargo-workflow.js');ui.submit('reassign');assert.equal(ui.confirms,0);
 d1.value='D';ui.submit('reassign');assert.equal(ui.confirms,0);date.value='2026-10-12T10:00';ui.submit('reassign');assert.equal(ui.confirms,1);
});
test('D03 hidden extend command validates date; returnReject note remains optional',()=>{
 const end=field('EndAt'),command=field('Command','extend','hidden'),ui=setup([end,command]);ui.run('cargo-workflow.js');
 ui.submit();assert.equal(ui.confirms,0);end.value='2026-10-12T12:00';ui.submit();assert.equal(ui.confirms,1);
 const other=setup([field('Note')]);other.run('cargo-workflow.js');other.submit('returnReject');assert.equal(other.confirms,1);
});
function usage(){
 const controls=['VehicleType','Vehicle','Driver1','Driver2','StartKm','EndKm'].map(n=>{
 const c=field('Form.'+n,n==='StartKm'?'0':n==='EndKm'?'1':n==='Driver2'?'':'X');c.id='Form_'+n;return c;});
 const distance=field('distance');distance.id='usage-distance';controls.push(distance);
 const ui=setup(controls);ui.run('cargo-usage.js');return {ui,controls};
}
test('D05 every required field blocks SweetAlert; zero mileage and optional driver2 allowed',()=>{
 for(const name of ['VehicleType','Vehicle','Driver1','StartKm','EndKm']){
 const {ui,controls}=usage();controls.find(c=>c.name==='Form.'+name).value='';ui.submit();assert.equal(ui.confirms,0,name);assert.equal(ui.messages.length,1);
 }
 const {ui}=usage();ui.submit();assert.equal(ui.confirms,1);assert.equal(ui.posts,0);ui.accept();assert.equal(ui.posts,1);
});
test('D05 reversed mileage shows toast and correction permits confirmation',()=>{
 const {ui,controls}=usage();const end=controls.find(c=>c.name==='Form.EndKm'),start=controls.find(c=>c.name==='Form.StartKm');
 start.value='100';end.value='99';ui.submit();assert.equal(ui.confirms,0);assert.match(ui.messages[0],/結束里程不可小於/);
 end.value='101';ui.submit();assert.equal(ui.confirms,1);
});

test('D02 selected decision drives validation and posting without a submit-button command',()=>{
 const approve=field('Command','approve','radio'),reject=field('Command','reject','radio'),note=field('Note',' ');
 approve.checked=false;reject.checked=true;
 const ui=setup([approve,reject,note]);ui.run('cargo-workflow.js');
 ui.submit();assert.equal(ui.confirms,0);assert.equal(ui.posts,0);
 note.value='請補充行程';ui.submit();assert.equal(ui.confirms,1);ui.accept();assert.equal(ui.posts,1);
 reject.checked=false;approve.checked=true;note.value='';ui.submit();assert.equal(ui.confirms,2);
});
