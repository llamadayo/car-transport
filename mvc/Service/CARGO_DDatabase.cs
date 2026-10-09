#if !LOCAL_DEMO
using System;
using System.Configuration;
using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace CARGO.Service
{
    internal static class CARGO_DDatabase
    {
        public static OracleConnection Open()
        {
            var setting = ConfigurationManager.ConnectionStrings["CARGO"];
            if (setting == null || String.IsNullOrWhiteSpace(setting.ConnectionString))
                throw new InvalidOperationException("尚未設定 CARGO Oracle 連線，請由公司設定機制提供。");
            var connection = new OracleConnection(setting.ConnectionString);
            try { connection.Open(); return connection; }
            catch { connection.Dispose(); throw; }
        }
        public static OracleParameters Params(object values) { return new OracleParameters(values); }
    }
    // Dapper 1.50.5 with ODP.NET: bind by name, including repeated placeholders.
    internal sealed class OracleParameters : SqlMapper.IDynamicParameters
    {
        private readonly DynamicParameters values;
        public OracleParameters(object input) { values = new DynamicParameters(input); }
        public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            ((OracleCommand)command).BindByName = true;
            ((SqlMapper.IDynamicParameters)values).AddParameters(command, identity);
        }
    }
}
#endif
