namespace GXUploader
{
    partial class PageMain
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            pnlRoot = new Guna.UI2.WinForms.Guna2Panel();
            lblTemp = new Guna.UI2.WinForms.Guna2HtmlLabel();
            lblTemp1 = new Guna.UI2.WinForms.Guna2HtmlLabel();
            ImgRproLogo = new Guna.UI2.WinForms.Guna2PictureBox();
            pnlRoot.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ImgRproLogo).BeginInit();
            SuspendLayout();
            // 
            // pnlRoot
            // 
            pnlRoot.Controls.Add(lblTemp);
            pnlRoot.Controls.Add(lblTemp1);
            pnlRoot.Controls.Add(ImgRproLogo);
            pnlRoot.CustomizableEdges = customizableEdges7;
            pnlRoot.Dock = DockStyle.Fill;
            pnlRoot.FillColor = SystemColors.Control;
            pnlRoot.Location = new Point(0, 0);
            pnlRoot.Name = "pnlRoot";
            pnlRoot.ShadowDecoration.CustomizableEdges = customizableEdges8;
            pnlRoot.Size = new Size(800, 480);
            pnlRoot.TabIndex = 2;
            // 
            // lblTemp
            // 
            lblTemp.BackColor = Color.Transparent;
            lblTemp.Font = new Font("Segoe UI Semibold", 21.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTemp.ForeColor = Color.FromArgb(125, 137, 149);
            lblTemp.Location = new Point(415, 181);
            lblTemp.Name = "lblTemp";
            lblTemp.Size = new Size(167, 42);
            lblTemp.TabIndex = 6;
            lblTemp.Text = "HOME PAGE";
            // 
            // lblTemp1
            // 
            lblTemp1.BackColor = Color.Transparent;
            lblTemp1.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold | FontStyle.Italic);
            lblTemp1.ForeColor = Color.FromArgb(125, 137, 149);
            lblTemp1.Location = new Point(398, 141);
            lblTemp1.Name = "lblTemp1";
            lblTemp1.Size = new Size(205, 34);
            lblTemp1.TabIndex = 5;
            lblTemp1.Text = "[Client Logo Here.]";
            // 
            // ImgRproLogo
            // 
            ImgRproLogo.BackColor = Color.Transparent;
            ImgRproLogo.CustomizableEdges = customizableEdges5;
            ImgRproLogo.FillColor = Color.Transparent;
            ImgRproLogo.Image = Properties.Resources.logo_retailpro;
            ImgRproLogo.ImageRotate = 0F;
            ImgRproLogo.Location = new Point(668, 377);
            ImgRproLogo.Name = "ImgRproLogo";
            ImgRproLogo.ShadowDecoration.CustomizableEdges = customizableEdges6;
            ImgRproLogo.Size = new Size(100, 50);
            ImgRproLogo.SizeMode = PictureBoxSizeMode.Zoom;
            ImgRproLogo.TabIndex = 4;
            ImgRproLogo.TabStop = false;
            // 
            // PageHome
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlRoot);
            Name = "PageHome";
            Size = new Size(800, 480);
            pnlRoot.ResumeLayout(false);
            pnlRoot.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)ImgRproLogo).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private Guna.UI2.WinForms.Guna2Panel pnlRoot;
        private Guna.UI2.WinForms.Guna2PictureBox ImgRproLogo;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblTemp1;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblTemp;
    }
}
