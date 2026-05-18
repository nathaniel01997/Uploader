using System;
using System.Windows.Forms;

namespace GXUploader
{
    public partial class MainForm : Form
    {
        // Configuration dropdown state
        private bool menuConfigExpanded = false;

        // Uploader dropdown state
        private bool menuUploaderExpanded = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 🔒 LOGIN FIRST
            using (LoginForm login = new LoginForm())
            {
                login.ShowDialog();

                if (!login.IsAuthenticated)
                {
                    Application.Exit();
                    return;
                }
            }

            // ✅ CONTINUE ONLY IF LOGIN SUCCESS

            timerDateTime.Start();

            pnlSideBar.BringToFront();
            lblDateTime.BringToFront();

            // Default state
            btnHome.Checked = true;

            // Collapse dropdowns on load
            SetConfigMenuStatus(false);
            SetUploaderMenuStatus(false);

            // Load default page
            LoadPage(new PageMain());
        }

        // =========================
        // MAIN MENU BUTTONS
        // =========================

        private void btnHome_Click(object sender, EventArgs e)
        {
            // checked states
            btnHome.Checked = true;
            btnConfiguration.Checked = false;
            btnUploader.Checked = false;

            // close dropdowns
            SetConfigMenuStatus(false);
            SetUploaderMenuStatus(false);

            LoadPage(new PageHome());
        }

        private void btnConfiguration_Click(object sender, EventArgs e)
        {
            // close other dropdown
            SetUploaderMenuStatus(false);

            // toggle config dropdown
            SetConfigMenuStatus(!menuConfigExpanded);

            // If expanded, optionally load default submenu page (Prism)
            if (menuConfigExpanded)
            {
                btnConfiguration.Checked = true;
                btnHome.Checked = false;
                btnUploader.Checked = false;

                // Default selection
                btnSubPrism.Checked = false;
                btnSubDatabase.Checked = true;

                LoadPage(new PageDatabase());
            }
            else
            {
                // If collapsed and you're on it, just uncheck
                btnConfiguration.Checked = false;
            }
        }

        private void btnUploader_Click(object sender, EventArgs e)
        {
            // close other dropdown
            SetConfigMenuStatus(false);

            // toggle uploader dropdown
            SetUploaderMenuStatus(!menuUploaderExpanded);

            // If expanded, optionally load default submenu page (Inventory)
            if (menuUploaderExpanded)
            {
                btnUploader.Checked = true;
                btnHome.Checked = false;
                btnConfiguration.Checked = false;

                // Default selection
                btnSubInventory.Checked = true;

                LoadPage(new PageInventory());
            }
            else
            {
                btnUploader.Checked = false;
            }
        }

        // =========================
        // SUBMENU BUTTONS
        // =========================

        private void btnSubDatabase_Click(object sender, EventArgs e)
        {
            btnSubDatabase.Checked = true;
            btnSubPrism.Checked = false;

            btnConfiguration.Checked = true;
            btnUploader.Checked = false;
            btnHome.Checked = false;

            LoadPage(new PageDatabase());
        }

        private void btnSubPrism_Click(object sender, EventArgs e)
        {
            btnSubPrism.Checked = true;
            btnSubDatabase.Checked = false;

            btnConfiguration.Checked = true;
            btnUploader.Checked = false;
            btnHome.Checked = false;

            LoadPage(new PageConfigPrism());
        }

        private void btnSubInventory_Click(object sender, EventArgs e)
        {
            btnSubInventory.Checked = true;

            btnUploader.Checked = true;
            btnConfiguration.Checked = false;
            btnHome.Checked = false;

            LoadPage(new PageInventory());
        }

        // =========================
        // HELPERS
        // =========================

        private void SetConfigMenuStatus(bool isExpanded)
        {
            menuConfigExpanded = isExpanded;

            // pnlSubMenu1 = Config submenu panel
            pnlSubMenu1.Visible = isExpanded;
            pnlSubMenu1.Height = isExpanded
                ? (btnSubDatabase.Height + btnSubPrism.Height)
                : 0;
        }

        private void SetUploaderMenuStatus(bool isExpanded)
        {
            menuUploaderExpanded = isExpanded;

            // pnlSubMenuUploader = Uploader submenu panel
            pnlSubMenuUploader.Visible = isExpanded;
            pnlSubMenuUploader.Height = isExpanded
                ? btnSubInventory.Height
                : 0;
        }

        private void timerDateTime_Tick(object sender, EventArgs e)
        {
            lblDateTime.Text = DateTime.Now.ToString("dddd, MMM dd yyyy\nhh:mm:ss tt");
        }

        private void lblVersion_Click(object sender, EventArgs e)
        {
            // optional
        }

        private void LoadPage(UserControl page)
        {
            pnlMainContent.Controls.Clear();
            page.Dock = DockStyle.Fill;
            pnlMainContent.Controls.Add(page);
        }
    }
}