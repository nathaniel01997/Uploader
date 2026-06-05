using GXUploader.Model;
using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace GXUploader
{
    public partial class LicenseForm : Form
    {
        private TextBox txtLicenseKey;
        private Button btnActivate;

        private string basePath =
            @"C:\Users\JohnDave\Desktop\LicenseGenerator\LicenseGenerator\bin\Release\net8.0\win-x64";

        public LicenseForm()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "License Activation";
            this.Width = 350;
            this.Height = 180;
            this.StartPosition = FormStartPosition.CenterScreen;

            txtLicenseKey = new TextBox()
            {
                Top = 30,
                Left = 50,
                Width = 230
            };

            btnActivate = new Button()
            {
                Text = "Activate",
                Top = 70,
                Left = 120,
                Width = 100
            };

            btnActivate.Click += BtnActivate_Click;

            this.Controls.Add(txtLicenseKey);
            this.Controls.Add(btnActivate);
        }

        private void BtnActivate_Click(object sender, EventArgs e)
        {
            string key = txtLicenseKey.Text.Trim();

            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("Enter license key.");
                return;
            }

            try
            {
                string logPath = Path.Combine(basePath, "license_logs.json");

                if (!File.Exists(logPath))
                {
                    MessageBox.Show("License database not found.");
                    return;
                }

                // 🔥 READ LICENSE DATABASE
                var licenses = JsonSerializer.Deserialize<List<LicenseLog>>(
                    File.ReadAllText(logPath));

                if (licenses == null || licenses.Count == 0)
                {
                    MessageBox.Show("Invalid license database.");
                    return;
                }

                // 🔥 CHECK IF KEY EXISTS
                var match = licenses.FirstOrDefault(x => x.LicenseKey == key);

                if (match == null)
                {
                    MessageBox.Show(
                        "Invalid license key.",
                        "Activation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                // 🔥 CHECK EXPIRY BEFORE SAVING
                DateTime now = DateTime.UtcNow.Date;
                DateTime expiry = match.Expiry.Date;

                if (expiry == now)
                {
                    MessageBox.Show(
                        "This license is already expired.",
                        "Activation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                // ✔ SAVE LICENSE KEY (VALID ONLY)
                string savePath = Path.Combine(basePath, "license_key.json");

                var data = new
                {
                    LicenseKey = key,
                    ActivatedDate = DateTime.UtcNow
                };

                File.WriteAllText(savePath,
                    JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));

                // ⚠ WARNING MESSAGE (OPTIONAL)
                int daysLeft = (expiry - now).Days;

                if (daysLeft >= 7)
                {
                    MessageBox.Show(
                        "License activated but will expire in " + daysLeft + " day(s).",
                        "Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("License activated successfully!");
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Activation error: " + ex.Message);
            }
        }
    }
}