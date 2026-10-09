(function () {
    'use strict';
    document.querySelectorAll('.workflow-action').forEach(function (form) {
        var clicked;
        form.querySelectorAll('button[type=submit]').forEach(function (button) { button.addEventListener('click', function () { clicked = button; }); });
        form.addEventListener('submit', function (event) {
            event.preventDefault();
            var button = event.submitter || clicked;
            var command = button && button.name === 'Command' ? button.value : '';
            var note = form.querySelector('[name=Note]');
            if (command === 'reject' && note && !note.value.trim()) { note.setCustomValidity('退回時請填寫審核意見'); note.reportValidity(); note.addEventListener('input', function () { note.setCustomValidity(''); }, { once: true }); return; }
            var message = command === 'noVehicle' ? '確認無車退回？此操作不送運輸主管簽審。' : form.getAttribute('data-confirm');
            window.cargoConfirm(message, function () {
                if (command) { var hidden = document.createElement('input'); hidden.type = 'hidden'; hidden.name = 'Command'; hidden.value = command; form.appendChild(hidden); }
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
