using Oracle.ManagedDataAccess.Client;
using System;
using System.Configuration;
using System.Data;

namespace GXUploader
{
    public sealed class DbHelper
    {
        private readonly string _connString;
        private static string _cachedConnection;

        /// <summary>
        /// Automatically detects and uses the working Oracle connection.
        /// </summary>
        public DbHelper()
        {
            _connString = GetOracleConnection();
        }

        /// <summary>
        /// Allows manually specifying a connection string.
        /// If null or empty, it automatically detects the connection.
        /// </summary>
        public DbHelper(string connectionString)
        {
            _connString = string.IsNullOrWhiteSpace(connectionString)
                ? GetOracleConnection()
                : connectionString;
        }

        private OracleConnection CreateConnection()
        {
            return new OracleConnection(_connString);
        }

        /// <summary>
        /// Detects Oracle 19C or 12C automatically.
        /// The successful connection string is cached.
        /// </summary>
        public static string GetOracleConnection()
        {
            if (!string.IsNullOrWhiteSpace(_cachedConnection))
                return _cachedConnection;

            string[] keys =
            {
                "OracleConnection19C",
                "OracleConnection12C"
            };

            foreach (string key in keys)
            {
                string conn = ConfigurationManager.AppSettings[key];

                if (string.IsNullOrWhiteSpace(conn))
                    continue;

                try
                {
                    using (var connection = new OracleConnection(conn))
                    {
                        connection.Open();
                        _cachedConnection = conn;
                        return conn;
                    }
                }
                catch
                {
                    // Try the next configured connection
                }
            }

            throw new Exception(
                "Unable to connect using OracleConnection19C or OracleConnection12C. Please verify your App.config settings.");
        }

        public DataTable QueryDataTable(string sql, params OracleParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL is required.", nameof(sql));

            using (var conn = CreateConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;

                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                using (var da = new OracleDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public int Execute(string sql, params OracleParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL is required.", nameof(sql));

            using (var conn = CreateConnection())
            {
                conn.Open();

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public object Scalar(string sql, params OracleParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL is required.", nameof(sql));

            using (var conn = CreateConnection())
            {
                conn.Open();

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    return cmd.ExecuteScalar();
                }
            }
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