(function(){'use strict';
var form=document.getElementById('usage-form');if(!form)return;
var type=document.getElementById('Form_VehicleType'),vehicle=document.getElementById('Form_Vehicle');
var available=Array.prototype.map.call(document.querySelectorAll('#usage-vehicles span'),function(x){return {id:x.getAttribute('data-id'),type:x.getAttribute('data-type'),name:x.getAttribute('data-name')};});
function fillVehicle(selected){vehicle.textContent='';var blank=document.createElement('option');blank.value='';blank.textContent=type.value?'請選擇':'請先選車種類型';vehicle.appendChild(blank);available.filter(function(x){return x.type===type.value;}).forEach(function(x){var o=document.createElement('option');o.value=x.id;o.textContent=x.name+'（'+x.id+'）';vehicle.appendChild(o);});vehicle.value=selected||'';}
fillVehicle(vehicle.value);type.addEventListener('change',function(){fillVehicle('');});
var start=document.getElementById('Form_StartKm'),end=document.getElementById('Form_EndKm'),distance=document.getElementById('usage-distance');
function showDistance(){end.setCustomValidity('');if(start.value===''||end.value===''){distance.textContent='—';return;}var n=Number(end.value)-Number(start.value);if(n<0){end.setCustomValidity('結束里程不可小於起始里程');distance.textContent='結束里程小於起始里程';}else distance.textContent=(Math.round((n+Number.EPSILON)*10)/10).toFixed(1)+' km';}
start.addEventListener('input',showDistance);end.addEventListener('input',showDistance);showDistance();
form.addEventListener('submit',function(event){event.preventDefault();showDistance();if(!form.reportValidity())return;window.cargoConfirm('確認儲存實登？行駛 '+distance.textContent+'，前次紀錄將保留於歷程。',function(){HTMLFormElement.prototype.submit.call(form);});});
}());
