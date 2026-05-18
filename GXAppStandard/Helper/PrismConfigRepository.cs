using System;
using System.IO;

namespace GXUploader.Helpers
{
    public static class PrismConfigRepository
    {
        public class PrismConfig
        {
            public string Host { get; set; } = "";
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public int Port { get; set; } = 3307;

            // UI may show this, but repository controls its value
            public string Workstation { get; set; } = "";

            // 0 = UPC, 1 = ALU
            public int ScanType { get; set; } = 0;
        }

        private static readonly string ConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prism_config.txt");

        private static string BuildWorkstation(string host, int port)
        {
            host = host?.Trim() ?? "";
            return $"{host}:{port}";
        }

        public static PrismConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return new PrismConfig();

                string[] lines = File.ReadAllLines(ConfigPath);

                PrismConfig cfg = new PrismConfig();

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split('=');

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
                            int.TryParse(value, out int port);
                            cfg.Port = port;
                            break;

                        case "scantype":
                            int.TryParse(value, out int scanType);
                            cfg.ScanType = scanType;
                            break;
                    }
                }

                cfg.Workstation = BuildWorkstation(cfg.Host, cfg.Port);

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
                string workstation = BuildWorkstation(cfg.Host, cfg.Port);

                string[] lines =
                {
                    $"Host={cfg.Host}",
                    $"Username={cfg.Username}",
                    $"Password={cfg.Password}",
                    $"Port={cfg.Port}",
                    $"Workstation={workstation}",
                    $"ScanType={cfg.ScanType}"
                };

                File.WriteAllLines(ConfigPath, lines);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to save configuration file. " + ex.Message);
            }
        }
    }
}