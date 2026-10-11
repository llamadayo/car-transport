const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const { test } = require('node:test');
const path = require('node:path');
const read = name => fs.readFileSync(path.join(__dirname, '../Web/Scripts/', name), 'utf8');
function fixture() {
  const controls = [];
  const elements = {};
  function add(name, value, type='text') {
    const control = { name, id:name, value, type, checked:false, disabled:false, validity:true,
      getAttribute(){return null;}, setAttribute(){}, removeAttribute(){},
      focus(){}, scrollIntoView(){}, checkValidity(){return this.validity;}, validationMessage:'格式不正確'
    };
    controls.push(control); elements[name]=control; return control;
  }
  add('Applicant','測試'); add('Preference1','CAR'); add('Preference2',''); add('Preference3','');
  add('StartAtDate','2026-10-12'); add('StartAtTime','09:00');
  add('EndAtDate','2026-10-12'); add('EndAtTime','12:00');
  add('StartAt','', 'hidden'); add('EndAt','', 'hidden'); add('Passengers','1','number');
  add('SelfDrive','false','radio').checked=true;
  add('RequestReview','true','radio').checked=true;
  add('Category','general'); add('TransportType','一般');
  for (const id of ['exercise-field','self-driver-section','submit-request']) elements[id]={};
  const handlers={}, toasts=[]; let confirms=0, posts=0, confirm;
  const form={ noValidate:false, addEventListener(name, fn){(handlers[name] ||= []).push(fn);},
    querySelectorAll(selector){return selector==='.radio-pill'?[]:controls;},
    querySelector(selector){
      if(selector[0]==='#')return elements[selector.slice(1)];
      const name=/name="([^"]+)"/.exec(selector)?.[1];
      return controls.find(c=>c.name===name && (!selector.includes(':checked')||c.checked))||null;
    }, appendChild(){} };
  const context={window:{cargoToast:m=>toasts.push(m),cargoConfirm:(m,fn)=>{confirms++;confirm=fn;}},
    document:{getElementById:id=>id==='application-form'?form:elements[id],createElement:()=>({})},
    HTMLFormElement:{prototype:{submit(){posts++;}}}};
  vm.runInNewContext(read('cargo-d01-validation.js'),context);
  vm.runInNewContext(read('cargo-d01.js'),context);
  return {elements,add,toasts,form,get confirms(){return confirms;},get posts(){return posts;},
    submit(command='submit'){handlers.submit[0]({submitter:{value:command},preventDefault(){}});},
    accept(){confirm();}};
}
for(const command of ['submit','draft']) {
  test(command+': required fields block confirmation and submission',()=>{
    for(const field of ['Applicant','Preference1','StartAtDate','StartAtTime','EndAtDate','EndAtTime','Passengers','SelfDrive']){
      const ui=fixture();
      if(field==='SelfDrive')ui.elements[field].checked=false;else ui.elements[field].value=' ';
      ui.submit(command);
      assert.equal(ui.confirms,0,field);assert.equal(ui.posts,0,field);assert.equal(ui.toasts.length,1,field);
    }
  });
}
test('valid form waits for confirmation before posting',()=>{
 const ui=fixture();ui.submit();assert.equal(ui.confirms,1);assert.equal(ui.posts,0);ui.accept();assert.equal(ui.posts,1);
});
test('radio false and numeric zero weight are not treated as empty',()=>{
 const ui=fixture();ui.add('Cargo[0].Weight','0','number');ui.submit();assert.equal(ui.confirms,1);
});
test('dynamic missing fields, invalid numbers, date order and duplicate preferences block confirmation',()=>{
 const cases=[
 u=>u.add('Locations[0].Name',' '),u=>u.add('Cargo[0].Name',''),u=>u.add('Cargo[0].Category',''),
 u=>{u.elements.Passengers.validity=false;},
 u=>{u.elements.EndAtTime.value='08:00';},
 u=>{u.elements.Preference2.value='CAR';}
 ];
 for(const change of cases){const ui=fixture();change(ui);ui.submit();assert.equal(ui.confirms,0);assert.equal(ui.toasts.length,1);}
});
test('toast uses plain text, replaces the previous message and disappears after four seconds',()=>{
 const elements={};let timer,delay;
 function element(){return {textContent:'',setAttribute(){},addEventListener(){},appendChild(child){this.child=child;if(child.id)elements[child.id]=child;}};}
 elements['menu-toggle']=element();elements.clock=element();
 const body=element();
 const ctx={window:{matchMedia:()=>({matches:false})},document:{body,getElementById:id=>elements[id],querySelector:()=>null,createElement:element},
 setInterval(){},clearTimeout(){},setTimeout(fn,ms){timer=fn;delay=ms;return 1;}};
 vm.runInNewContext(read('cargo-shell.js'),ctx);
 ctx.window.cargoToast('第一個訊息');
 ctx.window.cargoToast('<b>請選擇是否自駕</b>');
 assert.equal(elements['toast-wrap'].child.textContent,'<b>請選擇是否自駕</b>');
 assert.equal(delay,4000);timer();assert.equal(elements['toast-wrap'].textContent,'');
});
