using System;
using System.Collections.Generic;
using System.Linq;
using VD.Core;

namespace VD.Services
{
    public sealed class LoadFeasibilityService
    {
        private readonly MasterSnapshot master;
        public LoadFeasibilityService(MasterSnapshot snapshot) { master = snapshot; }
        private double Factor(string code) => master.Categories.FirstOrDefault(c => c.Active == 1 && c.Code == code)?.Factor ?? master.DefaultFactor;
        public LoadValue Effective(IEnumerable<CargoItem> items)
        {
            var total = new LoadValue();
            foreach (var item in items)
            {
                var sorted = new[] { item.LengthCm, item.WidthCm, item.HeightCm }.OrderByDescending(n => n).ToArray();
                var aspect = sorted[0] / sorted[2];
                var penalty = aspect > 3 ? 1.10 : aspect > 2 ? 1.05 : 1.0;
                total.Volume += item.LengthCm * item.WidthCm * item.HeightCm / 1000 * Factor(item.Category) * penalty * item.Qty;
                total.Floor += sorted[1] * sorted[2] * item.Qty;
                total.Weight += item.Weight * item.Qty;
            }
            return total;
        }
        public static bool Fits(CargoItem i, Vehicle v)
        {
            var d = new[] { i.LengthCm, i.WidthCm, i.HeightCm };
            int[,] p = { {0,1,2},{0,2,1},{1,0,2},{1,2,0},{2,0,1},{2,1,0} };
            for (int x=0;x<6;x++) if (d[p[x,0]] <= v.LengthCm && d[p[x,1]] <= v.WidthCm && d[p[x,2]] <= v.HeightCm) return true;
            return false;
        }
        public LoadResult Check(IList<CargoItem> items, Vehicle vehicle, LoadValue start = null)
        {
            RegionalApplicationService.ValidateItems(items);
            start = start ?? new LoadValue();
            var add = Effective(items);
            var used = new LoadValue { Volume=start.Volume+add.Volume, Weight=start.Weight+add.Weight, Floor=start.Floor+add.Floor };
            var result = new LoadResult { Used=used };
            foreach (var i in items) result.Trace.Add(new TraceStep { Code="ITEM", Message=$"{i.Name} × {i.Qty}，類別係數 {Factor(i.Category)}" });
            Action<bool,string,string> check = (ok,code,message) => {
                var step = new TraceStep { Code=code, Message=(ok ? "✓ " : "✗ ")+message };
                result.Trace.Add(step); if (!ok) result.Reasons.Add(step);
            };
            check(used.Volume <= vehicle.Volume, "L1_VOLUME", $"有效體積 {used.Volume:0.###}L / {vehicle.Volume:0.###}L");
            check(used.Floor <= vehicle.LengthCm*vehicle.WidthCm, "FLOOR", $"地板投影 {used.Floor:0.###}cm² / {vehicle.LengthCm*vehicle.WidthCm:0.###}cm²");
            foreach(var i in items) check(Fits(i,vehicle), "L2_DIM", $"{i.Name} 六方向尺寸檢查");
            check(used.Weight <= vehicle.WeightLimit, "WEIGHT", $"重量 {used.Weight:0.###}kg / {vehicle.WeightLimit:0.###}kg");
            return result;
        }
    }
}
