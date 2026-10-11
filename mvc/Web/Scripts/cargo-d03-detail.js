(function () {
    'use strict';
    function decide(s) {
        if (!s.freeVehicle) return { error: '此時段無可用車輛，請按「無車退回」。' };
        if (!s.vehicle) return { error: '請選擇車輛，或判定無車退回。' };
        if (s.vehicleBusy) return { error: '所選車輛不可用，請重新選擇。' };
        if (!s.driver1 && s.driver2) return { error: '請先選第一位司機，再加派第二位。' };
        if (s.driver1 && s.driver1 === s.driver2) return { error: '兩位司機不可重複。' };
        if (s.driverBusy) return { error: '所選司機不可用，請重新選擇。' };
        if (s.driver1) return { outcome: '派車＋派司機' + (s.driver2 ? '（雙駕駛）' : '') };
        if (s.freeDriver) return { error: '尚有可派司機，應派車＋派司機。' };
        return { outcome: s.selfDrive ? '派車（使用者自行駕駛）' : '無車退回（有車沒司機、未勾自駕；不進候補、不占用資源）', noVehicle: !s.selfDrive };
    }
    function reassign(s) {
        if (!s.vehicle) return { error: '請選擇車輛。' };
        if (s.supplement && !s.driver1) return { error: '補派司機至少需選擇司機 1。' };
        if (!s.driver1 && (s.driver2 || !s.selfDrive)) return { error: '請先選擇司機 1。' };
        if (s.driver1 && s.driver1 === s.driver2) return { error: '兩位司機不可重複。' };
        if (s.vehicleBusy || s.driverBusy) return { error: '所選資源不可用，請重新選擇。' };
        if (!s.effectiveAt) return { error: '請填寫生效時間。' };
        if (s.vehicle === s.originalVehicle && s.driver1 === s.originalDriver1 && s.driver2 === s.originalDriver2) return { error: '車輛與司機皆未變更。' };
        return { valid: true };
    }
    if (typeof module !== 'undefined' && module.exports) { module.exports = decide; module.exports.reassign = reassign; return; }
    var form = document.querySelector('[data-dispatch="true"]');
    if (form) {
        var vehicle = form.querySelector('[name=Vehicle]'), d1 = form.querySelector('[name=Driver1]'), d2 = form.querySelector('[name=Driver2]');
        var button = form.querySelector('[value=dispatch]'), preview = document.getElementById('d03-preview');
        function available(select) { return Array.prototype.some.call(select.options, function (o) { return o.value && !o.disabled; }); }
        function busy(select) { return select.value && select.options[select.selectedIndex].disabled; }
        function update() {
            var r = decide({ freeVehicle: available(vehicle), freeDriver: available(d1), vehicle: vehicle.value,
                vehicleBusy: busy(vehicle), driver1: d1.value, driver2: d2.value, driverBusy: busy(d1) || busy(d2), selfDrive: form.getAttribute('data-self-drive') === 'true' });
            form.dispatchReady = !r.error; button.disabled = !!r.error;
            button.textContent = r.noVehicle ? '確認無車退回' : '完成派車，送簽審';
            preview.textContent = r.error || '判斷結果：' + r.outcome + (r.noVehicle ? '' : '｜車輛 ' + vehicle.options[vehicle.selectedIndex].textContent + (d1.value ? '｜司機 ' + d1.options[d1.selectedIndex].textContent + (d2.value ? '、' + d2.options[d2.selectedIndex].textContent : '') : ''));
            form.setAttribute('data-confirm', r.noVehicle ? '確認無車退回？此案件不占用資源、不送簽審。' : '確認' + (r.outcome || '派車') + '並送運輸主管簽審？');
        }
        [vehicle, d1, d2].forEach(function (select) { select.addEventListener('change', update); }); update();
    }
    var changeForm = document.querySelector('[data-resource-url]');
    var changePreview = document.getElementById('d03-reassign-preview');
    if (changeForm && changePreview) {
        var v = changeForm.querySelector('[name=Vehicle]'), first = changeForm.querySelector('[name=Driver1]'), second = changeForm.querySelector('[name=Driver2]');
        var time = changeForm.querySelector('[name=EffectiveAt]'), mode = changeForm.querySelector('[name=SupplementDriver]');
        var changeButton = changeForm.querySelector('[value=reassign]'), cancelMode = document.getElementById('d03-cancel-supplement');
        function original(select) { return select.getAttribute('data-original') || ''; }
        function label(select, value) {
            var found = Array.prototype.filter.call(select.options, function (o) { return o.value === value; })[0];
            return value ? (found ? found.textContent : value) : '無';
        }
        function blocked(select) { return select.value && select.options[select.selectedIndex].disabled; }
        function updateChange() {
            var result = reassign({ vehicle: v.value, driver1: first.value, driver2: second.value, originalVehicle: original(v), originalDriver1: original(first), originalDriver2: original(second),
                selfDrive: changeForm.getAttribute('data-self-drive') === 'true', supplement: mode.value === 'true', effectiveAt: time.value, vehicleBusy: blocked(v), driverBusy: blocked(first) || blocked(second) });
            var preview = '車輛：' + label(v, original(v)) + ' → ' + label(v, v.value) + '；司機 1：' + label(first, original(first)) + ' → ' + label(first, first.value) + '；司機 2：' + label(second, original(second)) + ' → ' + label(second, second.value) + '；生效時間：' + time.value.replace('T', ' ');
            changePreview.textContent = (mode.value === 'true' ? '補派司機｜' : '') + preview + (result.error ? '。' + result.error : '。異動將立即生效。');
            changeForm.reassignReady = !result.error;
            changeButton.disabled = !!result.error || changeForm.resourcesReady === false;
            changeForm.setAttribute('data-confirm', '確認' + (mode.value === 'true' ? '補派司機' : '異動') + '？' + preview + '。此異動立即生效。');
            cancelMode.hidden = mode.value !== 'true';
        }
        changeForm.updateReassignment = updateChange;
        [v, first, second, time].forEach(function (control) { control.addEventListener('change', updateChange); });
        cancelMode.addEventListener('click', function () { mode.value = 'false'; updateChange(); });
        var supplement = document.getElementById('d03-supplement');
        if (supplement) supplement.addEventListener('click', function () {
            mode.value = 'true'; v.value = original(v);
            changeForm.parentNode.querySelector('.resource-status').textContent = '補派司機：請選擇司機及生效時間，可同時換車；至少需選擇司機 1。';
            updateChange(); first.focus(); changeForm.scrollIntoView({ block: 'center', behavior: 'smooth' });
        });
        updateChange();
    }
}());
