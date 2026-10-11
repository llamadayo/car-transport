(function () {
    'use strict';
    document.querySelectorAll('.workflow-action').forEach(function (form) {
        form.noValidate = true;
        var clicked;
        form.querySelectorAll('button[type=submit]').forEach(function (button) { button.addEventListener('click', function () { clicked = button; }); });
        form.addEventListener('submit', function (event) {
            event.preventDefault();
            var button = event.submitter || clicked;
            var command = button && button.name === 'Command' ? button.value : '';
            var hiddenCommand = form.querySelector('input[name=Command]');
            command = command || (hiddenCommand ? hiddenCommand.value : '');
            var required = {};
            if (command === 'reject') required.Note = '退回時請填寫審核意見';
            if (command === 'dispatch' || command === 'reassign') {
                required.Vehicle = '請選擇車輛';
                var driver1 = form.querySelector('[name=Driver1]'), driver2 = form.querySelector('[name=Driver2]');
                if ((driver2 && driver2.value) || (command === 'reassign' && form.getAttribute('data-self-drive') === 'false')) required.Driver1 = '請先選擇司機 1';
                if (command === 'reassign') required.EffectiveAt = '請填寫生效時間';
            }
            if (command === 'extend') required.EndAt = '請填寫新的結束時間';
            if (!window.cargoValidateForm(form, required)) return;
            var message = command === 'noVehicle' ? '確認無車退回？此操作不送運輸主管簽審。' : form.getAttribute('data-confirm');
            window.cargoConfirm(message, function () {
                if (command && !hiddenCommand) { var hidden = document.createElement('input'); hidden.type = 'hidden'; hidden.name = 'Command'; hidden.value = command; form.appendChild(hidden); }
                HTMLFormElement.prototype.submit.call(form);
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
