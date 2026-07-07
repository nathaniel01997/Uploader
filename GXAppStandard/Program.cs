using GXUploader.Helper;
using GXUploader.Model.Inventory;
using System.Text.Json;

namespace GXUploader
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            string basePath = LicensingPath.BasePath;
            string licenseKeyPath = Path.Combine(basePath, "license_logs.json");
            string lastRunPath = Path.Combine(basePath, "last_run.json");

            // 1. LICENSE CHECK
            if (!File.Exists(licenseKeyPath))
            {
                using (LicenseForm lf = new LicenseForm())
                {
                    lf.ShowDialog();
                }

                if (!File.Exists(licenseKeyPath))
                {
                    MessageBox.Show("License is required to continue.");
                    return;
                }
            }

            // 2. LOAD LICENSE + CHECK EXPIRY
            if (IsLicenseExpired(licenseKeyPath))
            {
                MessageBox.Show(
                    "License expired.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // 3. ANTI-TAMPER CHECK (TIME ROLLBACK ONLY)
            if (IsTimeTampered(lastRunPath))
            {
                return;
            }

            // 4. RUN APP
            Application.Run(new MainForm());
        }

        // -----------------------------
        // LICENSE EXPIRY CHECK
        // -----------------------------
        private static bool IsLicenseExpired(string licenseKeyPath)
        {
            try
            {
                string json = File.ReadAllText(licenseKeyPath);

                var licenses = JsonSerializer.Deserialize<List<LicenseLog>>(json);

                if (licenses == null || licenses.Count == 0)
                    return true;

                var license = licenses[0];

                // Direct DateTime comparison (NO parsing needed)
                return DateTime.UtcNow > license.Expiry;
            }
            catch
            {
                return true; // fail-safe: block app
            }
        }

        // -----------------------------
        // ANTI-TAMPER CHECK
        // (detect system clock rollback)
        // -----------------------------
        private static bool IsTimeTampered(string filePath)
        {
            DateTime now = DateTime.Now;

            try
            {
                // First run after activation
                if (!File.Exists(filePath))
                {
                    var save = new Dictionary<string, string>
                {
                    { "LastRun", now.ToString("o") }
                };

                    File.WriteAllText(
                        filePath,
                        JsonSerializer.Serialize(save, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }));

                    return false;
                }

                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(filePath));

                if (data == null || !data.ContainsKey("LastRun"))
                    return false;

                DateTime activationTime = DateTime.Parse(data["LastRun"]);

                // User rolled back system clock before activation date
                if (now < activationTime)
                {
                    MessageBox.Show(
                        "System time tampering detected.\nThe system date is earlier than the activation date.",
                        "Security Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return true;
                }

                // DO NOT UPDATE THE FILE
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}