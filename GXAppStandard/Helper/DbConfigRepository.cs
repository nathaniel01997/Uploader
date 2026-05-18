using System;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;

namespace GXUploader
{
    public class DbConfig
    {
        public string Server { get; set; } = "";
        public string DatabaseName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int Port { get; set; } = 1521;
    }

    public static class DbConfigRepository
    {
        private static readonly string AdminConnString =
            "Server=localhost;Database=prism_uploader;Uid=root;Pwd=;Port=3307;";

        public static DbConfig Load()
        {
            const string sql = @"
                SELECT db_host, db_username, db_password, db_port, db_name
                FROM database_configuration
                ORDER BY id ASC
                LIMIT 1;";

            using (var con = new MySqlConnection(AdminConnString))
            using (var cmd = new MySqlCommand(sql, con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return new DbConfig();

                    return new DbConfig
                    {
                        Server = r["db_host"]?.ToString() ?? "",
                        DatabaseName = r["db_name"]?.ToString() ?? "",
                        Username = r["db_username"]?.ToString() ?? "",
                        Password = r["db_password"]?.ToString() ?? "",
                        Port = Convert.ToInt32(r["db_port"])
                    };
                }
            }
        }

        public static void Save(DbConfig cfg)
        {
            using (var con = new MySqlConnection(AdminConnString))
            {
                con.Open();

                const string sqlSelect = @"
                    SELECT id, db_host, db_username, db_password, db_port, db_name
                    FROM database_configuration
                    ORDER BY id ASC
                    LIMIT 1;";

                int? id = null;
                string host = "", user = "", pass = "", db = "";
                int port = 0;

                using (var cmd = new MySqlCommand(sqlSelect, con))
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        id = Convert.ToInt32(r["id"]);
                        host = r["db_host"]?.ToString() ?? "";
                        user = r["db_username"]?.ToString() ?? "";
                        pass = r["db_password"]?.ToString() ?? "";
                        port = Convert.ToInt32(r["db_port"]);
                        db = r["db_name"]?.ToString() ?? "";
                    }
                }

                if (id == null)
                {
                    const string sqlInsert = @"
                        INSERT INTO database_configuration
                            (db_host, db_username, db_password, db_port, db_name, created_datetime, modified_datetime)
                        VALUES
                            (@host, @user, @pass, @port, @db, NOW(), NOW());";

                    using (var cmd = new MySqlCommand(sqlInsert, con))
                    {
                        cmd.Parameters.AddWithValue("@host", cfg.Server);
                        cmd.Parameters.AddWithValue("@user", cfg.Username);
                        cmd.Parameters.AddWithValue("@pass", cfg.Password);
                        cmd.Parameters.AddWithValue("@port", cfg.Port);
                        cmd.Parameters.AddWithValue("@db", cfg.DatabaseName);
                        cmd.ExecuteNonQuery();
                    }

                    return;
                }

                bool isSame =
                    host == cfg.Server &&
                    user == cfg.Username &&
                    pass == cfg.Password &&
                    port == cfg.Port &&
                    db == cfg.DatabaseName;

                string sqlUpdate;

                if (isSame)
                {
                    sqlUpdate = @"
                        UPDATE database_configuration
                        SET modified_datetime = NOW()
                        WHERE id = @id;";
                }
                else
                {
                    sqlUpdate = @"
                        UPDATE database_configuration
                        SET db_host = @host,
                            db_username = @user,
                            db_password = @pass,
                            db_port = @port,
                            db_name = @db,
                            modified_datetime = NOW()
                        WHERE id = @id;";
                }

                using (var cmd = new MySqlCommand(sqlUpdate, con))
                {
                    cmd.Parameters.AddWithValue("@id", id.Value);

                    if (!isSame)
                    {
                        cmd.Parameters.AddWithValue("@host", cfg.Server);
                        cmd.Parameters.AddWithValue("@user", cfg.Username);
                        cmd.Parameters.AddWithValue("@pass", cfg.Password);
                        cmd.Parameters.AddWithValue("@port", cfg.Port);
                        cmd.Parameters.AddWithValue("@db", cfg.DatabaseName);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }


        //for 19c
        //public static string BuildOracleConnectionString(DbConfig cfg)
        //{
        //    return
        //        $"User Id={cfg.Username};" +
        //        $"Password={cfg.Password};" +
        //        $"Data Source={cfg.Server}:{cfg.Port}/{cfg.DatabaseName};" +
        //        $"Min Pool Size=5;" +
        //        $"Max Pool Size=100;" +
        //        $"Connection Timeout=60;" +
        //        $"Validate Connection=true;";
        //}

        public static string BuildOracleConnectionString(DbConfig cfg)
        {
            return
                $"User Id={cfg.Username};" +
                $"Password={cfg.Password};" +
                $"Data Source={cfg.Server}:{cfg.Port}/{cfg.DatabaseName};";
                //$"Min Pool Size=5;" +
                //$"Max Pool Size=100;" +
                //$"Connection Timeout=60;" +
                //$"Validate Connection=true;";
        }

        public static void TestTargetConnection(DbConfig cfg)
        {
            var cs = BuildOracleConnectionString(cfg);

            using (var con = new OracleConnection(cs))
            {
                con.Open();
            }
        }
    }
}