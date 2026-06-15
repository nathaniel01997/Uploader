using GXUploader.Helper;
using GXUploader.Helpers;
using GXUploader.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace GXUploader
{
    public partial class LicenseForm : Form
    {
        private TextBox txtLicenseKey;
        private Button btnActivate;

        public LicenseForm()
        {
            InitializeComponent();

            _ = LicensingPath.BasePath;

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

            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show(
                    "Enter a license key.",
                    "Activation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                string logPath = Path.Combine(
                    LicensingPath.BasePath,
                    "license_logs.json");

                if (!File.Exists(logPath))
                {
                    MessageBox.Show(
                        "License database not found.",
                        "Activation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                var licenses = JsonSerializer.Deserialize<List<LicenseLog>>(
                    File.ReadAllText(logPath));

                if (licenses == null || !licenses.Any())
                {
                    MessageBox.Show(
                        "Invalid license database.",
                        "Activation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                var match = licenses.FirstOrDefault(x =>
                    string.Equals(
                        x.LicenseKey,
                        key,
                        StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    MessageBox.Show(
                        "Invalid license key.",
                        "Activation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                DateTime today = DateTime.UtcNow.Date;
                DateTime expiry = match.Expiry.Date;

                if (expiry < today)
                {
                    MessageBox.Show(
                        "This license has already expired.",
                        "Activation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                string savePath = Path.Combine(
                    LicensingPath.BasePath,
                    "license_key.json");

                var data = new
                {
                    LicenseKey = key,
                    ActivatedDate = DateTime.UtcNow
                };

                File.WriteAllText(
                    savePath,
                    JsonSerializer.Serialize(
                        data,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }));

                int daysLeft = (expiry - today).Days;

                if (daysLeft <= 7)
                {
                    MessageBox.Show(
                        $"License activated successfully.\n\nWarning: License will expire in {daysLeft} day(s).",
                        "License Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        $"License activated successfully.\n\nDays remaining: {daysLeft}",
                        "Activation Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Activation error:\n\n{ex.Message}",
                    "Activation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}