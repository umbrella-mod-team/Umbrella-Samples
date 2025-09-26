// PinballFXForm.Designer.cs
namespace CaptureCoreCompanion
{
    partial class PinballFxForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

        private System.Windows.Forms.Label lblEmulatorPath;
        private System.Windows.Forms.TextBox txtEmulatorPath;
        private System.Windows.Forms.Button btnBrowseEmulator;

        private System.Windows.Forms.Label lblOutputFolder;
        private System.Windows.Forms.TextBox txtOutputFolder;
        private System.Windows.Forms.Button btnBrowseOutput;

        private System.Windows.Forms.CheckBox chkUseSteamLaunch;
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
            btnBrowseEmulator = new Button();
            lblOutputFolder = new Label();
            txtOutputFolder = new TextBox();
            btnBrowseOutput = new Button();
            chkUseSteamLaunch = new CheckBox();
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
            tableLayoutPanel1.Controls.Add(btnBrowseEmulator, 2, 0);
            tableLayoutPanel1.Controls.Add(lblOutputFolder, 0, 1);
            tableLayoutPanel1.Controls.Add(txtOutputFolder, 1, 1);
            tableLayoutPanel1.Controls.Add(btnBrowseOutput, 2, 1);
            tableLayoutPanel1.Controls.Add(chkUseSteamLaunch, 0, 2);
            tableLayoutPanel1.Controls.Add(btnGenerate, 1, 3);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 3, 4, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(12);
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            tableLayoutPanel1.Size = new Size(700, 242);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // lblEmulatorPath
            // 
            lblEmulatorPath.AutoSize = true;
            lblEmulatorPath.Location = new Point(16, 12);
            lblEmulatorPath.Margin = new Padding(4, 0, 4, 0);
            lblEmulatorPath.Name = "lblEmulatorPath";
            lblEmulatorPath.Size = new Size(139, 15);
            lblEmulatorPath.TabIndex = 0;
            lblEmulatorPath.Text = "Select Pinball FX/M Path:";
            // 
            // txtEmulatorPath
            // 
            txtEmulatorPath.Dock = DockStyle.Fill;
            txtEmulatorPath.Location = new Point(224, 15);
            txtEmulatorPath.Margin = new Padding(4, 3, 4, 3);
            txtEmulatorPath.Name = "txtEmulatorPath";
            txtEmulatorPath.Size = new Size(364, 23);
            txtEmulatorPath.TabIndex = 1;
            // 
            // btnBrowseEmulator
            // 
            btnBrowseEmulator.Location = new Point(596, 15);
            btnBrowseEmulator.Margin = new Padding(4, 3, 4, 3);
            btnBrowseEmulator.Name = "btnBrowseEmulator";
            btnBrowseEmulator.Size = new Size(88, 27);
            btnBrowseEmulator.TabIndex = 2;
            btnBrowseEmulator.Text = "Browse...";
            btnBrowseEmulator.Click += BtnBrowseEmulator_Click;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.AutoSize = true;
            lblOutputFolder.Location = new Point(16, 46);
            lblOutputFolder.Margin = new Padding(4, 0, 4, 0);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(200, 15);
            lblOutputFolder.TabIndex = 3;
            lblOutputFolder.Text = "Select EmuVR/Games Output Folder:";
            // 
            // txtOutputFolder
            // 
            txtOutputFolder.Dock = DockStyle.Fill;
            txtOutputFolder.Location = new Point(224, 49);
            txtOutputFolder.Margin = new Padding(4, 3, 4, 3);
            txtOutputFolder.Name = "txtOutputFolder";
            txtOutputFolder.Size = new Size(364, 23);
            txtOutputFolder.TabIndex = 4;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Location = new Point(596, 49);
            btnBrowseOutput.Margin = new Padding(4, 3, 4, 3);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new Size(88, 27);
            btnBrowseOutput.TabIndex = 5;
            btnBrowseOutput.Text = "Browse...";
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            // 
            // chkUseSteamLaunch
            // 
            chkUseSteamLaunch.AutoSize = true;
            tableLayoutPanel1.SetColumnSpan(chkUseSteamLaunch, 3);
            chkUseSteamLaunch.Location = new Point(16, 88);
            chkUseSteamLaunch.Margin = new Padding(4, 8, 4, 8);
            chkUseSteamLaunch.Name = "chkUseSteamLaunch";
            chkUseSteamLaunch.Size = new Size(255, 18);
            chkUseSteamLaunch.TabIndex = 6;
            chkUseSteamLaunch.Text = "Use Steam -applaunch instead of direct EXE";
            // 
            // btnGenerate
            // 
            btnGenerate.Anchor = AnchorStyles.Top;
            btnGenerate.AutoSize = true;
            btnGenerate.Location = new Point(362, 117);
            btnGenerate.Margin = new Padding(4, 3, 4, 3);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(88, 25);
            btnGenerate.TabIndex = 8;
            btnGenerate.Text = "Generate";
            btnGenerate.Click += BtnGenerate_Click;
            // 
            // PinballFxForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 242);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "PinballFxForm";
            Text = "Pinball FX/M Capture Core Settings";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }
    }
}
