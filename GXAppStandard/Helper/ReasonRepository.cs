using Oracle.ManagedDataAccess.Client;
using System;
using System.Configuration;

namespace GXUploader.Helpers
{
    public static class ReasonRepository
    {
        // Oracle connection string
        private static readonly string ConnString =
            ConfigurationManager.AppSettings["OracleConnection"];

        private static string _cachedUploaderSid;

        /// <summary>
        /// Returns the SID from pref_reason where name = 'Upload'
        /// </summary>
        public static string GetUploaderReasonSid()
        {
            if (!string.IsNullOrWhiteSpace(_cachedUploaderSid))
                return _cachedUploaderSid;

            using var conn = new OracleConnection(ConnString);
            conn.Open();

            const string sql = @"
                SELECT sid
                FROM rps.pref_reason
                WHERE name = :name
                FETCH FIRST 1 ROWS ONLY";

            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("name", "Upload"));

            var result = cmd.ExecuteScalar();
            _cachedUploaderSid = result?.ToString();

            return _cachedUploaderSid;
        }
    }
}