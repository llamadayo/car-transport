(function () {
    'use strict';
    document.querySelectorAll('.workflow-action').forEach(function (form) {
        form.noValidate = true;
        var clicked;
        form.querySelectorAll('button[type=submit]').forEach(function (button) { button.addEventListener('click', function () { clicked = button; }); });
        form.addEventListener('submit', function (event) {
            event.preventDefault();
            if (form.resourcesReady === false) return;
            var button = event.submitter || clicked;
            var command = button && button.name === 'Command' ? button.value : '';
            var hiddenCommand = form.querySelector('input[type=hidden][name=Command]');
            var selectedCommand = form.querySelector('input[type=radio][name=Command]:checked');
            command = command || (selectedCommand ? selectedCommand.value : '') || (hiddenCommand ? hiddenCommand.value : '');
            if (command === 'dispatch' && form.dispatchReady === false) return;
            if (command === 'reassign' && form.reassignReady === false) return;
            var required = {};
            if (command === 'reject') required.Note = '退回時請填寫審核意見';
            if (command === 'dispatch' || command === 'reassign') {
                required.Vehicle = '請選擇車輛';
                var driver1 = form.querySelector('[name=Driver1]'), driver2 = form.querySelector('[name=Driver2]');
                if ((driver2 && driver2.value) || (command === 'reassign' && form.getAttribute('data-self-drive') === 'false')) required.Driver1 = '請先選擇司機 1';
                if (command === 'reassign') {
                    required.EffectiveAt = '請填寫生效時間';
                    var supplement = form.querySelector('[name=SupplementDriver]');
                    if (supplement && supplement.value === 'true') required.Driver1 = '補派司機至少需選擇司機 1';
                }
            }
            if (command === 'extend') required.EndAt = '請填寫新的結束時間';
            if (!window.cargoValidateForm(form, required)) return;
            var message = command === 'noVehicle' ? '確認無車退回？此操作不送運輸主管簽審。' : form.getAttribute('data-confirm');
            window.cargoConfirm(message, function () {
                if (command && !hiddenCommand && !selectedCommand) { var hidden = document.createElement('input'); hidden.type = 'hidden'; hidden.name = 'Command'; hidden.value = command; form.appendChild(hidden); }
                HTMLFormElement.prototype.submit.call(form);
            });
        });
    });
    document.querySelectorAll('[data-resource-url]').forEach(function (form) {
        var url = form.getAttribute('data-resource-url');
        if (!url) return;
        var time = form.querySelector('[name=EffectiveAt]');
        var status = form.parentNode.querySelector('.resource-status');
        var selects = ['Vehicle', 'Driver1', 'Driver2'].map(function (name) { return form.querySelector('[name=' + name + ']'); });
        var buttons = form.querySelectorAll('button[type=submit]');
        var generation = 0;
        time.addEventListener('change', function () {
            var current = ++generation;
            form.resourcesReady = false;
            buttons.forEach(function (b) { b.disabled = true; });
            status.textContent = '正在查詢剩餘時段可用資源…';
            fetch(url + (url.indexOf('?') >= 0 ? '&' : '?') + 'effectiveAt=' + encodeURIComponent(time.value), { credentials: 'same-origin' })
                .then(function (response) { if (!response.ok) throw new Error(); return response.json(); })
                .then(function (resources) {
                    if (current !== generation) return;
                    selects.forEach(function (select) {
                        var chosen = select.value, original = select.getAttribute('data-original');
                        var kind = select.name === 'Vehicle' ? 'VEHICLE' : 'DRIVER';
                        select.textContent = '';
                        var empty = document.createElement('option'); empty.value = ''; empty.textContent = '— 請選擇 —'; select.appendChild(empty);
                        resources.filter(function (r) { return r.Kind === kind; }).forEach(function (r) {
                            var option = document.createElement('option'); option.value = r.Id;
                            option.disabled = !!r.Reason && r.Id !== original;
                            option.textContent = r.Label + (r.Reason && r.Id !== original ? ' · ⚠ ' + r.Reason : '');
                            select.appendChild(option);
                        });
                        var keep = Array.prototype.some.call(select.options, function (o) { return o.value === chosen && !o.disabled; });
                        select.value = keep ? chosen : '';
                    });
                    form.resourcesReady = true;
                    status.textContent = '已依生效時間更新可用資源。';
                    buttons.forEach(function (b) { b.disabled = false; });
                    if (form.updateReassignment) form.updateReassignment();
                }).catch(function () {
                    if (current !== generation) return;
                    status.textContent = '無法更新資源，請確認生效時間在目前指派區間內，再修改時間重試。';
                });
        });
    });
    jQuery(function () {
        var grid = jQuery('#workGrid').data('kendoGrid');
        if (!grid) return;
        function search(event) { if (event) event.preventDefault(); jQuery('#grid-error').hide(); grid.dataSource.page(1); }
        jQuery('#search').on('click', search); jQuery('#query-form').on('submit', search);
        jQuery('#batch').on('click', function () {
            var rows = grid.dataSource.view().filter(function (x) { return x.CanApprove; });
            if (!rows.length) { jQuery('#grid-error').text('本頁沒有待審資料。').prop('hidden', false).show(); return; }
            window.cargoConfirm('確認核准本頁 ' + rows.length + ' 筆待審申請？', function () {
                var target = document.getElementById('batch-items'); target.textContent = '';
                rows.forEach(function (row, i) { ['RequestId','Version'].forEach(function (key) {
                    var input = document.createElement('input'); input.type = 'hidden'; input.name = 'items[' + i + '].' + key; input.value = row[key]; target.appendChild(input);
                }); });
                HTMLFormElement.prototype.submit.call(document.getElementById('batch-form'));
            });
        });
    });
}());
