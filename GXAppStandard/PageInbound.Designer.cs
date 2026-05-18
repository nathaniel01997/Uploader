namespace GXUploader
{
    partial class PageInbound
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
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            pnlRoot = new Guna.UI2.WinForms.Guna2Panel();
            lblTemp = new Guna.UI2.WinForms.Guna2HtmlLabel();
            pnlRoot.SuspendLayout();
            SuspendLayout();
            // 
            // pnlRoot
            // 
            pnlRoot.Controls.Add(lblTemp);
            pnlRoot.CustomizableEdges = customizableEdges1;
            pnlRoot.Dock = DockStyle.Fill;
            pnlRoot.FillColor = SystemColors.Control;
            pnlRoot.Location = new Point(0, 0);
            pnlRoot.Name = "pnlRoot";
            pnlRoot.ShadowDecoration.CustomizableEdges = customizableEdges2;
            pnlRoot.Size = new Size(800, 480);
            pnlRoot.TabIndex = 0;
            // 
            // lblTemp
            // 
            lblTemp.BackColor = Color.Transparent;
            lblTemp.Font = new Font("Segoe UI Semibold", 21.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTemp.ForeColor = Color.FromArgb(125, 137, 149);
            lblTemp.Location = new Point(415, 181);
            lblTemp.Name = "lblTemp";
            lblTemp.Size = new Size(215, 42);
            lblTemp.TabIndex = 7;
            lblTemp.Text = "INBOUND PAGE";
            // 
            // PageInbound
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlRoot);
            Name = "PageInbound";
            Size = new Size(800, 480);
            pnlRoot.ResumeLayout(false);
            pnlRoot.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2Panel pnlRoot;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblTemp;
    }
}
