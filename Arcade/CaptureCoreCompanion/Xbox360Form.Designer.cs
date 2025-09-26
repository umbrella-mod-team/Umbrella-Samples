// Xbox360Form.Designer.cs
namespace CaptureCoreCompanion
{
    partial class Xbox360Form
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

        private System.Windows.Forms.Label lblXeniaPath;
        private System.Windows.Forms.TextBox txtXeniaPath;
        private System.Windows.Forms.Button btnXeniaBrowse;

        private System.Windows.Forms.Label lblInstalledFolder;
        private System.Windows.Forms.TextBox txtInstalledFolder;
        private System.Windows.Forms.Button btnInstalledBrowse;

        private System.Windows.Forms.Label lblXbox360Folder;
        private System.Windows.Forms.TextBox txtXbox360Folder;
        private System.Windows.Forms.Button btnXbox360Browse;

        private System.Windows.Forms.Label lblGODFolder;
        private System.Windows.Forms.TextBox txtGODFolder;
        private System.Windows.Forms.Button btnGodBrowse;

        private System.Windows.Forms.Label lblOutputXBLA;
        private System.Windows.Forms.TextBox txtOutputXBLA;
        private System.Windows.Forms.Button btnOutputXBLA;

        private System.Windows.Forms.Label lblOutputXbox360;
        private System.Windows.Forms.TextBox txtOutputXbox360;
        private System.Windows.Forms.Button btnOutput360;
        private System.Windows.Forms.FlowLayoutPanel panelFooter;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.CheckBox chkRelativePaths;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            lblXeniaPath = new Label();
            txtXeniaPath = new TextBox();
            btnXeniaBrowse = new Button();
            lblInstalledFolder = new Label();
            txtInstalledFolder = new TextBox();
            btnInstalledBrowse = new Button();
            lblXbox360Folder = new Label();
            txtXbox360Folder = new TextBox();
            btnXbox360Browse = new Button();
            lblGODFolder = new Label();
            txtGODFolder = new TextBox();
            btnGodBrowse = new Button();
            lblOutputXBLA = new Label();
            txtOutputXBLA = new TextBox();
            btnOutputXBLA = new Button();
            lblOutputXbox360 = new Label();
            txtOutputXbox360 = new TextBox();
            btnOutput360 = new Button();
            panelFooter = new FlowLayoutPanel();
            btnGenerate = new Button();
            chkRelativePaths = new CheckBox();
            tableLayoutPanel1.SuspendLayout();
            panelFooter.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.Controls.Add(lblXeniaPath, 0, 0);
            tableLayoutPanel1.Controls.Add(txtXeniaPath, 1, 0);
            tableLayoutPanel1.Controls.Add(btnXeniaBrowse, 2, 0);
            tableLayoutPanel1.Controls.Add(lblInstalledFolder, 0, 1);
            tableLayoutPanel1.Controls.Add(txtInstalledFolder, 1, 1);
            tableLayoutPanel1.Controls.Add(btnInstalledBrowse, 2, 1);
            tableLayoutPanel1.Controls.Add(lblXbox360Folder, 0, 2);
            tableLayoutPanel1.Controls.Add(txtXbox360Folder, 1, 2);
            tableLayoutPanel1.Controls.Add(btnXbox360Browse, 2, 2);
            tableLayoutPanel1.Controls.Add(lblGODFolder, 0, 3);
            tableLayoutPanel1.Controls.Add(txtGODFolder, 1, 3);
            tableLayoutPanel1.Controls.Add(btnGodBrowse, 2, 3);
            tableLayoutPanel1.Controls.Add(lblOutputXBLA, 0, 4);
            tableLayoutPanel1.Controls.Add(txtOutputXBLA, 1, 4);
            tableLayoutPanel1.Controls.Add(btnOutputXBLA, 2, 4);
            tableLayoutPanel1.Controls.Add(lblOutputXbox360, 0, 5);
            tableLayoutPanel1.Controls.Add(txtOutputXbox360, 1, 5);
            tableLayoutPanel1.Controls.Add(btnOutput360, 2, 5);
            tableLayoutPanel1.Controls.Add(panelFooter, 1, 6);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(12);
            tableLayoutPanel1.RowCount = 7;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
            tableLayoutPanel1.Size = new Size(703, 258);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // lblXeniaPath
            // 
            lblXeniaPath.AutoSize = true;
            lblXeniaPath.Location = new Point(15, 12);
            lblXeniaPath.Name = "lblXeniaPath";
            lblXeniaPath.Size = new Size(184, 15);
            lblXeniaPath.TabIndex = 0;
            lblXeniaPath.Text = "Select Xenia Emulator (Xenia.exe):";
            // 
            // txtXeniaPath
            // 
            txtXeniaPath.Dock = DockStyle.Fill;
            txtXeniaPath.Location = new Point(212, 15);
            txtXeniaPath.Name = "txtXeniaPath";
            txtXeniaPath.Size = new Size(395, 23);
            txtXeniaPath.TabIndex = 1;
            // 
            // btnXeniaBrowse
            // 
            btnXeniaBrowse.AutoSize = true;
            btnXeniaBrowse.Location = new Point(613, 15);
            btnXeniaBrowse.Name = "btnXeniaBrowse";
            btnXeniaBrowse.Size = new Size(75, 25);
            btnXeniaBrowse.TabIndex = 2;
            btnXeniaBrowse.Text = "Browse...";
            btnXeniaBrowse.Click += BtnXeniaBrowse_Click;
            // 
            // lblInstalledFolder
            // 
            lblInstalledFolder.AutoSize = true;
            lblInstalledFolder.Location = new Point(15, 45);
            lblInstalledFolder.Name = "lblInstalledFolder";
            lblInstalledFolder.Size = new Size(162, 15);
            lblInstalledFolder.TabIndex = 3;
            lblInstalledFolder.Text = "Installed/XBLA Games Folder:";
            // 
            // txtInstalledFolder
            // 
            txtInstalledFolder.Dock = DockStyle.Fill;
            txtInstalledFolder.Location = new Point(212, 48);
            txtInstalledFolder.Name = "txtInstalledFolder";
            txtInstalledFolder.Size = new Size(395, 23);
            txtInstalledFolder.TabIndex = 4;
            // 
            // btnInstalledBrowse
            // 
            btnInstalledBrowse.AutoSize = true;
            btnInstalledBrowse.Location = new Point(613, 48);
            btnInstalledBrowse.Name = "btnInstalledBrowse";
            btnInstalledBrowse.Size = new Size(75, 25);
            btnInstalledBrowse.TabIndex = 5;
            btnInstalledBrowse.Text = "Browse...";
            btnInstalledBrowse.Click += BtnInstalledBrowse_Click;
            // 
            // lblXbox360Folder
            // 
            lblXbox360Folder.AutoSize = true;
            lblXbox360Folder.Location = new Point(15, 78);
            lblXbox360Folder.Name = "lblXbox360Folder";
            lblXbox360Folder.Size = new Size(132, 15);
            lblXbox360Folder.TabIndex = 6;
            lblXbox360Folder.Text = "Xbox 360 Games Folder:";
            // 
            // txtXbox360Folder
            // 
            txtXbox360Folder.Dock = DockStyle.Fill;
            txtXbox360Folder.Location = new Point(212, 81);
            txtXbox360Folder.Name = "txtXbox360Folder";
            txtXbox360Folder.Size = new Size(395, 23);
            txtXbox360Folder.TabIndex = 7;
            // 
            // btnXbox360Browse
            // 
            btnXbox360Browse.AutoSize = true;
            btnXbox360Browse.Location = new Point(613, 81);
            btnXbox360Browse.Name = "btnXbox360Browse";
            btnXbox360Browse.Size = new Size(75, 25);
            btnXbox360Browse.TabIndex = 8;
            btnXbox360Browse.Text = "Browse...";
            btnXbox360Browse.Click += BtnXbox360Browse_Click;
            // 
            // lblGODFolder
            // 
            lblGODFolder.AutoSize = true;
            lblGODFolder.Location = new Point(15, 111);
            lblGODFolder.Name = "lblGODFolder";
            lblGODFolder.Size = new Size(160, 15);
            lblGODFolder.TabIndex = 9;
            lblGODFolder.Text = "Xbox 360 GOD Games Folder:";
            // 
            // txtGODFolder
            // 
            txtGODFolder.Dock = DockStyle.Fill;
            txtGODFolder.Location = new Point(212, 114);
            txtGODFolder.Name = "txtGODFolder";
            txtGODFolder.Size = new Size(395, 23);
            txtGODFolder.TabIndex = 10;
            // 
            // btnGodBrowse
            // 
            btnGodBrowse.AutoSize = true;
            btnGodBrowse.Location = new Point(613, 114);
            btnGodBrowse.Name = "btnGodBrowse";
            btnGodBrowse.Size = new Size(75, 25);
            btnGodBrowse.TabIndex = 11;
            btnGodBrowse.Text = "Browse...";
            btnGodBrowse.Click += BtnGodBrowse_Click;
            // 
            // lblOutputXBLA
            // 
            lblOutputXBLA.AutoSize = true;
            lblOutputXBLA.Location = new Point(15, 144);
            lblOutputXBLA.Name = "lblOutputXBLA";
            lblOutputXBLA.Size = new Size(172, 15);
            lblOutputXBLA.TabIndex = 12;
            lblOutputXBLA.Text = "Output Folder for XBLA Games:";
            // 
            // txtOutputXBLA
            // 
            txtOutputXBLA.Dock = DockStyle.Fill;
            txtOutputXBLA.Location = new Point(212, 147);
            txtOutputXBLA.Name = "txtOutputXBLA";
            txtOutputXBLA.Size = new Size(395, 23);
            txtOutputXBLA.TabIndex = 13;
            // 
            // btnOutputXBLA
            // 
            btnOutputXBLA.AutoSize = true;
            btnOutputXBLA.Location = new Point(613, 147);
            btnOutputXBLA.Name = "btnOutputXBLA";
            btnOutputXBLA.Size = new Size(75, 25);
            btnOutputXBLA.TabIndex = 14;
            btnOutputXBLA.Text = "Browse...";
            btnOutputXBLA.Click += BtnOutputXBLA_Click;
            // 
            // lblOutputXbox360
            // 
            lblOutputXbox360.AutoSize = true;
            lblOutputXbox360.Location = new Point(15, 177);
            lblOutputXbox360.Name = "lblOutputXbox360";
            lblOutputXbox360.Size = new Size(191, 15);
            lblOutputXbox360.TabIndex = 15;
            lblOutputXbox360.Text = "Output Folder for Xbox 360 Games:";
            // 
            // txtOutputXbox360
            // 
            txtOutputXbox360.Dock = DockStyle.Fill;
            txtOutputXbox360.Location = new Point(212, 180);
            txtOutputXbox360.Name = "txtOutputXbox360";
            txtOutputXbox360.Size = new Size(395, 23);
            txtOutputXbox360.TabIndex = 16;
            // 
            // btnOutput360
            // 
            btnOutput360.AutoSize = true;
            btnOutput360.Location = new Point(613, 180);
            btnOutput360.Name = "btnOutput360";
            btnOutput360.Size = new Size(75, 25);
            btnOutput360.TabIndex = 17;
            btnOutput360.Text = "Browse...";
            btnOutput360.Click += BtnOutput360_Click;
            // 
            // panelFooter
            // 
            panelFooter.AutoSize = true;
            tableLayoutPanel1.SetColumnSpan(panelFooter, 2);
            panelFooter.Controls.Add(btnGenerate);
            panelFooter.Controls.Add(chkRelativePaths);
            panelFooter.Location = new Point(212, 213);
            panelFooter.Name = "panelFooter";
            panelFooter.Size = new Size(240, 31);
            panelFooter.TabIndex = 19;
            // 
            // btnGenerate
            // 
            btnGenerate.AutoSize = true;
            btnGenerate.Location = new Point(3, 3);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(75, 25);
            btnGenerate.TabIndex = 0;
            btnGenerate.Text = "Generate";
            btnGenerate.Click += BtnGenerate_Click;
            // 
            // chkRelativePaths
            // 
            chkRelativePaths.AutoSize = true;
            chkRelativePaths.Location = new Point(84, 3);
            chkRelativePaths.Name = "chkRelativePaths";
            chkRelativePaths.Size = new Size(153, 19);
            chkRelativePaths.TabIndex = 1;
            chkRelativePaths.Text = "Try to use Relative Paths";
            // 
            // Xbox360Form
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(703, 258);
            Controls.Add(tableLayoutPanel1);
            Name = "Xbox360Form";
            Text = "Xbox 360 Capture Core Settings";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            panelFooter.ResumeLayout(false);
            panelFooter.PerformLayout();
            ResumeLayout(false);
        }
    }
}
