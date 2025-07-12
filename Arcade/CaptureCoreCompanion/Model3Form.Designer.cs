// Model3Form.Designer.cs
namespace CaptureCoreCompanion
{
    partial class Model3Form
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

        private System.Windows.Forms.Label lblEmulatorPath;
        private System.Windows.Forms.TextBox txtEmulatorPath;
        private System.Windows.Forms.Button btnEmulatorBrowse;

        private System.Windows.Forms.Label lblRomsPath;
        private System.Windows.Forms.TextBox txtRomsPath;
        private System.Windows.Forms.Button btnRomsBrowse;

        private System.Windows.Forms.Label lblOutputPath;
        private System.Windows.Forms.TextBox txtOutputPath;
        private System.Windows.Forms.Button btnOutputBrowse;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Button btnGenerate;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            lblEmulatorPath = new Label();
            txtEmulatorPath = new TextBox();
            btnEmulatorBrowse = new Button();
            lblRomsPath = new Label();
            txtRomsPath = new TextBox();
            btnRomsBrowse = new Button();
            lblOutputPath = new Label();
            txtOutputPath = new TextBox();
            btnOutputBrowse = new Button();
            lblInfo = new Label();
            btnGenerate = new Button();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.Controls.Add(lblEmulatorPath, 0, 0);
            tableLayoutPanel1.Controls.Add(txtEmulatorPath, 1, 0);
            tableLayoutPanel1.Controls.Add(btnEmulatorBrowse, 2, 0);
            tableLayoutPanel1.Controls.Add(lblRomsPath, 0, 1);
            tableLayoutPanel1.Controls.Add(txtRomsPath, 1, 1);
            tableLayoutPanel1.Controls.Add(btnRomsBrowse, 2, 1);
            tableLayoutPanel1.Controls.Add(lblOutputPath, 0, 2);
            tableLayoutPanel1.Controls.Add(txtOutputPath, 1, 2);
            tableLayoutPanel1.Controls.Add(btnOutputBrowse, 2, 2);
            tableLayoutPanel1.Controls.Add(lblInfo, 0, 3);
            tableLayoutPanel1.Controls.Add(btnGenerate, 1, 3);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 3, 4, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(12);
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanel1.Size = new Size(700, 277);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // lblEmulatorPath
            // 
            lblEmulatorPath.AutoSize = true;
            lblEmulatorPath.Location = new Point(16, 12);
            lblEmulatorPath.Margin = new Padding(4, 0, 4, 0);
            lblEmulatorPath.Name = "lblEmulatorPath";
            lblEmulatorPath.Size = new Size(254, 15);
            lblEmulatorPath.TabIndex = 0;
            lblEmulatorPath.Text = "Select Supermodel Emulator (Supermodel.exe):";
            // 
            // txtEmulatorPath
            // 
            txtEmulatorPath.Dock = DockStyle.Fill;
            txtEmulatorPath.Location = new Point(278, 15);
            txtEmulatorPath.Margin = new Padding(4, 3, 4, 3);
            txtEmulatorPath.Name = "txtEmulatorPath";
            txtEmulatorPath.Size = new Size(310, 23);
            txtEmulatorPath.TabIndex = 1;
            // 
            // btnEmulatorBrowse
            // 
            btnEmulatorBrowse.AutoSize = true;
            btnEmulatorBrowse.Location = new Point(596, 15);
            btnEmulatorBrowse.Margin = new Padding(4, 3, 4, 3);
            btnEmulatorBrowse.Name = "btnEmulatorBrowse";
            btnEmulatorBrowse.Size = new Size(88, 25);
            btnEmulatorBrowse.TabIndex = 2;
            btnEmulatorBrowse.Text = "Browse...";
            btnEmulatorBrowse.Click += BtnEmulatorBrowse_Click;
            // 
            // lblRomsPath
            // 
            lblRomsPath.AutoSize = true;
            lblRomsPath.Location = new Point(16, 43);
            lblRomsPath.Margin = new Padding(4, 0, 4, 0);
            lblRomsPath.Name = "lblRomsPath";
            lblRomsPath.Size = new Size(112, 15);
            lblRomsPath.TabIndex = 3;
            lblRomsPath.Text = "Select ROMs Folder:";
            // 
            // txtRomsPath
            // 
            txtRomsPath.Dock = DockStyle.Fill;
            txtRomsPath.Location = new Point(278, 46);
            txtRomsPath.Margin = new Padding(4, 3, 4, 3);
            txtRomsPath.Name = "txtRomsPath";
            txtRomsPath.Size = new Size(310, 23);
            txtRomsPath.TabIndex = 4;
            // 
            // btnRomsBrowse
            // 
            btnRomsBrowse.AutoSize = true;
            btnRomsBrowse.Location = new Point(596, 46);
            btnRomsBrowse.Margin = new Padding(4, 3, 4, 3);
            btnRomsBrowse.Name = "btnRomsBrowse";
            btnRomsBrowse.Size = new Size(88, 25);
            btnRomsBrowse.TabIndex = 5;
            btnRomsBrowse.Text = "Browse...";
            btnRomsBrowse.Click += BtnRomsBrowse_Click;
            // 
            // lblOutputPath
            // 
            lblOutputPath.AutoSize = true;
            lblOutputPath.Location = new Point(16, 74);
            lblOutputPath.Margin = new Padding(4, 0, 4, 0);
            lblOutputPath.Name = "lblOutputPath";
            lblOutputPath.Size = new Size(200, 15);
            lblOutputPath.TabIndex = 6;
            lblOutputPath.Text = "Select EmuVR/Games Output Folder:";
            // 
            // txtOutputPath
            // 
            txtOutputPath.Dock = DockStyle.Fill;
            txtOutputPath.Location = new Point(278, 77);
            txtOutputPath.Margin = new Padding(4, 3, 4, 3);
            txtOutputPath.Name = "txtOutputPath";
            txtOutputPath.Size = new Size(310, 23);
            txtOutputPath.TabIndex = 7;
            // 
            // btnOutputBrowse
            // 
            btnOutputBrowse.AutoSize = true;
            btnOutputBrowse.Location = new Point(596, 77);
            btnOutputBrowse.Margin = new Padding(4, 3, 4, 3);
            btnOutputBrowse.Name = "btnOutputBrowse";
            btnOutputBrowse.Size = new Size(88, 24);
            btnOutputBrowse.TabIndex = 8;
            btnOutputBrowse.Text = "Browse...";
            btnOutputBrowse.Click += BtnOutputBrowse_Click;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Location = new Point(16, 104);
            lblInfo.Margin = new Padding(4, 0, 4, 0);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(156, 15);
            lblInfo.TabIndex = 10;
            lblInfo.Text = "Generate Capture Core Files.";
            // 
            // btnGenerate
            // 
            btnGenerate.Anchor = AnchorStyles.Top;
            btnGenerate.AutoSize = true;
            btnGenerate.Location = new Point(389, 107);
            btnGenerate.Margin = new Padding(4, 3, 4, 3);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(88, 25);
            btnGenerate.TabIndex = 9;
            btnGenerate.Text = "Generate";
            btnGenerate.Click += BtnGenerate_Click;
            // 
            // Model3Form
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 277);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "Model3Form";
            Text = "Sega Model 3 Capture Core Settings";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }
    }
}
