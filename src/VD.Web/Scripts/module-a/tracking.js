(function ($, ui) {
    'use strict';
    $(function () {
        var incident;
        async function refresh() {
            var rows = await ui.request('ReadApplications'), assigned = rows.filter(function (r) { return r.Status === 'matched' || r.Status === 'delivered'; }), unscheduled = rows.filter(function (r) { return r.Status === 'unscheduled'; });
            ui.grid('scheduled-grid', assigned); ui.grid('unscheduled-grid', unscheduled); ui.show('unscheduled-card', !!unscheduled.length);
            ui.grid('incident-grid', assigned.map(function (r) { r.Application.Incident = r.Application.Incident || '正常運送'; return r; }));
        }
        ui.command('incident-grid', function (_, row) { incident = row; $('#incident-summary').text(row.AppNo + '｜' + row.Applicant + '｜' + row.Destination); ui.value('incident-reason', row.Application.Incident === '正常運送' ? '' : row.Application.Incident); $('#incident-window').data('kendoWindow').center().open(); });
        $('#incident-cancel').on('click', function () { $('#incident-window').data('kendoWindow').close(); });
        $('#incident-save').on('click', function () { ui.run(this, async function () { var result = await ui.request('ReportIncident', { id: incident.Id, reason: ui.value('incident-reason') }, true); $('#incident-window').data('kendoWindow').close(); await refresh(); ui.notify(result.Message); }); });
        $('#tracking-tabs').data('kendoTabStrip').bind('activate', function () { kendo.resize($('.content')); ui.layout(); });
        ui.run(null, async function () {
            await refresh(); var routes = await ui.request('ReadRoutes');
            routes.forEach(function (branch) {
                var card = $('<div class="card">').appendTo('#route-cards'); $('<div class="card-title">').text(branch.Name + ' · 9 站固定路線').appendTo(card);
                $('<div class="card-desc">').text('固定地理順序（站點 100→900）、無貨跳過、不重排。同站先卸後裝、多單時間加總。').appendTo(card);
                var route = $('<div class="route">').appendTo(card);
                branch.Stations.forEach(function (station) { var stop = $('<div class="stop">').appendTo(route); $('<div class="s-name">').text(station.Name).appendTo(stop); $('<div class="s-meta">').text('建物 ' + station.Buildings[0] + '–' + station.Buildings[station.Buildings.length - 1]).appendTo(stop); });
                $('<div class="card-title">').text('今日班次 · 車輛對應').appendTo(card);
                ui.dynamicGrid($('<div>').appendTo(card), branch.Shifts, [{ field: 'Label', title: '班次' }, { field: 'Depart', title: '出發' }, { field: 'VehicleName', title: '車輛' }, { field: 'Volume', title: '容量 L', format: '{0:n0}' }, { field: 'WeightLimit', title: '重量上限 kg' }]);
            });
        });
    });
})(jQuery, vd);
