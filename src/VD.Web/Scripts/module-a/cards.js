(function ($, ui) {
    'use strict';
    $(function () {
        $('#m-relayout').on('click', ui.layout);
        ['m-mode', 'm-station', 'm-date', 'm-time'].forEach(function (id) { ui.widget(id).bind('change', ui.layout); });
        ui.run(null, async function () { var rows = await ui.request('ReadApplications'); if (!rows.length) { ui.value('m-mode', 'exact'); ui.layout(); return; } var r = rows[0], a = r.Application;
            $('#m-number,#m-id').text(r.AppNo); $('#m-applicant').text(r.Applicant); $('#m-building').text(a.DropBuilding); $('#m-pickup').text(r.Pickup); $('#m-handle').text(a.LoadMin + ' 分 / ' + a.UnloadMin + ' 分'); $('#m-shift').text(r.ShiftLabel); $('#m-recipient').text(ui.recipient(a)); $('#m-cargo').text(a.Items.map(function (i) { return i.Name + ' × ' + i.Qty; }).join('\n')); ui.value('m-mode', a.RecvMode); ui.value('m-station', a.DropStationId); ui.value('m-date', kendo.parseDate(a.ServiceDate, 'yyyy-MM-dd')); ui.value('m-time', kendo.parseDate(a.ExpectedTime || '14:00', 'HH:mm')); ui.layout();
        });
    });
})(jQuery, vd);
