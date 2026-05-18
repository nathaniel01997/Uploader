using System;
using System.IO;
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
        private static readonly string ConfigPath =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..\\..\\..\\dbconfig.txt"
            );

        // LOAD FROM TXT
        public static DbConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return new DbConfig();

                var cfg = new DbConfig();
                string[] lines = File.ReadAllLines(ConfigPath);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0].Trim().ToLower();
                    string value = parts[1].Trim();

                    switch (key)
                    {
                        case "server":
                            cfg.Server = value;
                            break;

                        case "databasename":
                            cfg.DatabaseName = value;
                            break;

                        case "username":
                            cfg.Username = value;
                            break;

                        case "password":
                            cfg.Password = value;
                            break;

                        case "port":
                            if (int.TryParse(value, out int port))
                                cfg.Port = port;
                            break;
                    }
                }

                return cfg;
            }
            catch
            {
                return new DbConfig();
            }
        }

        // SAVE TO TXT
        public static void Save(DbConfig cfg)
        {
            string[] lines =
            {
                $"Server={cfg.Server}",
                $"DatabaseName={cfg.DatabaseName}",
                $"Username={cfg.Username}",
                $"Password={cfg.Password}",
                $"Port={cfg.Port}"
            };

            File.WriteAllLines(ConfigPath, lines);
        }

        // ORACLE CONNECTION STRING
        public static string BuildOracleConnectionString(DbConfig cfg)
        {
            return
                $"User Id={cfg.Username};" +
                $"Password={cfg.Password};" +
                $"Data Source={cfg.Server}:{cfg.Port}/{cfg.DatabaseName};";
        }

        // TEST CONNECTION
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