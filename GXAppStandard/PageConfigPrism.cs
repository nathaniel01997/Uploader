using System;
using System.Windows.Forms;
using GXUploader.Helpers;
using static GXUploader.Helpers.PrismConfigRepository;

namespace GXUploader
{
    public partial class PageConfigPrism : UserControl
    {
        public PageConfigPrism()
        {
            InitializeComponent();

            txtHost.TextChanged += (s, e) => UpdateWorkstation();
            txtPort.TextChanged += (s, e) => UpdateWorkstation();

            btnSave.Click += BtnSave_Click;
            btnReload.Click += BtnReload_Click;

            LoadFromDb();
        }

        private void UpdateWorkstation()
        {
            string host = txtHost.Text.Trim();
            string port = txtPort.Text.Trim();

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(port))
            {
                txtWs.Text = "";
                return;
            }

            txtWs.Text = $"{host}:{port}";
        }

        private void LoadFromDb()
        {
            try
            {
                var cfg = PrismConfigRepository.Load();

                txtHost.Text = cfg.Host;
                txtUser.Text = cfg.Username;
                txtPass.Text = cfg.Password;
                txtPort.Text = cfg.Port.ToString();

                // Set radio selection
                if (cfg.ScanType == 0)
                    rbUPC.Checked = true;
                else
                    rbALU.Checked = true;

                UpdateWorkstation();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load Prism config:\n" + ex.Message,
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            LoadFromDb();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(txtPort.Text.Trim(), out int port))
                {
                    MessageBox.Show("Port must be a number.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int scanType = rbUPC.Checked ? 0 : 1;

                var cfg = new PrismConfig
                {
                    Host = txtHost.Text.Trim(),
                    Username = txtUser.Text.Trim(),
                    Password = txtPass.Text,
                    Port = port,
                    ScanType = scanType
                };

                PrismConfigRepository.Save(cfg);

                MessageBox.Show("Saved successfully!", "Prism Configuration",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed:\n" + ex.Message,
                    "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}