(function ($, ui) {
    'use strict';
    $(function () {
        var trip;
        async function query() { ui.grid('dispatch-grid', await ui.request('ReadTrips', { ServiceDate: ui.date('dq-date'), ShiftId: ui.value('dq-shift'), VehicleId: ui.value('dq-vehicle'), DriverId: ui.value('dq-driver') })); }
        function render(row) { trip = row; ui.show('dispatch-list', false); ui.show('dispatch-detail', true); $('#trip-title').text('車次明細 · ' + row.BranchName + '｜' + row.ServiceDate + '｜' + row.ShiftLabel); $('#trip-info').html(ui.fields([['分公司據點', row.BranchName], ['班次', row.ShiftLabel], ['收貨日期', row.ServiceDate]])); ui.value('trip-vehicle', row.VehicleId); ui.value('trip-driver', row.DriverId); $('#trip-count').text('本車次申請單（' + row.Count + ' 筆）'); ui.grid('trip-apps', row.Applications); ui.layout(); }
        async function refresh() { render(await ui.request('GetTrip', { date: trip.ServiceDate, shiftId: trip.ShiftId })); }
        $('#dq-search').on('click', function () { ui.run(this, query); });
        ui.command('dispatch-grid', function (_, row, index, button) { ui.run(button, async function () { render(await ui.request('GetTrip', { date: row.ServiceDate, shiftId: row.ShiftId })); }); });
        $('#trip-back').on('click', function () { ui.show('dispatch-list', true); ui.show('dispatch-detail', false); ui.run(this, query); ui.layout(); });
        $('#trip-save').on('click', function () { ui.run(this, async function () { render(await ui.request('SaveTripPlan', { date: trip.ServiceDate, shiftId: trip.ShiftId, vehicleId: ui.value('trip-vehicle'), driverId: ui.value('trip-driver') }, true)); ui.notify('車輛／司機已更新。'); }); });
        ui.command('trip-apps', function (_, row, index, button) { ui.run(button, async function () { if (!await ui.confirm('確認移出本班次？此單將回到未排入狀態，待業務重新指定。')) return; await ui.request('RemoveFromShift', { id: row.Id }, true); await refresh(); ui.notify(row.AppNo + ' 已移出本班次。'); }); });
        $('#trip-add').on('click', function () { ui.run(this, async function () { var candidates = await ui.request('ReadAssignableApplications', { date: trip.ServiceDate, shiftId: trip.ShiftId }); $('#assign-window').data('kendoWindow').center().open(); ui.grid('assign-grid', candidates); }); });
        ui.command('assign-grid', function (_, row, index, button) { ui.run(button, async function () { await ui.request('ReassignShift', { id: row.Id, date: trip.ServiceDate, shiftId: trip.ShiftId }, true); $('#assign-window').data('kendoWindow').close(); await refresh(); ui.notify(row.AppNo + ' 已加入本班次。'); }); });
        ui.run(null, query);
    });
})(jQuery, vd);
