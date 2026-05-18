// PageConfiguration.cs

using System;
using System.IO;
using System.Windows.Forms;

namespace GXUploader
{
    public partial class PageConfiguration : UserControl
    {
        private readonly string settingsFile =
            Path.Combine(Application.StartupPath, "toggle_settings.txt");

        public PageConfiguration()
        {
            InitializeComponent();

            LoadToggleState();

            //toggleEnableUpload.CheckedChanged += ToggleEnableUpload_CheckedChanged;
        }

        private void ToggleEnableUpload_CheckedChanged(object sender, EventArgs e)
        {
            SaveToggleState();
        }

        private void SaveToggleState()
        {
            try
            {
                //File.WriteAllText(settingsFile, toggleEnableUpload.Checked.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving toggle state: " + ex.Message);
            }
        }

        private void LoadToggleState()
        {
            try
            {
                if (File.Exists(settingsFile))
                {
                    string value = File.ReadAllText(settingsFile);

                    bool isChecked;
                    if (bool.TryParse(value, out isChecked))
                    {
                        //toggleEnableUpload.Checked = isChecked;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading toggle state: " + ex.Message);
            }
        }

        private void toggleEnableUpload_CheckedChanged_1(object sender, EventArgs e)
        {

        }
    }
}