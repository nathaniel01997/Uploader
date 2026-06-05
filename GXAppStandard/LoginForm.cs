using GXUploader.Helper;
using GXUploader.Model;
using System;
using System.Text.Json;
using System.Windows.Forms;

namespace GXUploader
{
    public partial class LoginForm : Form
    {
        public bool IsAuthenticated { get; private set; } = false;

        private string basePath =
            @"C:\Users\JohnDave\Desktop\LicenseGenerator\LicenseGenerator\bin\Release\net8.0\win-x64";

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
                string keyPath = Path.Combine(basePath, "license_key.json");
                string logPath = Path.Combine(basePath, "license_logs.json");

                if (!File.Exists(keyPath))
                {
                    MessageBox.Show("License not found.");
                    return false;
                }

                var keyData = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(keyPath));

                string inputKey = keyData["LicenseKey"];

                var licenses = JsonSerializer.Deserialize<List<LicenseLog>>(
                    File.ReadAllText(logPath));

                var match = licenses.FirstOrDefault(x => x.LicenseKey == inputKey);

                if (match == null)
                {
                    MessageBox.Show("Invalid license key.");
                    return false;
                }

                DateTime now = DateTime.UtcNow.Date;
                DateTime expiry = match.Expiry.Date;

                int daysLeft = (expiry - now).Days;

                if (daysLeft < 0)
                {
                    MessageBox.Show(
                    $"Your license was expired {daysLeft} day(s) ago.",
                    "License Expired",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                    return false;
                }

                if (daysLeft <= 7)
                {
                    MessageBox.Show(
                    $"Your license is about to expired {daysLeft} day(s) remaining.",
                    "License About to Expired",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("License error: " + ex.Message);
                return false;
            }
        }
    }
}