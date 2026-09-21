using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using VD.Core;

namespace VD.Services
{
    public sealed class MasterDataService : ServiceBase
    {
        public MasterDataService(DatabaseSettings settings) : base(settings) { }
        public MasterSnapshot Read()
        {
            using (var c = OpenConnection()) return Read(c, null);
        }
        internal MasterSnapshot Read(IDbConnection c, IDbTransaction tx)
        {
            var configs = c.Query("SELECT KEY_NAME, NUM_VALUE FROM VD_CONFIG", transaction: tx)
                .ToDictionary(x => (string)x.KEY_NAME, x => Convert.ToDouble(x.NUM_VALUE));
            return new MasterSnapshot {
                Sites = c.Query<Site>("SELECT ID, NAME, SORT_ORDER FROM VD_SITE ORDER BY SORT_ORDER", transaction: tx).ToList(),
                Stations = c.Query<Station>("SELECT s.ID, s.BRANCH_ID, s.NAME, s.SORT_ORDER FROM VD_STATION s JOIN VD_SITE b ON b.ID=s.BRANCH_ID ORDER BY b.SORT_ORDER, s.SORT_ORDER", transaction: tx).ToList(),
                Buildings = c.Query<Building>("SELECT ID, SITE_ID, STATION_ID, NAME FROM VD_BUILDING ORDER BY ID", transaction: tx).ToList(),
                Vehicles = c.Query<Vehicle>("SELECT * FROM VD_VEHICLE ORDER BY SORT_ORDER", transaction: tx).ToList(),
                Drivers = c.Query<Driver>("SELECT * FROM VD_DRIVER ORDER BY SORT_ORDER", transaction: tx).ToList(),
                Shifts = c.Query<Shift>("SELECT * FROM VD_REG_SHIFT ORDER BY SORT_ORDER", transaction: tx).ToList(),
                Categories = WasteFactorProvider.Read(Settings.Provider + Settings.ConnectionString,
                    () => c.Query<CargoCategory>("SELECT CODE, NAME, FACTOR, ACTIVE, SORT_ORDER FROM VD_CARGO_CATEGORY ORDER BY SORT_ORDER", transaction: tx).ToList()),
                Config = configs
            };
        }
        public void RefreshFactors() { WasteFactorProvider.Invalidate(Settings.Provider + Settings.ConnectionString); Read(); }
        public object ReadSharedTables()
        {
            using (var c = OpenConnection()) return new {
                Travel = c.Query("SELECT * FROM VD_TRAVEL_TIME ORDER BY SIZE_CLASS, FROM_ID, TO_ID").ToList(),
                Breaks = c.Query("SELECT * FROM VD_DRIVER_BREAK ORDER BY AFTER_DRIVE_MIN").ToList(),
                MinDays = c.Query("SELECT * FROM VD_MIN_TRIP_DAYS ORDER BY SIZE_CLASS, SITE_ID").ToList(),
                Maintenance = c.Query("SELECT ID, VEHICLE_ID, " + DateColumn("FROM_DATE", "FROM_DATE") + ", " + DateColumn("TO_DATE", "TO_DATE") + ", REASON FROM VD_MAINTENANCE").ToList(),
                Leaves = c.Query("SELECT ID, DRIVER_ID, " + DateColumn("LEAVE_DATE", "LEAVE_DATE") + ", FROM_MINUTE, TO_MINUTE, LEAVE_TYPE FROM VD_DRIVER_LEAVE").ToList(),
                BizTravel = c.Query("SELECT * FROM VD_BIZ_TRAVEL").ToList()
            };
        }
    }
    public static class WasteFactorProvider
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, List<CargoCategory>> Cache = new Dictionary<string, List<CargoCategory>>();
        public static List<CargoCategory> Read(string key, Func<List<CargoCategory>> load)
        {
            lock (Gate)
            {
                if (!Cache.ContainsKey(key)) Cache[key] = load();
                return Cache[key].Select(x => new CargoCategory { Code=x.Code, Name=x.Name, Factor=x.Factor, Active=x.Active, SortOrder=x.SortOrder }).ToList();
            }
        }
        public static void Invalidate(string key) { lock (Gate) Cache.Remove(key); }
    }
}
