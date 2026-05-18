using System;
using System.IO;
using System.Windows.Forms;

namespace GXUploader
{
    public partial class PageDatabase : UserControl
    {
        private static readonly string configPath =
            Path.GetFullPath(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    @"..\..\..\dbconfig.txt"
                )
            );

        public PageDatabase()
        {
            InitializeComponent();

            // Initial load
            try
            {
                LoadFromFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load config:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            btnSave.Click += (s, e) => SaveToFile();
        }

        private void LoadFromFile()
        {
            if (!File.Exists(configPath))
                return;

            string[] lines = File.ReadAllLines(configPath);

            foreach (string line in lines)
            {
                string[] parts = line.Split('=');

                if (parts.Length != 2)
                    continue;

                string key = parts[0];
                string value = parts[1];

                switch (key)
                {
                    case "Server":
                        txtServer.Text = value;
                        break;

                    case "Database":
                        txtDb.Text = value;
                        break;

                    case "Username":
                        txtUser.Text = value;
                        break;

                    case "Password":
                        txtPass.Text = value;
                        break;

                    case "Port":
                        txtPort.Text = value;
                        break;
                }
            }
        }

        private void SaveToFile()
        {
            try
            {
                if (!int.TryParse(txtPort.Text.Trim(), out int port))
                {
                    MessageBox.Show("Port must be a number.",
                        "Validation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                string[] configLines =
                {
                    "Server=" + txtServer.Text.Trim(),
                    "Database=" + txtDb.Text.Trim(),
                    "Username=" + txtUser.Text.Trim(),
                    "Password=" + txtPass.Text,
                    "Port=" + port
                };

                File.WriteAllLines(configPath, configLines);

                MessageBox.Show("Saved successfully!",
                    "Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed:\n" + ex.Message,
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}