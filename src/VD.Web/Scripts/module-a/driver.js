(function ($, ui) {
    'use strict';
    $(function () { ui.run(null, async function () {
        var tasks = await ui.request('ReadDriverTasks');
        if (!tasks.length) $('<div class="card"><div class="empty">尚無已排定的班次任務。使用者送出申請並自動媒合成功後，這裡會依日期＋班次顯示任務單。</div></div>').appendTo('#driver-cards');
        tasks.forEach(function (task) {
            var card = $('<div class="card">').appendTo('#driver-cards'); var title = $('<div class="card-title split">').appendTo(card);
            $('<span>').text('🚚 ' + task.BranchName + '｜' + task.ServiceDate + '｜' + task.ShiftLabel + '｜車 ' + task.VehicleId + '（' + task.VehicleName + '）').appendTo(title); $('<span class="badge b-navy">').text('駕駛：' + task.DriverName).appendTo(title);
            $('<div class="card-desc">').text('沿分公司固定 9 站路線一次通過，於 ' + task.Stops.length + ' 個停靠站依序卸貨／取貨；本班 ' + task.Count + ' 筆、總貨量約 ' + task.RawVolume.toFixed(0) + 'L。').appendTo(card);
            ui.dynamicGrid($('<div>').appendTo(card), task.Stops, [{ title: '順序', template: function (s) { return task.Stops.findIndex(function (x) { return x.SortOrder === s.SortOrder; }) + 1; } }, { field: 'Name', title: '停靠站' }, { field: 'Arrival', title: '抵達' }, { title: '作業（卸貨／取貨）', template: function (s) {
                return s.Drops.map(function (r) { return '<div><span class="badge b-amber">卸貨</span> ' + ui.escape(r.AppNo + '｜' + r.Application.DropBuilding + '｜接收：' + ui.recipient(r.Application)) + (r.Status === 'delivered' ? ' <span class="badge b-green">已交貨</span>' : '') + '</div>'; }).join('') + s.Picks.map(function (r) { return '<div><span class="badge b-navy">取貨</span> ' + ui.escape(r.AppNo + '｜' + r.Pickup + '｜' + r.CargoSummary) + '</div>'; }).join('');
            } }]);
        });
    }); });
})(jQuery, vd);
