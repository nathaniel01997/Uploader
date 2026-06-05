using System.Text.Json;

namespace GXUploader
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            string basePath =
                @"C:\Users\JohnDave\Desktop\LicenseGenerator\LicenseGenerator\bin\Release\net8.0\win-x64";

            if (IsTimeTampered(basePath))
            {
                return; // STOP APP
            }

            string licenseKeyPath = Path.Combine(basePath, "license_key.json");

            // 🔥 2. LICENSE CHECK
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

                    // ❌ BACKDATE DETECTED
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

                // ✔ SAVE CURRENT TIME
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