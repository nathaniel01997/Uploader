// PageInventory.Designer.cs (UPDATED - switch removed)

namespace GXUploader
{
    partial class PageInventory
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges9 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges10 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();

            pnlRoot = new Guna.UI2.WinForms.Guna2Panel();
            dgvInventory = new Guna.UI2.WinForms.Guna2DataGridView();
            btnReadCsv = new Guna.UI2.WinForms.Guna2Button();
            btnStartUploading = new Guna.UI2.WinForms.Guna2Button();
            btnBrowseCsv = new Guna.UI2.WinForms.Guna2Button();
            txtCsvPath = new Guna.UI2.WinForms.Guna2TextBox();
            lblTemp = new Guna.UI2.WinForms.Guna2HtmlLabel();
            lblLogs = new Guna.UI2.WinForms.Guna2HtmlLabel();
            txtLogs = new System.Windows.Forms.RichTextBox();

            pnlRoot.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInventory).BeginInit();
            SuspendLayout();

            // 
            // pnlRoot
            // 
            pnlRoot.BackColor = System.Drawing.SystemColors.Control;
            pnlRoot.Controls.Add(dgvInventory);
            pnlRoot.Controls.Add(btnReadCsv);
            pnlRoot.Controls.Add(btnStartUploading);
            pnlRoot.Controls.Add(btnBrowseCsv);
            pnlRoot.Controls.Add(txtCsvPath);
            pnlRoot.Controls.Add(lblTemp);
            pnlRoot.Controls.Add(lblLogs);
            pnlRoot.Controls.Add(txtLogs);
            pnlRoot.CustomizableEdges = customizableEdges9;
            pnlRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlRoot.FillColor = System.Drawing.SystemColors.Control;
            pnlRoot.Location = new System.Drawing.Point(0, 0);
            pnlRoot.Name = "pnlRoot";
            pnlRoot.ShadowDecoration.CustomizableEdges = customizableEdges10;
            pnlRoot.Size = new System.Drawing.Size(800, 480);
            pnlRoot.TabIndex = 0;

            // 
            // dgvInventory
            // 
            dgvInventory.AllowUserToAddRows = false;
            dgvInventory.AllowUserToDeleteRows = false;
            dgvInventory.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            dgvInventory.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            dgvInventory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(100, 88, 255);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            dgvInventory.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dgvInventory.ColumnHeadersHeight = 36;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(71, 69, 94);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(231, 229, 255);
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.FromArgb(71, 69, 94);
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dgvInventory.DefaultCellStyle = dataGridViewCellStyle3;
            dgvInventory.GridColor = System.Drawing.Color.FromArgb(231, 229, 255);
            dgvInventory.Location = new System.Drawing.Point(244, 76);
            dgvInventory.MultiSelect = false;
            dgvInventory.Name = "dgvInventory";
            dgvInventory.ReadOnly = true;
            dgvInventory.RowHeadersVisible = false;
            dgvInventory.RowTemplate.Height = 32;
            dgvInventory.Size = new System.Drawing.Size(536, 200);
            dgvInventory.TabIndex = 13;
            dgvInventory.ThemeStyle.AlternatingRowsStyle.BackColor = System.Drawing.Color.White;
            dgvInventory.ThemeStyle.AlternatingRowsStyle.Font = null;
            dgvInventory.ThemeStyle.AlternatingRowsStyle.ForeColor = System.Drawing.Color.Empty;
            dgvInventory.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = System.Drawing.Color.Empty;
            dgvInventory.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = System.Drawing.Color.Empty;
            dgvInventory.ThemeStyle.BackColor = System.Drawing.Color.White;
            dgvInventory.ThemeStyle.GridColor = System.Drawing.Color.FromArgb(231, 229, 255);
            dgvInventory.ThemeStyle.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(100, 88, 255);
            dgvInventory.ThemeStyle.HeaderStyle.BorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dgvInventory.ThemeStyle.HeaderStyle.Font = new System.Drawing.Font("Segoe UI", 9F);
            dgvInventory.ThemeStyle.HeaderStyle.ForeColor = System.Drawing.Color.White;
            dgvInventory.ThemeStyle.HeaderStyle.HeaightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvInventory.ThemeStyle.HeaderStyle.Height = 36;
            dgvInventory.ThemeStyle.ReadOnly = true;
            dgvInventory.ThemeStyle.RowsStyle.BackColor = System.Drawing.Color.White;
            dgvInventory.ThemeStyle.RowsStyle.BorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dgvInventory.ThemeStyle.RowsStyle.Font = new System.Drawing.Font("Segoe UI", 9F);
            dgvInventory.ThemeStyle.RowsStyle.ForeColor = System.Drawing.Color.FromArgb(71, 69, 94);
            dgvInventory.ThemeStyle.RowsStyle.Height = 32;
            dgvInventory.ThemeStyle.RowsStyle.SelectionBackColor = System.Drawing.Color.FromArgb(231, 229, 255);
            dgvInventory.ThemeStyle.RowsStyle.SelectionForeColor = System.Drawing.Color.FromArgb(71, 69, 94);

            // 
            // btnReadCsv
            // 
            btnReadCsv.BorderRadius = 8;
            btnReadCsv.CustomizableEdges = customizableEdges1;
            btnReadCsv.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnReadCsv.ForeColor = System.Drawing.Color.White;
            btnReadCsv.Location = new System.Drawing.Point(555, 34);
            btnReadCsv.Name = "btnReadCsv";
            btnReadCsv.ShadowDecoration.CustomizableEdges = customizableEdges2;
            btnReadCsv.Size = new System.Drawing.Size(110, 36);
            btnReadCsv.TabIndex = 12;
            btnReadCsv.Text = "Read CSV";
            btnReadCsv.Click += btnReadCsv_Click;

            // 
            // btnStartUploading
            // 
            btnStartUploading.BorderRadius = 8;
            btnStartUploading.CustomizableEdges = customizableEdges3;
            btnStartUploading.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnStartUploading.ForeColor = System.Drawing.Color.White;
            btnStartUploading.Location = new System.Drawing.Point(670, 34);
            btnStartUploading.Name = "btnStartUploading";
            btnStartUploading.ShadowDecoration.CustomizableEdges = customizableEdges4;
            btnStartUploading.Size = new System.Drawing.Size(110, 36);
            btnStartUploading.TabIndex = 13;
            btnStartUploading.Text = "Start Uploading";
            btnStartUploading.Click += btnStartUploading_Click;

            // 
            // btnBrowseCsv
            // 
            btnBrowseCsv.BorderRadius = 8;
            btnBrowseCsv.CustomizableEdges = customizableEdges5;
            btnBrowseCsv.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnBrowseCsv.ForeColor = System.Drawing.Color.White;
            btnBrowseCsv.Location = new System.Drawing.Point(454, 34);
            btnBrowseCsv.Name = "btnBrowseCsv";
            btnBrowseCsv.ShadowDecoration.CustomizableEdges = customizableEdges6;
            btnBrowseCsv.Size = new System.Drawing.Size(95, 36);
            btnBrowseCsv.TabIndex = 11;
            btnBrowseCsv.Text = "Browse";
            btnBrowseCsv.Click += btnBrowseCsv_Click;

            // 
            // txtCsvPath
            // 
            txtCsvPath.BackColor = System.Drawing.SystemColors.Control;
            txtCsvPath.BorderRadius = 8;
            txtCsvPath.CustomizableEdges = customizableEdges7;
            txtCsvPath.DefaultText = "";
            txtCsvPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            txtCsvPath.Location = new System.Drawing.Point(244, 34);
            txtCsvPath.Name = "txtCsvPath";
            txtCsvPath.PlaceholderText = "SELECT A CSV FILE";
            txtCsvPath.ReadOnly = true;
            txtCsvPath.SelectedText = "";
            txtCsvPath.ShadowDecoration.CustomizableEdges = customizableEdges8;
            txtCsvPath.Size = new System.Drawing.Size(204, 36);
            txtCsvPath.TabIndex = 10;

            // 
            // lblTemp
            // 
            lblTemp.BackColor = System.Drawing.Color.Transparent;
            lblTemp.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            lblTemp.ForeColor = System.Drawing.Color.FromArgb(125, 137, 149);
            lblTemp.Location = new System.Drawing.Point(244, 2);
            lblTemp.Name = "lblTemp";
            lblTemp.Size = new System.Drawing.Size(160, 27);
            lblTemp.TabIndex = 7;
            lblTemp.Text = "INVENTORY PAGE";

            // 
            // lblLogs
            // 
            lblLogs.BackColor = System.Drawing.Color.Transparent;
            lblLogs.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            lblLogs.ForeColor = System.Drawing.Color.FromArgb(90, 90, 90);
            lblLogs.Location = new System.Drawing.Point(244, 282);
            lblLogs.Name = "lblLogs";
            lblLogs.Size = new System.Drawing.Size(31, 19);
            lblLogs.TabIndex = 20;
            lblLogs.Text = "Logs";

            // 
            // txtLogs
            // 
            txtLogs.BackColor = System.Drawing.Color.White;
            txtLogs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtLogs.Font = new System.Drawing.Font("Consolas", 9F);
            txtLogs.ForeColor = System.Drawing.Color.Black;
            txtLogs.Location = new System.Drawing.Point(244, 307);
            txtLogs.Name = "txtLogs";
            txtLogs.ReadOnly = true;
            txtLogs.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            txtLogs.Size = new System.Drawing.Size(536, 128);
            txtLogs.TabIndex = 21;
            txtLogs.Text = "";
            txtLogs.WordWrap = false;

            // 
            // PageInventory
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlRoot);
            Name = "PageInventory";
            Size = new System.Drawing.Size(800, 480);

            pnlRoot.ResumeLayout(false);
            pnlRoot.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInventory).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2Panel pnlRoot;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblTemp;

        private Guna.UI2.WinForms.Guna2TextBox txtCsvPath;
        private Guna.UI2.WinForms.Guna2Button btnBrowseCsv;
        private Guna.UI2.WinForms.Guna2Button btnReadCsv;
        private Guna.UI2.WinForms.Guna2Button btnStartUploading;
        private Guna.UI2.WinForms.Guna2DataGridView dgvInventory;

        private Guna.UI2.WinForms.Guna2HtmlLabel lblLogs;
        private System.Windows.Forms.RichTextBox txtLogs;
    }
}