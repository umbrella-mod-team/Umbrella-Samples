namespace CaptureCoreCompanion
{
    partial class CaptureCoreCompanion
    {
        private System.ComponentModel.IContainer components = null;

        // Control declarations – each is declared only once.
        private System.Windows.Forms.Label lblSystem;
        private System.Windows.Forms.ComboBox comboSystems;
        private System.Windows.Forms.Label lblEmulatorPath;
        private System.Windows.Forms.TextBox txtEmulatorPath;
        private System.Windows.Forms.Button btnSelectEmulator;
        private System.Windows.Forms.Label lblCommand;
        private System.Windows.Forms.TextBox txtCommand;
        private System.Windows.Forms.Label lblExtensions;
        private System.Windows.Forms.TextBox txtExtensions;
        private System.Windows.Forms.Label lblGameSystem;
        private System.Windows.Forms.TextBox txtGameSystem;
        private System.Windows.Forms.Label lblInputFolder;
        private System.Windows.Forms.TextBox txtInputFolder;
        private System.Windows.Forms.Button btnInputFolder;
        private System.Windows.Forms.Label lblOutputFolder;
        private System.Windows.Forms.TextBox txtOutputFolder;
        private System.Windows.Forms.Button btnOutputFolder;
        private System.Windows.Forms.CheckBox chkShortPath;
        private System.Windows.Forms.Button btnGenerate;
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Initialize the form components.
        /// </summary>
        private void InitializeComponent()
        {
            lblSystem = new Label();
            comboSystems = new ComboBox();
            lblEmulatorPath = new Label();
            txtEmulatorPath = new TextBox();
            btnSelectEmulator = new Button();
            lblCommand = new Label();
            txtCommand = new TextBox();
            lblExtensions = new Label();
            txtExtensions = new TextBox();
            lblGameSystem = new Label();
            txtGameSystem = new TextBox();
            lblInputFolder = new Label();
            txtInputFolder = new TextBox();
            btnInputFolder = new Button();
            lblOutputFolder = new Label();
            txtOutputFolder = new TextBox();
            btnOutputFolder = new Button();
            chkShortPath = new CheckBox();
            btnGenerate = new Button();
            panelHost = new Panel();
            panelHost.SuspendLayout();
            SuspendLayout();
            // 
            // lblSystem
            // 
            lblSystem.Location = new Point(20, 23);
            lblSystem.Name = "lblSystem";
            lblSystem.Size = new Size(120, 23);
            lblSystem.TabIndex = 0;
            lblSystem.Text = "Select System:";
            // 
            // comboSystems
            // 
            comboSystems.DropDownStyle = ComboBoxStyle.DropDownList;
            comboSystems.Location = new Point(150, 20);
            comboSystems.Name = "comboSystems";
            comboSystems.Size = new Size(400, 23);
            comboSystems.TabIndex = 1;
            comboSystems.SelectedIndexChanged += ComboSystems_SelectedIndexChanged;
            // 
            // lblEmulatorPath
            // 
            lblEmulatorPath.Location = new Point(20, 60);
            lblEmulatorPath.Name = "lblEmulatorPath";
            lblEmulatorPath.Size = new Size(120, 23);
            lblEmulatorPath.TabIndex = 2;
            lblEmulatorPath.Text = "Select Emulator (.exe):";
            // 
            // txtEmulatorPath
            // 
            txtEmulatorPath.Location = new Point(150, 60);
            txtEmulatorPath.Name = "txtEmulatorPath";
            txtEmulatorPath.Size = new Size(300, 23);
            txtEmulatorPath.TabIndex = 3;
            // 
            // btnSelectEmulator
            // 
            btnSelectEmulator.Location = new Point(460, 60);
            btnSelectEmulator.Name = "btnSelectEmulator";
            btnSelectEmulator.Size = new Size(75, 23);
            btnSelectEmulator.TabIndex = 4;
            btnSelectEmulator.Text = "Browse";
            btnSelectEmulator.UseVisualStyleBackColor = true;
            btnSelectEmulator.Click += SelectEmulator_Click;
            // 
            // lblCommand
            // 
            lblCommand.Location = new Point(20, 100);
            lblCommand.Name = "lblCommand";
            lblCommand.Size = new Size(120, 23);
            lblCommand.TabIndex = 5;
            lblCommand.Text = "Additional Command-Line Commands:";
            // 
            // txtCommand
            // 
            txtCommand.Location = new Point(150, 100);
            txtCommand.Name = "txtCommand";
            txtCommand.Size = new Size(400, 23);
            txtCommand.TabIndex = 6;
            // 
            // lblExtensions
            // 
            lblExtensions.Location = new Point(20, 140);
            lblExtensions.Name = "lblExtensions";
            lblExtensions.Size = new Size(120, 23);
            lblExtensions.TabIndex = 7;
            lblExtensions.Text = "File Extensions (e.g., .zip, .chd):";
            // 
            // txtExtensions
            // 
            txtExtensions.Location = new Point(150, 140);
            txtExtensions.Name = "txtExtensions";
            txtExtensions.Size = new Size(400, 23);
            txtExtensions.TabIndex = 8;
            // 
            // lblGameSystem
            // 
            lblGameSystem.Location = new Point(20, 180);
            lblGameSystem.Name = "lblGameSystem";
            lblGameSystem.Size = new Size(120, 23);
            lblGameSystem.TabIndex = 9;
            lblGameSystem.Text = "EMUVR System Name:";
            // 
            // txtGameSystem
            // 
            txtGameSystem.Location = new Point(150, 180);
            txtGameSystem.Name = "txtGameSystem";
            txtGameSystem.Size = new Size(400, 23);
            txtGameSystem.TabIndex = 10;
            // 
            // lblInputFolder
            // 
            lblInputFolder.Location = new Point(20, 220);
            lblInputFolder.Name = "lblInputFolder";
            lblInputFolder.Size = new Size(120, 23);
            lblInputFolder.TabIndex = 11;
            lblInputFolder.Text = "Select Input ROMs Folder:";
            // 
            // txtInputFolder
            // 
            txtInputFolder.Location = new Point(150, 220);
            txtInputFolder.Name = "txtInputFolder";
            txtInputFolder.Size = new Size(300, 23);
            txtInputFolder.TabIndex = 12;
            // 
            // btnInputFolder
            // 
            btnInputFolder.Location = new Point(460, 220);
            btnInputFolder.Name = "btnInputFolder";
            btnInputFolder.Size = new Size(75, 23);
            btnInputFolder.TabIndex = 13;
            btnInputFolder.Text = "Browse";
            btnInputFolder.UseVisualStyleBackColor = true;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.Location = new Point(20, 260);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(120, 23);
            lblOutputFolder.TabIndex = 14;
            lblOutputFolder.Text = "Select Output Folder:";
            // 
            // txtOutputFolder
            // 
            txtOutputFolder.Location = new Point(150, 260);
            txtOutputFolder.Name = "txtOutputFolder";
            txtOutputFolder.Size = new Size(300, 23);
            txtOutputFolder.TabIndex = 15;
            // 
            // btnOutputFolder
            // 
            btnOutputFolder.Location = new Point(460, 260);
            btnOutputFolder.Name = "btnOutputFolder";
            btnOutputFolder.Size = new Size(75, 23);
            btnOutputFolder.TabIndex = 16;
            btnOutputFolder.Text = "Browse";
            btnOutputFolder.UseVisualStyleBackColor = true;
            btnOutputFolder.Click += btnOutputFolder_Click;
            // 
            // chkShortPath
            // 
            chkShortPath.Location = new Point(150, 300);
            chkShortPath.Name = "chkShortPath";
            chkShortPath.Size = new Size(200, 24);
            chkShortPath.TabIndex = 17;
            chkShortPath.Text = "Use short ROM path in .bat files";
            chkShortPath.UseVisualStyleBackColor = true;
            // 
            // btnGenerate
            // 
            btnGenerate.Anchor = AnchorStyles.Top;
            btnGenerate.Location = new Point(218, 290);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(120, 30);
            btnGenerate.TabIndex = 18;
            btnGenerate.Text = "Generate";
            btnGenerate.UseVisualStyleBackColor = true;
            btnGenerate.Click += btnGenerate_Click;
            // 
            // panelHost
            // 
            panelHost.Controls.Add(btnGenerate);
            panelHost.Dock = DockStyle.Bottom;
            panelHost.Location = new Point(0, 50);
            panelHost.Name = "panelHost";
            panelHost.Size = new Size(560, 350);
            panelHost.TabIndex = 0;
            // 
            // CaptureCoreCompanion
            // 
            ClientSize = new Size(560, 400);
            Controls.Add(lblSystem);
            Controls.Add(comboSystems);
            Controls.Add(lblEmulatorPath);
            Controls.Add(txtEmulatorPath);
            Controls.Add(btnSelectEmulator);
            Controls.Add(lblCommand);
            Controls.Add(txtCommand);
            Controls.Add(lblExtensions);
            Controls.Add(txtExtensions);
            Controls.Add(lblGameSystem);
            Controls.Add(txtGameSystem);
            Controls.Add(lblInputFolder);
            Controls.Add(txtInputFolder);
            Controls.Add(btnInputFolder);
            Controls.Add(lblOutputFolder);
            Controls.Add(txtOutputFolder);
            Controls.Add(btnOutputFolder);
            Controls.Add(chkShortPath);
            Controls.Add(panelHost);
            Name = "CaptureCoreCompanion";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Capture Core Companion";
            Load += CaptureCoreCompanion_Load;
            panelHost.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        // Renamed helper method to avoid ambiguity.
        private void OpenFolderDialog(System.Windows.Forms.TextBox target)
        {
            // Implementation should open a FolderBrowserDialog and set target.Text accordingly.
        }
    }
}
