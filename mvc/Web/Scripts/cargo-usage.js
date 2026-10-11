(function(){'use strict';
var form=document.getElementById('usage-form');if(!form)return;form.noValidate=true;
var type=document.getElementById('Form_VehicleType'),vehicle=document.getElementById('Form_Vehicle');
var available=Array.prototype.map.call(document.querySelectorAll('#usage-vehicles span'),function(x){return {id:x.getAttribute('data-id'),type:x.getAttribute('data-type'),name:x.getAttribute('data-name')};});
function fillVehicle(selected){vehicle.textContent='';var blank=document.createElement('option');blank.value='';blank.textContent=type.value?'請選擇':'請先選車種類型';vehicle.appendChild(blank);available.filter(function(x){return x.type===type.value;}).forEach(function(x){var o=document.createElement('option');o.value=x.id;o.textContent=x.name+'（'+x.id+'）';vehicle.appendChild(o);});vehicle.value=selected||'';}
fillVehicle(vehicle.value);type.addEventListener('change',function(){fillVehicle('');});
var start=document.getElementById('Form_StartKm'),end=document.getElementById('Form_EndKm'),distance=document.getElementById('usage-distance');
function mileageThousandths(value){
var parts=/^(\d*)(?:\.(\d*))?$/.exec(value);if(!parts||!(parts[1]||parts[2]))return null;
var whole=Number(parts[1]||'0'),fraction=(parts[2]||'').replace(/0+$/,'');if(whole>999999999||fraction.length>3)return null;
var thousandths=whole*1000+Number((fraction+'000').slice(0,3));return thousandths<=999999999000?thousandths:null;
}
function showDistance(){end.setCustomValidity('');var startValue=mileageThousandths(start.value),endValue=mileageThousandths(end.value);if(startValue===null||endValue===null){distance.textContent='—';return;}var n=endValue-startValue;if(n<0){end.setCustomValidity('結束里程不可小於起始里程');distance.textContent='結束里程小於起始里程';}else{
// Accepted values fit exactly in integer thousandths; round midpoint away from zero like Decimal.Round on the server.
var tenths=Math.floor((n+50)/100);distance.textContent=Math.floor(tenths/10)+'.'+(tenths%10)+' km';}}
start.addEventListener('input',showDistance);end.addEventListener('input',showDistance);showDistance();
form.addEventListener('submit',function(event){event.preventDefault();showDistance();if(!window.cargoValidateForm(form,{'Form.VehicleType':'請選擇車種類型','Form.Vehicle':'請選擇車號','Form.Driver1':'請選擇駕駛人 1','Form.StartKm':'請填寫起始里程','Form.EndKm':'請填寫結束里程'}))return;window.cargoConfirm('確認儲存實登？行駛 '+distance.textContent+'，前次紀錄將保留於歷程。',function(){HTMLFormElement.prototype.submit.call(form);});});
}());
