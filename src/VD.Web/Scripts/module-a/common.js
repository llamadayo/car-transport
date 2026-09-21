/* UI state and Ajax only. Business rules and database operations live in C# Services. */
(function ($, kendo) {
    'use strict';
    var ui = window.vd = {};
    ui.escape = function (s) { return kendo.htmlEncode(s == null ? '' : String(s)); };
    ui.widget = function (id) { var el = $('#' + id); return el.data('kendoDropDownList') || el.data('kendoDatePicker') || el.data('kendoTimePicker') || el.data('kendoNumericTextBox'); };
    ui.value = function (id, value) { var w = ui.widget(id); if (arguments.length > 1) { if (w) w.value(value); else $('#' + id).val(value); } return w ? w.value() : $('#' + id).val(); };
    ui.date = function (id) { var d = ui.value(id); return d instanceof Date && !isNaN(d) ? kendo.toString(d, 'yyyy-MM-dd') : ''; };
    ui.time = function (id) { var d = ui.value(id); return d instanceof Date && !isNaN(d) ? kendo.toString(d, 'HH:mm') : ''; };
    ui.show = function (id, visible) { $('#' + id).prop('hidden', !visible); };
    ui.fields = function (pairs) { return '<div class="fgrid"><div class="grid-sizer"></div>' + pairs.map(function (p) { return '<div class="grid-item"><div class="fcard"><span class="fcard-label">' + ui.escape(p[0]) + '</span> <span class="fcard-value is-text">' + ui.escape(p[1] || '—') + '</span></div></div>'; }).join('') + '</div>'; };
    ui.recipient = function (a) { return [a.RecipientUnit, a.RecipientName, a.RecipientPhone, a.AgentName ? '代理人：' + a.AgentName + ' ' + (a.AgentPhone || '') : ''].filter(Boolean).join('\n'); };
    ui.layout = function () { requestAnimationFrame(function () { $('.fgrid:visible').each(function () { var m = Masonry.data(this); if (m) { m.reloadItems(); m.layout(); } else new Masonry(this, { itemSelector: '.grid-item', columnWidth: '.grid-sizer', percentPosition: true, gutter: 0, transitionDuration: '0.2s' }); }); }); };
    ui.notify = function (message) { var n = $('<div class="toast ok">').text(message).appendTo('#toast-wrap'); setTimeout(function () { n.remove(); }, 6500); };
    ui.run = async function (button, work) {
        if (button && $(button).prop('disabled')) return;
        $(button).prop('disabled', true); ui.show('page-error', false);
        try { return await work(); }
        catch (e) { $('#page-error').text(e.message || '處理失敗，請稍後重試。'); ui.show('page-error', true); document.getElementById('page-error').scrollIntoView({ block: 'nearest' }); }
        finally { $(button).prop('disabled', false); }
    };
    // MVC 5's form binder expects Items[0].Name, not jQuery's default Items[0][Name].
    function flatten(value, prefix, result) {
        if (Array.isArray(value)) value.forEach(function (v, i) { flatten(v, prefix + '[' + i + ']', result); });
        else if (value && typeof value === 'object') Object.keys(value).forEach(function (key) { flatten(value[key], prefix ? prefix + '.' + key : key, result); });
        else result[prefix] = value == null ? '' : value;
        return result;
    }
    ui.request = async function (action, data, post, master) {
        var values = flatten(data || {}, '', {});
        if (post) values.__RequestVerificationToken = $('#request-token input').val();
        try {
            var r = await $.ajax({ url: (master ? vdConfig.masterUrl : vdConfig.moduleUrl) + action, type: post ? 'POST' : 'GET', data: values, dataType: 'json', cache: false });
            if (!r.success) throw new Error(r.message); return r.data;
        } catch (e) { throw new Error(e.responseJSON && e.responseJSON.message || e.message || '連線失敗，請確認伺服器狀態後再試。'); }
    };
    ui.grid = function (id, rows) { var grid = $('#' + id).data('kendoGrid'); grid.dataSource.data(rows); grid.dataSource.pageSize(Math.max(1, rows.length)); grid.resize(); };
    ui.command = function (id, callback) { $('#' + id).on('click', '[data-command]', function (event) { event.preventDefault(); var grid = $('#' + id).data('kendoGrid'); var row = grid.dataItem($(this).closest('tr')); callback(this.dataset.command, row.toJSON(), grid.dataSource.indexOf(row), this); }); };
    ui.confirm = function (message) {
        return new Promise(function (resolve) {
            var win = $('#confirm-window').data('kendoWindow'), done = false;
            function finish(answer) { if (done) return; done = true; win.unbind('close', close); win.close(); resolve(answer); }
            function close() { finish(false); }
            $('#confirm-message').text(message); $('#confirm-ok').off('click').on('click', function () { finish(true); }); $('#confirm-cancel').off('click').on('click', close);
            win.bind('close', close); win.center().open();
        });
    };
    ui.openCargo = function (item, save) {
        var a = item || { Name: '', LengthCm: 50, WidthCm: 40, HeightCm: 30, Category: 'BOX', Qty: 1, Weight: 10 };
        var map = { name: 'Name', length: 'LengthCm', width: 'WidthCm', height: 'HeightCm', category: 'Category', qty: 'Qty', weight: 'Weight' };
        Object.keys(map).forEach(function (key) { ui.value('cargo-' + key, a[map[key]]); });
        var win = $('#cargo-window').data('kendoWindow'); ui.show('cargo-error', false);
        $('#cargo-save').off('click').on('click', function () {
            var next = {}; Object.keys(map).forEach(function (key) { next[map[key]] = ui.value('cargo-' + key); });
            if (!next.Name.trim() || !next.Category || ![next.LengthCm, next.WidthCm, next.HeightCm, next.Qty].every(function (v) { return typeof v === 'number' && isFinite(v) && v > 0; }) || next.Weight == null || next.Weight < 0) { $('#cargo-error').text('請填寫品名、分類、正數尺寸／件數及非負重量。'); ui.show('cargo-error', true); return; }
            save(next); win.close();
        });
        $('#cargo-cancel').off('click').on('click', function () { win.close(); }); win.center().open(); ui.layout();
    };
    ui.cargoCommands = function (gridId, getItems, changed, editable) {
        ui.command(gridId, function (command, row, index, button) {
            if (editable && !editable()) return;
            if (command === 'cargo-remove') ui.run(button, async function () { if (await ui.confirm('確認刪除此貨物項目？')) { getItems().splice(index, 1); changed(); } });
            else ui.openCargo(getItems()[index], function (item) { getItems()[index] = item; changed(); });
        });
    };
    ui.dynamicGrid = function (element, rows, columns) { $(element).kendoGrid({ dataSource: { data: rows }, columns: columns, scrollable: false }); };
    $(function () {
        if (!kendo || kendo.version !== '2019.1.115' || $.fn.jquery !== '3.3.1') { $('#page-error').text('Kendo／jQuery 版本不符，請依建置文件重新匯入公司指定套件。'); ui.show('page-error', true); }
        $('#menu-toggle').on('click', function () { $('body').toggleClass('nav-collapsed'); setTimeout(ui.layout, 250); });
        function tick() { $('#clock').text(new Date().toLocaleString('zh-TW', { timeZone: 'Asia/Taipei', hour12: false })); } tick(); setInterval(tick, 1000);
        ui.layout(); $(window).on('resize', ui.layout);
    });
})(window.jQuery, window.kendo);
