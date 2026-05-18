using Org.BouncyCastle.Tls;
using System;
using System.Windows.Forms;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace GXUploader
{
    public partial class PageDatabase : UserControl
    {
        public PageDatabase()
        {
            InitializeComponent();

            // Initial load
            try { LoadFromDb(); }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load config:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            btnSave.Click += (s, e) => SaveToDb();
        }

        private void LoadFromDb()
        {
            var cfg = DbConfigRepository.Load();
            txtServer.Text = cfg.Server;
            txtDb.Text = cfg.DatabaseName;
            txtUser.Text = cfg.Username;
            txtPass.Text = cfg.Password;
            txtPort.Text = cfg.Port.ToString();
        }

        private void SaveToDb()
        {
            try
            {
                if (!int.TryParse(txtPort.Text.Trim(), out int port))
                {
                    MessageBox.Show("Port must be a number.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var cfg = new DbConfig
                {
                    Server = txtServer.Text.Trim(),
                    DatabaseName = txtDb.Text.Trim(),
                    Username = txtUser.Text.Trim(),
                    Password = txtPass.Text,
                    Port = port
                };

                DbConfigRepository.Save(cfg);

                MessageBox.Show("Saved successfully!", "Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed:\n" + ex.Message, "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}