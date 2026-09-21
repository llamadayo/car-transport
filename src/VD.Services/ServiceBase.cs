using System;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using VD.Core;

namespace VD.Services
{
    public sealed class DatabaseSettings
    {
        public string Provider { get; }
        public string ConnectionString { get; }
        public bool IsOracle => Provider == "Oracle";
        public DatabaseSettings(string provider, string connectionString)
        {
            if (provider != "Oracle" && provider != "SQLite") throw new ArgumentException("VD.Provider 必須是 Oracle 或 SQLite。");
            Provider = provider; ConnectionString = connectionString;
        }
        public static DatabaseSettings FromConfiguration() => new DatabaseSettings(
            ConfigurationManager.AppSettings["VD.Provider"] ?? "SQLite",
            ConfigurationManager.ConnectionStrings["VD"].ConnectionString);
    }
    // Service-local connection helpers only. No CRUD abstraction or Repository layer.
    public abstract class ServiceBase
    {
        protected readonly DatabaseSettings Settings;
        protected ServiceBase(DatabaseSettings settings) { Settings = settings; DefaultTypeMap.MatchNamesWithUnderscores = true; }
        protected IDbConnection OpenConnection()
        {
            IDbConnection connection = Settings.IsOracle
                ? (IDbConnection)new OracleConnection(Settings.ConnectionString)
                : new SQLiteConnection(Settings.ConnectionString);
            try
            {
                connection.Open();
                if (!Settings.IsOracle) connection.Execute("PRAGMA foreign_keys = ON;");
                return connection;
            }
            catch { connection.Dispose(); throw; }
        }
        protected IDbTransaction BeginWrite(IDbConnection connection) => connection.BeginTransaction(
            Settings.IsOracle ? IsolationLevel.ReadCommitted : IsolationLevel.Serializable);
        // Serialize Oracle writers per branch before taking the scheduling snapshot.
        // SQLite's write transaction serializes writers at database level.
        protected void LockBranch(IDbConnection c, IDbTransaction tx, string branchId)
        {
            if(Settings.IsOracle) c.QuerySingle<string>("SELECT ID FROM VD_SITE WHERE ID=:Id FOR UPDATE",new {Id=branchId},tx);
        }
        protected object DateValue(string value) => string.IsNullOrEmpty(value) ? null : (Settings.IsOracle ? (object)BusinessTime.ParseDate(value) : value);
        protected object TimestampValue(DateTime value) => Settings.IsOracle ? (object)value : value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        protected string DateColumn(string column, string alias) => (Settings.IsOracle ? "TO_CHAR(" + column + ", 'YYYY-MM-DD')" : column) + " AS " + alias;
        protected string TimestampColumn(string column, string alias) => (Settings.IsOracle ? "TO_CHAR(" + column + ", 'YYYY-MM-DD\"T\"HH24:MI:SS')" : column) + " AS " + alias;
    }
}
