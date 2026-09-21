(function ($, ui) {
    'use strict';
    $(function () {
        var master, newItems = [], detailItems = [], detail;
        function show(name) { ['list', 'new', 'detail'].forEach(function (x) { ui.show('apply-' + x, name === x); }); kendo.resize($('.content')); ui.layout(); }
        function refreshCargo() { ui.grid('aa-items', newItems); }
        function refreshDetailCargo() { ui.grid('ad-items', detailItems); }
        async function query() {
            var rows = await ui.request('ReadApplications', { Applicant: ui.value('aq-applicant'), BranchId: ui.value('aq-branch'), StationId: ui.value('aq-station'), Status: ui.value('aq-status'), Mode: ui.value('aq-mode') });
            ui.grid('aq-grid', rows); $('#aq-count').text(rows.length + ' 筆');
        }
        function renderDetail(row) {
            detail = row; var a = row.Application; detailItems = a.Items.map(function (x) { return Object.assign({}, x); }); var editable = a.Status === 'unscheduled';
            $('#detail-title').text('收貨申請明細 · ' + row.AppNo); $('#detail-status').text(row.StatusLabel);
            $('#detail-basic').html(ui.fields([['單號', row.AppNo], ['申請人', row.Applicant], ['分公司據點', row.BranchName], ['收貨地點（起）', row.Pickup], ['送貨地點（迄）', row.Destination], ['收貨模式', row.ModeLabel], ['排班日期', row.ServiceDate], ['期望收貨時間', a.ExpectedTime], ['上貨 / 下貨時間', a.LoadMin + ' 分 / ' + a.UnloadMin + ' 分（合計 ' + a.HandleMin + ' 分）'], ['建立時間', row.CreatedAt]]));
            $('#detail-recipient').text(ui.recipient(a));
            $('#detail-cargo-title').text('貨物項目（總體積約 ' + a.Items.reduce(function (s, i) { return s + i.LengthCm * i.WidthCm * i.HeightCm / 1000 * i.Qty; }, 0).toFixed(0) + 'L）');
            ['ad-add', 'ad-rematch', 'detail-edit-hint'].forEach(function (id) { ui.show(id, editable); });
            var grid = $('#ad-items').data('kendoGrid'); if (editable) grid.showColumn(0); else grid.hideColumn(0);
            if (editable) $('#detail-result').html('<div class="callout warn"><b>未排入 — 請改期</b><br>' + ui.escape(a.Note) + '</div>');
            else $('#detail-result').html(ui.fields([['排定班次', row.ShiftLabel], ['車號', row.VehicleId + '（' + row.VehicleName + '）'], ['預計到站時間', row.Arrival], ['與期望時間差', a.ExpectDiffMin == null ? '—（未指定期望）' : a.ExpectDiffMin === 0 ? '準時' : '較期望時間' + (a.ExpectDiffMin > 0 ? '晚' : '早') + ' ' + Math.abs(a.ExpectDiffMin) + ' 分（僅提示）'], ['異常回報', a.Incident || '無']]));
            show('detail'); refreshDetailCargo();
        }
        function fill(id, values) { var w = ui.widget(id); w.setOptions({ dataTextField: 'Text', dataValueField: 'Value' }); w.setDataSource(new kendo.data.DataSource({ data: values })); w.select(0); }
        function buildings(stationId, buildingId, otherId) {
            var values = master.Buildings.filter(function (b) { return b.StationId === ui.value(stationId); }).map(function (b) { return { Text: b.Name, Value: b.Name }; }); values.push({ Text: '其他', Value: '__other' });
            fill(buildingId, values); ui.show(otherId, false);
        }
        function branchChanged() {
            var values = master.Stations.filter(function (s) { return s.BranchId === ui.value('aa-branch'); }).map(function (s) { return { Text: s.Name, Value: s.Id }; });
            fill('aa-pickuploc', values); fill('aa-station', values); buildings('aa-pickuploc', 'aa-pickbldg', 'aa-pickother'); buildings('aa-station', 'aa-building', 'aa-destother'); ui.layout();
        }
        function buildingValue(id, other) { return ui.value(id) === '__other' ? ui.value(other) : ui.value(id); }
        $('#aq-search').on('click', function () { ui.run(this, query); });
        $('#aq-new').on('click', function () { if (!master) return; newItems = []; show('new'); refreshCargo(); });
        $('#aa-cancel,#ad-back').on('click', function () { show('list'); ui.run(this, query); });
        $('#aq-demo').on('click', function () { ui.run(this, async function () { await ui.request('SeedDemo', {}, true); await query(); ui.notify('已載入 3 筆收貨申請（送出即自動媒合）'); }); });
        ui.command('aq-grid', function (_, row, index, button) { ui.run(button, async function () { renderDetail(await ui.request('GetApplication', { id: row.Id })); }); });
        $('#aa-add').on('click', function () { ui.openCargo(null, function (i) { newItems.push(i); refreshCargo(); }); });
        $('#ad-add').on('click', function () { ui.openCargo(null, function (i) { detailItems.push(i); refreshDetailCargo(); }); });
        ui.cargoCommands('aa-items', function () { return newItems; }, refreshCargo);
        ui.cargoCommands('ad-items', function () { return detailItems; }, refreshDetailCargo, function () { return detail && detail.Status === 'unscheduled'; });
        $('[name=aa-recv]').on('change', function () { var exact = $('[name=aa-recv]:checked').val() === 'exact'; ui.show('aa-deliver-wrap', exact); $('.radio-pill').each(function () { $(this).toggleClass('sel', $(this).find('input').prop('checked')); }); ui.layout(); });
        $('#aa-submit').on('click', function () {
            ui.run(this, async function () {
                if (!newItems.length) throw new Error('請至少新增一項貨物。');
                if (!await ui.confirm('確認送出收貨申請？送出後系統將立即自動媒合並告知班次時間與車號。')) return;
                var input = { Applicant: ui.value('aa-applicant'), BranchId: ui.value('aa-branch'), PickStationId: ui.value('aa-pickuploc'), DropStationId: ui.value('aa-station'), PickBuilding: buildingValue('aa-pickbldg', 'aa-pickother'), DropBuilding: buildingValue('aa-building', 'aa-destother'), RecvMode: $('[name=aa-recv]:checked').val(), ServiceDate: ui.date('aa-date'), ExpectedTime: ui.time('aa-deliver'), LoadMin: ui.value('aa-load'), UnloadMin: ui.value('aa-unload'), RecipientUnit: ui.value('aa-runit'), RecipientName: ui.value('aa-rname'), RecipientPhone: ui.value('aa-rphone'), AgentName: ui.value('aa-aname'), AgentPhone: ui.value('aa-aphone'), Items: newItems };
                var row = await ui.request('Submit', input, true); newItems = []; renderDetail(row); ui.notify(row.AppNo + ' ' + row.StatusLabel);
            });
        });
        $('#ad-rematch').on('click', function () { ui.run(this, async function () { if (await ui.confirm('確認重新媒合？將儲存目前貨物內容並重新執行自動媒合。')) { renderDetail(await ui.request('Rematch', { id: detail.Id, items: detailItems }, true)); ui.notify(detail.AppNo + ' ' + detail.StatusLabel); } }); });
        ui.run(null, async function () {
            master = await ui.request('Read', {}, false, true); branchChanged();
            ui.widget('aa-branch').bind('change', branchChanged);
            [['aa-pickuploc', 'aa-pickbldg', 'aa-pickother'], ['aa-station', 'aa-building', 'aa-destother']].forEach(function (ids) {
                ui.widget(ids[0]).bind('change', function () { buildings.apply(null, ids); ui.layout(); });
                ui.widget(ids[1]).bind('change', function () { ui.show(ids[2], ui.value(ids[1]) === '__other'); ui.layout(); });
            });
            await query();
        });
    });
})(jQuery, vd);
