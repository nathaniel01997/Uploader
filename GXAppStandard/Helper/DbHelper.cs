using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace GXUploader
{
    public sealed class DbHelper
    {
        private readonly string _connString;

        public DbHelper(string connectionString)
        {
            _connString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private OracleConnection CreateConnection()
        {
            return new OracleConnection(_connString);
        }

        public DataTable QueryDataTable(string sql, params OracleParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL is required.", nameof(sql));

            using var conn = CreateConnection();
            using var cmd = new OracleCommand(sql, conn);

            cmd.BindByName = true;

            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            using var da = new OracleDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public int Execute(string sql, params OracleParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL is required.", nameof(sql));

            using var conn = CreateConnection();
            conn.Open();

            using var cmd = new OracleCommand(sql, conn);

            cmd.BindByName = true;

            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            return cmd.ExecuteNonQuery();
        }

        public object Scalar(string sql, params OracleParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL is required.", nameof(sql));

            using var conn = CreateConnection();
            conn.Open();

            using var cmd = new OracleCommand(sql, conn);

            cmd.BindByName = true;

            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            return cmd.ExecuteScalar();
        }

        public static OracleParameter P(string name, object value)
        {
            return new OracleParameter(name, value ?? DBNull.Value);
        }

        public static OracleParameter P(string name, OracleDbType dbType, object value)
        {
            return new OracleParameter(name, dbType)
            {
                Value = value ?? DBNull.Value
            };
        }
    }
}