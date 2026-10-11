// Local browser regression fixtures. Run: node mvc/tests/serve-ui-checks.js
// Only the two fixture pages and three named production scripts are served.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const web = path.join(__dirname, '..', 'Web');
const scripts = ['cargo-validation.js', 'cargo-d01-validation.js', 'cargo-shell.js', 'cargo-d01.js', 'cargo-usage.js'];
function template(kind) {
  const source = fs.readFileSync(path.join(web, 'Views', 'CARGO_D01', '_' + kind + '.cshtml'), 'utf8');
  const markup = /<template[^>]*>[\s\S]*?<\/template>/.exec(source)[0];
  return markup.replace(/@foreach[\s\S]*?\{[\s\S]*?\}/g, '');
}
function page(title, body, script, setup = '') {
  return `<!doctype html><html lang="zh-TW"><meta charset="utf-8"><title>${title}</title>
<style>body{font:16px sans-serif;margin:24px;color:#123}table{border-collapse:collapse;margin:12px 0}
td,th{padding:8px;border:1px solid #bbb}input{width:110px;padding:6px}button,select{padding:8px;margin:4px}
button:disabled{opacity:.4}pre{white-space:pre-wrap}#confirm-mask{position:fixed;inset:0;background:#0009}
#confirm-mask>div{background:white;margin:20vh auto;padding:24px;width:480px}#confirm-mask[hidden]{display:none}</style>
<button id="menu-toggle" type="button" aria-expanded="true">選單</button><span id="clock"></span>
<nav><a href="/d01">D01 測試</a> | <a href="/usage">里程測試</a></nav><h1>${title}</h1>
${body}<pre id="posted" aria-live="polite"></pre>
<link rel="stylesheet" href="/vendor/sweetalert2.min.css"><script src="/vendor/sweetalert2.min.js"></script>
<script>HTMLFormElement.prototype.submit=function(){document.getElementById('posted').textContent=
'Confirmed (fixture only)\\n'+JSON.stringify(Array.from(new FormData(this).entries()),null,2);};</script>
<script src="/scripts/cargo-shell.js"></script><script src="/scripts/cargo-validation.js"></script><script src="/scripts/cargo-d01-validation.js"></script><script src="/scripts/${script}"></script>
<script>${setup}</script></html>`;
}
const d01 = page('D01 數量與行程排序回歸', `<form id="application-form">
<div hidden><select id="Category"><option value="general">general</option></select>
<select id="TransportType"><option>一般</option></select><div id="exercise-field"></div>
<input name="SelfDrive" type="radio" value="false" checked><input name="RequestReview" type="radio" value="true" checked>
<div id="self-driver-section"></div><input id="StartAtDate" value="2026-10-10"><input id="StartAtTime" value="10:00">
<input id="EndAtDate" value="2026-10-10"><input id="EndAtTime" value="11:00">
<input id="StartAt" name="StartAt"><input id="EndAt" name="EndAt"></div>
<button type="button" data-add="Locations">新增地點</button><table><thead><tr><th>地點</th><th>排序</th><th>操作</th></tr></thead>
<tbody id="rows-Locations"></tbody></table>${template('Locations')}
<button type="button" data-add="Cargo">新增貨物</button><table><thead><tr><th>品名</th><th>長</th><th>寬</th><th>高</th>
<th>重量</th><th>件數</th><th>品類</th><th>外包裝</th><th>危險品</th><th>操作</th></tr></thead>
<tbody id="rows-Cargo"></tbody></table>${template('Cargo')}
<button type="button" id="quantity-check">檢查件數</button><output id="quantity-validity" aria-live="polite"></output>
<button type="submit" id="submit-request" name="command" value="submit">送出測試申請</button></form>`, 'cargo-d01.js', `
['A','C','B'].forEach(function(name){document.querySelector('[data-add="Locations"]').click();
document.querySelector('#rows-Locations tr:last-child input').value=name;});
document.querySelector('[data-add="Cargo"]').click();
function quantityValidity(){var inputs=document.querySelectorAll('#rows-Cargo input[name$=".Quantity"]');
document.getElementById('quantity-validity').textContent=Array.from(inputs).map(function(input){
return 'Quantity '+input.value+': '+(input.checkValidity()?'valid':'invalid');}).join('; ');}
document.getElementById('quantity-check').onclick=quantityValidity;
document.getElementById('application-form').addEventListener('input',quantityValidity);quantityValidity();`);
const usage = page('實登里程四捨五入回歸', `<form id="usage-form">
<div id="usage-vehicles" hidden><span data-id="TEST-001" data-type="CAR" data-name="測試車"></span></div>
<label>車種 <select id="Form_VehicleType" name="VehicleType" required><option value="CAR">CAR</option></select></label>
<label>車輛 <select id="Form_Vehicle" name="Vehicle" required><option value="TEST-001" selected>TEST-001</option></select></label>
<p><label>起始里程 <input id="Form_StartKm" name="StartKm" type="number" min="0" max="999999999" step="0.001" value="100" required></label>
<label>結束里程 <input id="Form_EndKm" name="EndKm" type="number" min="0" max="999999999" step="0.001" value="101.05" required></label></p>
<p>行駛里程：<output id="usage-distance" aria-live="polite"></output></p><button type="submit">儲存測試實登</button></form>`, 'cargo-usage.js');
const routes = new Map([['/d01', ['text/html', d01]], ['/usage', ['text/html', usage]]]);
for (const name of scripts) routes.set('/scripts/' + name,
  ['text/javascript', fs.readFileSync(path.join(web, 'Scripts', name), 'utf8')]);
for (const [name, type] of [['sweetalert2.min.js', 'text/javascript'], ['sweetalert2.min.css', 'text/css']]) {
  routes.set('/vendor/' + name, [type, fs.readFileSync(path.join(web, 'Content/vendor/sweetalert2', name), 'utf8')]);
}
const server = http.createServer((request, response) => {
  if (request.method !== 'GET') { response.writeHead(405); response.end('GET only'); return; }
  const route = routes.get(request.url);
  if (!route) { response.writeHead(404); response.end('Not found'); return; }
  response.writeHead(200, { 'Content-Type': route[0] + '; charset=utf-8', 'Cache-Control': 'no-store' });
  response.end(route[1]);
});
server.listen(5199, '127.0.0.1', () => console.log('UI checks: http://127.0.0.1:5199/d01 and /usage'));
