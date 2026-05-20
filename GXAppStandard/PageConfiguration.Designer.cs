namespace GXUploader
{
    partial class PageConfiguration
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblVendorCode;
        private Guna.UI2.WinForms.Guna2ToggleSwitch tgUDFText;

        private System.Windows.Forms.Label lblVendorName;
        private Guna.UI2.WinForms.Guna2ToggleSwitch tgVendorName;

        private System.Windows.Forms.Label lblDescription;
        private Guna.UI2.WinForms.Guna2ToggleSwitch tgDescription;

        private System.Windows.Forms.Label lblCost;
        private Guna.UI2.WinForms.Guna2ToggleSwitch tgCost;

        private System.Windows.Forms.Label lblPriceLevel;
        private Guna.UI2.WinForms.Guna2ToggleSwitch tgPriceLevel;

        private System.Windows.Forms.Label lblTaxCode;
        private Guna.UI2.WinForms.Guna2ToggleSwitch tgTaxCode;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            guna2HtmlLabel1 = new Guna.UI2.WinForms.Guna2HtmlLabel();
            header = new Guna.UI2.WinForms.Guna2HtmlLabel();
            lblUDFText = new Label();
            tgUDFText = new Guna.UI2.WinForms.Guna2ToggleSwitch();
            label1 = new Label();
            label2 = new Label();
            tgText = new Guna.UI2.WinForms.Guna2ToggleSwitch();
            label3 = new Label();
            tgUDFDate = new Guna.UI2.WinForms.Guna2ToggleSwitch();
            SuspendLayout();
            // 
            // guna2HtmlLabel1
            // 
            guna2HtmlLabel1.BackColor = Color.Transparent;
            guna2HtmlLabel1.Font = new Font("Arial Narrow", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            guna2HtmlLabel1.ForeColor = Color.Gray;
            guna2HtmlLabel1.Location = new Point(258, 53);
            guna2HtmlLabel1.Name = "guna2HtmlLabel1";
            guna2HtmlLabel1.Size = new Size(439, 18);
            guna2HtmlLabel1.TabIndex = 3;
            guna2HtmlLabel1.Text = "Note: Turn ON to include this field in changes and updates. Turn OFF to exclude it.";
            // 
            // header
            // 
            header.BackColor = Color.Transparent;
            header.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            header.ForeColor = Color.Gray;
            header.Location = new Point(240, 13);
            header.Name = "header";
            header.Size = new Size(201, 34);
            header.TabIndex = 2;
            header.Text = "CONFIGURATION";
            // 
            // lblUDFText
            // 
            lblUDFText.AutoSize = true;
            lblUDFText.Font = new Font("Segoe UI", 10F);
            lblUDFText.Location = new Point(241, 131);
            lblUDFText.Name = "lblUDFText";
            lblUDFText.Size = new Size(106, 19);
            lblUDFText.TabIndex = 0;
            lblUDFText.Text = "UDF Text (1-15)";
            // 
            // tgUDFText
            // 
            tgUDFText.CheckedState.FillColor = Color.MediumSeaGreen;
            tgUDFText.CustomizableEdges = customizableEdges1;
            tgUDFText.Location = new Point(383, 131);
            tgUDFText.Name = "tgUDFText";
            tgUDFText.ShadowDecoration.CustomizableEdges = customizableEdges2;
            tgUDFText.Size = new Size(45, 22);
            tgUDFText.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(240, 88);
            label1.Name = "label1";
            label1.Size = new Size(102, 21);
            label1.TabIndex = 4;
            label1.Text = "DATA ITEMS";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10F);
            label2.Location = new Point(241, 159);
            label2.Name = "label2";
            label2.Size = new Size(80, 19);
            label2.TabIndex = 5;
            label2.Text = "TEXT (1-10)";
            // 
            // tgText
            // 
            tgText.CheckedState.FillColor = Color.MediumSeaGreen;
            tgText.CustomizableEdges = customizableEdges3;
            tgText.Location = new Point(383, 159);
            tgText.Name = "tgText";
            tgText.ShadowDecoration.CustomizableEdges = customizableEdges4;
            tgText.Size = new Size(45, 22);
            tgText.TabIndex = 6;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 10F);
            label3.Location = new Point(241, 187);
            label3.Name = "label3";
            label3.Size = new Size(72, 19);
            label3.TabIndex = 7;
            label3.Text = "DATE UDF";
            // 
            // tgUDFDate
            // 
            tgUDFDate.CheckedState.FillColor = Color.MediumSeaGreen;
            tgUDFDate.CustomizableEdges = customizableEdges5;
            tgUDFDate.Location = new Point(383, 187);
            tgUDFDate.Name = "tgUDFDate";
            tgUDFDate.ShadowDecoration.CustomizableEdges = customizableEdges6;
            tgUDFDate.Size = new Size(45, 22);
            tgUDFDate.TabIndex = 8;
            // 
            // PageConfiguration
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(label3);
            Controls.Add(tgUDFDate);
            Controls.Add(label2);
            Controls.Add(tgText);
            Controls.Add(label1);
            Controls.Add(lblUDFText);
            Controls.Add(guna2HtmlLabel1);
            Controls.Add(tgUDFText);
            Controls.Add(header);
            Name = "PageConfiguration";
            Size = new Size(800, 480);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblUDFText;
        private Guna.UI2.WinForms.Guna2HtmlLabel guna2HtmlLabel1;
        private Guna.UI2.WinForms.Guna2HtmlLabel header;
        private Label label1;
        private Label label2;
        private Guna.UI2.WinForms.Guna2ToggleSwitch tgText;
        private Label label3;
        private Guna.UI2.WinForms.Guna2ToggleSwitch tgUDFDate;
    }
}