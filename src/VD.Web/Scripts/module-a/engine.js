(function ($, ui) {
    'use strict';
    $(function () {
        var items = []; function refresh() { ui.grid('engine-items', items); }
        ui.cargoCommands('engine-items', function () { return items; }, refresh);
        $('#engine-add').on('click', function () { ui.openCargo(null, function (i) { items.push(i); refresh(); }); });
        $('#engine-check').on('click', function () { ui.run(this, async function () { var result = await ui.request('CheckLoad', { vehicleId: ui.value('engine-vehicle'), items: items }, true, true); var box = $('#engine-result').empty(); $('<h3>').text(result.Ok ? '可裝載' : '無法裝載').appendTo(box); $('<p>').text('有效體積 ' + result.Used.Volume.toFixed(3) + 'L；重量 ' + result.Used.Weight.toFixed(3) + 'kg；底面積 ' + result.Used.Floor.toFixed(3) + 'cm²').appendTo(box); result.Trace.forEach(function (t) { $('<div>').text(t.Code + ' · ' + t.Message).appendTo(box); }); }); }); refresh();
    });
})(jQuery, vd);
