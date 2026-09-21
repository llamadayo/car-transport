(function ($, ui) {
    'use strict';
    $(function () {
        var data;
        function render() {
            if (!data) return; var rows = data[ui.value('master-table')];
            if (!Array.isArray(rows)) rows = Object.keys(rows || {}).map(function (key) { return { Key: key, Value: rows[key] }; });
            var old = $('#master-grid').data('kendoGrid'); if (old) old.destroy(); $('#master-grid').empty();
            var cols = rows.length ? Object.keys(rows[0]).filter(function (k) { return typeof rows[0][k] !== 'object'; }).map(function (key) { return { field: key, title: key }; }) : [];
            $('#master-grid').kendoGrid({ dataSource: { data: rows, pageSize: 25 }, columns: cols, pageable: true, scrollable: true, sortable: true });
        }
        async function load() { var values = await Promise.all([ui.request('Read', {}, false, true), ui.request('ReadShared', {}, false, true)]); data = Object.assign({}, values[0], values[1]); render(); }
        ui.widget('master-table').bind('change', render);
        $('#master-refresh').on('click', function () { ui.run(this, async function () { ui.notify(await ui.request('RefreshFactors', {}, true, true)); await load(); }); }); ui.run(null, load);
    });
})(jQuery, vd);
