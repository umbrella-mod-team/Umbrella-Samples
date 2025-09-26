// PS3Form.Designer.cs
namespace CaptureCoreCompanion
{
    partial class PS3Form
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

        private System.Windows.Forms.Label lblPS3Path;
        private System.Windows.Forms.TextBox txtPS3Path;
        private System.Windows.Forms.Button btnPS3Browse;

        private System.Windows.Forms.Label lblPS3GamesFolder;
        private System.Windows.Forms.TextBox txtPS3GamesFolder;
        private System.Windows.Forms.Button btnPS3GamesBrowse;

        private System.Windows.Forms.Label lblOutputPSNFolder;
        private System.Windows.Forms.TextBox txtOutputPSNFolder;
        private System.Windows.Forms.Button btnOutputPSNBrowse;

        private System.Windows.Forms.Label lblOutputPS3Folder;
        private System.Windows.Forms.TextBox txtOutputPS3Folder;
        private System.Windows.Forms.Button btnOutputPS3Browse;
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
            lblPS3Path = new Label();
            txtPS3Path = new TextBox();
            btnPS3Browse = new Button();
            lblPS3GamesFolder = new Label();
            txtPS3GamesFolder = new TextBox();
            btnPS3GamesBrowse = new Button();
            lblOutputPSNFolder = new Label();
            txtOutputPSNFolder = new TextBox();
            btnOutputPSNBrowse = new Button();
            lblOutputPS3Folder = new Label();
            txtOutputPS3Folder = new TextBox();
            btnOutputPS3Browse = new Button();
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
            tableLayoutPanel1.Controls.Add(lblPS3Path, 0, 0);
            tableLayoutPanel1.Controls.Add(txtPS3Path, 1, 0);
            tableLayoutPanel1.Controls.Add(btnPS3Browse, 2, 0);
            tableLayoutPanel1.Controls.Add(lblPS3GamesFolder, 0, 1);
            tableLayoutPanel1.Controls.Add(txtPS3GamesFolder, 1, 1);
            tableLayoutPanel1.Controls.Add(btnPS3GamesBrowse, 2, 1);
            tableLayoutPanel1.Controls.Add(lblOutputPSNFolder, 0, 2);
            tableLayoutPanel1.Controls.Add(txtOutputPSNFolder, 1, 2);
            tableLayoutPanel1.Controls.Add(btnOutputPSNBrowse, 2, 2);
            tableLayoutPanel1.Controls.Add(lblOutputPS3Folder, 0, 3);
            tableLayoutPanel1.Controls.Add(txtOutputPS3Folder, 1, 3);
            tableLayoutPanel1.Controls.Add(btnOutputPS3Browse, 2, 3);
            tableLayoutPanel1.Controls.Add(panelFooter, 1, 4);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 3, 4, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(12);
            tableLayoutPanel1.RowCount = 5;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 148F));
            tableLayoutPanel1.Size = new Size(700, 203);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // lblPS3Path
            // 
            lblPS3Path.AutoSize = true;
            lblPS3Path.Location = new Point(16, 12);
            lblPS3Path.Margin = new Padding(4, 0, 4, 0);
            lblPS3Path.Name = "lblPS3Path";
            lblPS3Path.Size = new Size(191, 15);
            lblPS3Path.TabIndex = 0;
            lblPS3Path.Text = "Select RPCS3 Emulator (RPCS3.exe)";
            // 
            // txtPS3Path
            // 
            txtPS3Path.Dock = DockStyle.Fill;
            txtPS3Path.Location = new Point(215, 15);
            txtPS3Path.Margin = new Padding(4, 3, 4, 3);
            txtPS3Path.Name = "txtPS3Path";
            txtPS3Path.Size = new Size(346, 23);
            txtPS3Path.TabIndex = 1;
            // 
            // btnPS3Browse
            // 
            btnPS3Browse.AutoSize = true;
            btnPS3Browse.Location = new Point(569, 15);
            btnPS3Browse.Margin = new Padding(4, 3, 4, 3);
            btnPS3Browse.Name = "btnPS3Browse";
            btnPS3Browse.Size = new Size(115, 24);
            btnPS3Browse.TabIndex = 2;
            btnPS3Browse.Text = "Browse...";
            btnPS3Browse.Click += BtnPS3Browse_Click;
            // 
            // lblPS3GamesFolder
            // 
            lblPS3GamesFolder.AutoSize = true;
            lblPS3GamesFolder.Location = new Point(16, 42);
            lblPS3GamesFolder.Margin = new Padding(4, 0, 4, 0);
            lblPS3GamesFolder.Name = "lblPS3GamesFolder";
            lblPS3GamesFolder.Size = new Size(143, 15);
            lblPS3GamesFolder.TabIndex = 3;
            lblPS3GamesFolder.Text = "Source PS3 Games Folder:";
            // 
            // txtPS3GamesFolder
            // 
            txtPS3GamesFolder.Dock = DockStyle.Fill;
            txtPS3GamesFolder.Location = new Point(215, 45);
            txtPS3GamesFolder.Margin = new Padding(4, 3, 4, 3);
            txtPS3GamesFolder.Name = "txtPS3GamesFolder";
            txtPS3GamesFolder.Size = new Size(346, 23);
            txtPS3GamesFolder.TabIndex = 4;
            // 
            // btnPS3GamesBrowse
            // 
            btnPS3GamesBrowse.AutoSize = true;
            btnPS3GamesBrowse.Location = new Point(569, 45);
            btnPS3GamesBrowse.Margin = new Padding(4, 3, 4, 3);
            btnPS3GamesBrowse.Name = "btnPS3GamesBrowse";
            btnPS3GamesBrowse.Size = new Size(115, 24);
            btnPS3GamesBrowse.TabIndex = 5;
            btnPS3GamesBrowse.Text = "Browse...";
            btnPS3GamesBrowse.Click += BtnPS3GamesBrowse_Click;
            // 
            // lblOutputPSNFolder
            // 
            lblOutputPSNFolder.AutoSize = true;
            lblOutputPSNFolder.Location = new Point(16, 72);
            lblOutputPSNFolder.Margin = new Padding(4, 0, 4, 0);
            lblOutputPSNFolder.Name = "lblOutputPSNFolder";
            lblOutputPSNFolder.Size = new Size(166, 15);
            lblOutputPSNFolder.TabIndex = 6;
            lblOutputPSNFolder.Text = "Output Folder for PSN Games:";
            // 
            // txtOutputPSNFolder
            // 
            txtOutputPSNFolder.Dock = DockStyle.Fill;
            txtOutputPSNFolder.Location = new Point(215, 75);
            txtOutputPSNFolder.Margin = new Padding(4, 3, 4, 3);
            txtOutputPSNFolder.Name = "txtOutputPSNFolder";
            txtOutputPSNFolder.Size = new Size(346, 23);
            txtOutputPSNFolder.TabIndex = 7;
            // 
            // btnOutputPSNBrowse
            // 
            btnOutputPSNBrowse.AutoSize = true;
            btnOutputPSNBrowse.Location = new Point(569, 75);
            btnOutputPSNBrowse.Margin = new Padding(4, 3, 4, 3);
            btnOutputPSNBrowse.Name = "btnOutputPSNBrowse";
            btnOutputPSNBrowse.Size = new Size(115, 24);
            btnOutputPSNBrowse.TabIndex = 8;
            btnOutputPSNBrowse.Text = "Browse...";
            btnOutputPSNBrowse.Click += BtnOutputPSNBrowse_Click;
            // 
            // lblOutputPS3Folder
            // 
            lblOutputPS3Folder.AutoSize = true;
            lblOutputPS3Folder.Location = new Point(16, 102);
            lblOutputPS3Folder.Margin = new Padding(4, 0, 4, 0);
            lblOutputPS3Folder.Name = "lblOutputPS3Folder";
            lblOutputPS3Folder.Size = new Size(163, 15);
            lblOutputPS3Folder.TabIndex = 9;
            lblOutputPS3Folder.Text = "Output Folder for PS3 Games:";
            // 
            // txtOutputPS3Folder
            // 
            txtOutputPS3Folder.Dock = DockStyle.Fill;
            txtOutputPS3Folder.Location = new Point(215, 105);
            txtOutputPS3Folder.Margin = new Padding(4, 3, 4, 3);
            txtOutputPS3Folder.Name = "txtOutputPS3Folder";
            txtOutputPS3Folder.Size = new Size(346, 23);
            txtOutputPS3Folder.TabIndex = 10;
            // 
            // btnOutputPS3Browse
            // 
            btnOutputPS3Browse.AutoSize = true;
            btnOutputPS3Browse.Location = new Point(569, 105);
            btnOutputPS3Browse.Margin = new Padding(4, 3, 4, 3);
            btnOutputPS3Browse.Name = "btnOutputPS3Browse";
            btnOutputPS3Browse.Size = new Size(115, 24);
            btnOutputPS3Browse.TabIndex = 11;
            btnOutputPS3Browse.Text = "Browse...";
            btnOutputPS3Browse.Click += BtnOutputPS3Browse_Click;
            // 
            // panelFooter
            // 
            panelFooter.AutoSize = true;
            tableLayoutPanel1.SetColumnSpan(panelFooter, 2);
            panelFooter.Controls.Add(btnGenerate);
            panelFooter.Controls.Add(chkRelativePaths);
            panelFooter.Location = new Point(214, 135);
            panelFooter.Name = "panelFooter";
            panelFooter.Size = new Size(240, 31);
            panelFooter.TabIndex = 13;
            // 
            // btnGenerate
            // 
            btnGenerate.Anchor = AnchorStyles.Top;
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
            chkRelativePaths.TabIndex = 14;
            chkRelativePaths.Text = "Try to use Relative Paths";
            // 
            // PS3Form
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 203);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "PS3Form";
            Text = "PS3 Capture Core Settings";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            panelFooter.ResumeLayout(false);
            panelFooter.PerformLayout();
            ResumeLayout(false);
        }
        private FlowLayoutPanel panelFooter;
    }
}
