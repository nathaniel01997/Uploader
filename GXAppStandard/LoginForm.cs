using GXUploader.Helper;
using GXUploader.Model;
using GXUploader.Model.Inventory;
using System;
using System.Text.Json;
using System.Windows.Forms;

namespace GXUploader
{
    public partial class LoginForm : Form
    {
        public bool IsAuthenticated { get; private set; } = false;

        public string LoggedInUsername { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            var creds = CredentialManager.LoadCredentials();
            
            if (username == creds.Username &&
                password == creds.Password)
            {
                if (!ValidateLicense())
                    return;
                IsAuthenticated = true;

                LoggedInUsername = creds.Username;

                MessageBox.Show(
                    "Welcome " + creds.Username + "!",
                    "Login Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.Close();
            }
            else
            {
                MessageBox.Show(
                    "Invalid username or password.",
                    "Login Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            IsAuthenticated = false;
            this.Close();
        }

        // OPTIONAL BUTTON FOR CHANGING USERNAME/PASSWORD
        private void btnUpdateCredentials_Click(object sender, EventArgs e)
        {
            var newCreds = new UserCredential
            {
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text.Trim()
            };

            CredentialManager.SaveCredentials(newCreds);

            MessageBox.Show(
                "Credentials updated successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private bool ValidateLicense()
        {
            try
            {
                string keyPath = Path.Combine(LicensingPath.BasePath, "license_key.json");
                string logPath = Path.Combine(LicensingPath.BasePath, "license_logs.json");

                if (!File.Exists(keyPath))
                {
                    MessageBox.Show(
                        "License key file not found.",
                        "License Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return false;
                }

                if (!File.Exists(logPath))
                {
                    MessageBox.Show(
                        "License log file not found.",
                        "License Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return false;
                }

                var keyData = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(keyPath));

                if (keyData == null ||
                    !keyData.TryGetValue("LicenseKey", out string inputKey))
                {
                    MessageBox.Show(
                        "Invalid license key file.",
                        "License Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return false;
                }

                var licenses = JsonSerializer.Deserialize<List<LicenseLog>>(
                    File.ReadAllText(logPath));

                if (licenses == null || !licenses.Any())
                {
                    MessageBox.Show(
                        "No license records found.",
                        "License Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return false;
                }

                var match = licenses.FirstOrDefault(x => x.LicenseKey == inputKey);

                if (match == null)
                {
                    MessageBox.Show(
                        "Invalid license key.",
                        "License Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return false;
                }

                DateTime today = DateTime.UtcNow.Date;
                DateTime expiry = match.Expiry.Date;

                int daysLeft = (expiry - today).Days;

                if (daysLeft < 0)
                {
                    MessageBox.Show(
                        $"Your license expired {Math.Abs(daysLeft)} day(s) ago.",
                        "License Expired",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return false;
                }

                if (daysLeft <= 7)
                {
                    MessageBox.Show(
                        $"Your license will expire in {daysLeft} day(s).",
                        "License Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"License validation failed.\n\n{ex.Message}",
                    "License Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }
    }
}