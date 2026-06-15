using GXUploader.Helper;
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
            string licenseKeyPath = Path.Combine(basePath, "license_key.json");

            // 🔥 1. LICENSE CHECK FIRST
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

            // 🔥 2. ONLY RUN TAMPER CHECK IF LICENSE EXISTS
            if (IsTimeTampered(basePath))
            {
                return; // STOP APP
            }

            Application.Run(new MainForm());
        }

        private static bool IsTimeTampered(string basePath)
        {
            string file = Path.Combine(basePath, "last_run.json");

            DateTime now = DateTime.UtcNow;

            try
            {
                if (File.Exists(file))
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        File.ReadAllText(file));

                    DateTime lastRun = DateTime.Parse(data["LastRun"]);

                    if (now < lastRun)
                    {
                        MessageBox.Show(
                            "System time tampering detected!\nPlease fix your system date.",
                            "Security Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        return true;
                    }
                }

                var save = new Dictionary<string, string>
                {
                    { "LastRun", now.ToString("o") }
                };

                File.WriteAllText(file,
                    JsonSerializer.Serialize(save, new JsonSerializerOptions { WriteIndented = true }));

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}