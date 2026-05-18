using System;
using System.IO;

namespace GXUploader.Helpers
{
    public static class PrismConfigRepository
    {
        public class PrismConfig
        {
            // MySQL
            public string Host { get; set; } = "";
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public int Port { get; set; } = 8080;

            // Auto generated
            public string Workstation { get; set; } = "";

            // 0 = UPC, 1 = ALU
            public int ScanType { get; set; } = 0;

        }

        private static readonly string ConfigPath =
            Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\prism_config.txt")
            );

        private static string BuildWorkstation(string host, int port)
        {
            host = host?.Trim() ?? "";
            return $"{host}:{port}";
        }

        public static PrismConfig Load()
        {
            try
            {
                PrismConfig cfg = new PrismConfig();

                if (!File.Exists(ConfigPath))
                    return cfg;

                string[] lines = File.ReadAllLines(ConfigPath);

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split(new char[] { '=' }, 2);

                    if (parts.Length < 2)
                        continue;

                    string key = parts[0].Trim().ToLower();
                    string value = parts[1].Trim();

                    switch (key)
                    {
                        case "host":
                            cfg.Host = value;
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

                        case "scantype":
                            if (int.TryParse(value, out int scanType))
                                cfg.ScanType = scanType;
                            break;
                    }
                }

                cfg.Workstation =
                    BuildWorkstation(cfg.Host, cfg.Port);

                return cfg;
            }
            catch
            {
                return new PrismConfig();
            }
        }

        public static void Save(PrismConfig cfg)
        {
            try
            {
                string workstation =
                    BuildWorkstation(cfg.Host, cfg.Port);

                string[] lines =
                {
                    // MySQL
                    $"Host={cfg.Host}",
                    $"Username={cfg.Username}",
                    $"Password={cfg.Password}",
                    $"Port={cfg.Port}",
                    $"Workstation={workstation}",
                    $"ScanType={cfg.ScanType}",
                };

                File.WriteAllLines(ConfigPath, lines);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Failed to save configuration file. " +
                    ex.Message
                );
            }
        }

        public static string GetMySqlConnectionString()
        {
            PrismConfig cfg = Load();

            return
                $"Server={cfg.Host};" +
                $"Uid={cfg.Username};" +
                $"Pwd={cfg.Password};" +
                $"Port={cfg.Port};";
        }
    }
}