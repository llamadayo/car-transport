using System;
using System.IO;
using Dapper;
namespace VD.Services
{
    public sealed class DatabaseSetupService : ServiceBase
    {
        public DatabaseSetupService(DatabaseSettings settings) : base(settings) { }
        public void InitializeSqlite(string scriptDirectory)
        {
            if(Settings.IsOracle) throw new InvalidOperationException("Oracle 必須由 DBA 執行 database/oracle 的 SQL，不在網站啟動時建表。");
            using(var c=OpenConnection()) using(var tx=BeginWrite(c))
            {
                try
                {
                    // Check under the write lock, so simultaneous IIS workers cannot both create tables.
                    var exists=c.QuerySingle<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='VD_SCHEMA_VERSION'",transaction:tx);
                    if(exists>0)
                    {
                        if(c.QuerySingle<int>("SELECT VERSION_NO FROM VD_SCHEMA_VERSION",transaction:tx)!=1) throw new InvalidOperationException("資料庫版本不相符。");
                    }
                    else
                    {
                        c.Execute(File.ReadAllText(Path.Combine(scriptDirectory,"001_schema.sql")), transaction:tx);
                        c.Execute(File.ReadAllText(Path.Combine(scriptDirectory,"002_seed.sql")), transaction:tx);
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }
}
