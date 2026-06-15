// PageConfiguration.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace GXUploader
{
    public partial class PageConfiguration : UserControl
    {
        private readonly string settingsFile =
            Path.Combine(Application.StartupPath, "toggle_settings.txt");

        private Dictionary<string, Control> toggleControls;
        private bool isLoading = false;

        public PageConfiguration()
        {
            InitializeComponent();

            toggleControls = new Dictionary<string, Control>
            {
                { "UDFText", tgUDFText },
                { "UDFDate", tgUDFDate },
                { "Text", tgText },
                { "VendorName",tgVendorName },
                { "DCS", tgDCS }
            };

            // attach event AFTER dictionary setup
            tgUDFText.CheckedChanged += ToggleChanged;
            tgUDFDate.CheckedChanged += ToggleChanged;
            tgText.CheckedChanged += ToggleChanged;
            tgVendorName.CheckedChanged += ToggleChanged;
            tgDCS.CheckedChanged += ToggleChanged;
            LoadToggleState();
        }

        private void ToggleChanged(object sender, EventArgs e)
        {
            if (isLoading) return; // 🔥 prevents auto overwrite during load

            SaveToggleState();
        }

        private void SaveToggleState()
        {
            try
            {
                List<string> lines = new List<string>();

                foreach (var item in toggleControls)
                {
                    var control = item.Value;

                    bool value = GetToggleValue(control);
                    lines.Add($"{item.Key}={value}");
                }

                File.WriteAllLines(settingsFile, lines);
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
                isLoading = true;

                if (!File.Exists(settingsFile))
                    return;

                string[] lines = File.ReadAllLines(settingsFile);

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split('=');
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0];
                    bool value = bool.Parse(parts[1]);

                    if (toggleControls.ContainsKey(key))
                    {
                        SetToggleValue(toggleControls[key], value);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading toggle state: " + ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        // 🔥 SAFE GET VALUE (works even if NOT CheckBox)
        private bool GetToggleValue(Control control)
        {
            var prop = control.GetType().GetProperty("Checked");
            if (prop != null)
                return (bool)prop.GetValue(control);

            return false;
        }

        // 🔥 SAFE SET VALUE
        private void SetToggleValue(Control control, bool value)
        {
            var prop = control.GetType().GetProperty("Checked");
            if (prop != null)
                prop.SetValue(control, value);
        }

        private void tgUDFDate_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void tgUDFText_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void tgText_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void tgVendorName_CheckChanged(object sender, EventArgs e)
        {

        }

        private void tgDCS_CheckChanged(object sender, EventArgs e)
        {

        }

        public bool IsUDFTextEnabled()
        {
            var prop = tgUDFText.GetType().GetProperty("Checked");
            if (prop != null)
                return (bool)prop.GetValue(tgUDFText);

            return false;
        }

        public bool IsTextEnabled()
        {
            var prop = tgText.GetType().GetProperty("Checked");
            if (prop != null)
                return (bool)prop.GetValue(tgText);

            return false;
        }

        public bool IsUDFDateEnabled()
        {
            var prop = tgUDFDate.GetType().GetProperty("Checked");
            if (prop != null)
                return (bool)prop.GetValue(tgUDFDate);

            return false;
        }

        public bool IsVendorNameEnabled()
        {
            var prop = tgVendorName.GetType().GetProperty("Checked");
            if (prop != null)
                return (bool)prop.GetValue(tgVendorName);

            return false;
        }

        public bool IsDCSEnabled()
        {
            var prop = tgDCS.GetType().GetProperty("Checked");
            if (prop != null)
                return (bool)prop.GetValue(tgDCS);

            return false;
        }
    }
}