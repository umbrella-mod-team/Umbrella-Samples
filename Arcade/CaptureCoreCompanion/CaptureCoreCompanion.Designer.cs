namespace CaptureCoreCompanion
{
    partial class CaptureCoreCompanion
    {
        private System.ComponentModel.IContainer components = null;

        // Control declarations – each is declared only once.


        // add these fields near the top of the Designer class:
        private System.Windows.Forms.Panel panelHeader;

        private System.Windows.Forms.TableLayoutPanel tableHeader;
        private System.Windows.Forms.TableLayoutPanel tableGeneric;
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
        private System.Windows.Forms.FlowLayoutPanel panelCheckboxes;
        private System.Windows.Forms.CheckBox chkShortPath;
        private System.Windows.Forms.CheckBox chkRelativePaths;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Panel panelFooter;
        private System.Windows.Forms.FlowLayoutPanel panelFooterFlow;
        private System.Windows.Forms.Panel panelGeneric;
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
            chkRelativePaths = new CheckBox();
            btnGenerate = new Button();
            panelHeader = new Panel();
            tableHeader = new TableLayoutPanel();
            panelGeneric = new Panel();
            tableGeneric = new TableLayoutPanel();
            panelGenerateRow = new FlowLayoutPanel();
            panelHost = new Panel();
            panelHeader.SuspendLayout();
            tableHeader.SuspendLayout();
            panelGeneric.SuspendLayout();
            tableGeneric.SuspendLayout();
            panelGenerateRow.SuspendLayout();
            SuspendLayout();
            // 
            // lblSystem
            // 
            lblSystem.AutoSize = true;
            lblSystem.Location = new Point(15, 12);
            lblSystem.Name = "lblSystem";
            lblSystem.Size = new Size(82, 15);
            lblSystem.TabIndex = 0;
            lblSystem.Text = "Select System:";
            // 
            // comboSystems
            // 
            comboSystems.Dock = DockStyle.Fill;
            comboSystems.DropDownStyle = ComboBoxStyle.DropDownList;
            comboSystems.Location = new Point(103, 15);
            comboSystems.Name = "comboSystems";
            comboSystems.Size = new Size(882, 23);
            comboSystems.TabIndex = 1;
            comboSystems.SelectedIndexChanged += ComboSystems_SelectedIndexChanged;
            // 
            // lblEmulatorPath
            // 
            lblEmulatorPath.AutoSize = true;
            lblEmulatorPath.Location = new Point(15, 8);
            lblEmulatorPath.Name = "lblEmulatorPath";
            lblEmulatorPath.Size = new Size(123, 15);
            lblEmulatorPath.TabIndex = 0;
            lblEmulatorPath.Text = "Select Emulator (.exe):";
            // 
            // txtEmulatorPath
            // 
            txtEmulatorPath.Dock = DockStyle.Fill;
            txtEmulatorPath.Location = new Point(238, 11);
            txtEmulatorPath.Name = "txtEmulatorPath";
            txtEmulatorPath.Size = new Size(666, 23);
            txtEmulatorPath.TabIndex = 1;
            // 
            // btnSelectEmulator
            // 
            btnSelectEmulator.AutoSize = true;
            btnSelectEmulator.Location = new Point(910, 11);
            btnSelectEmulator.Name = "btnSelectEmulator";
            btnSelectEmulator.Size = new Size(75, 24);
            btnSelectEmulator.TabIndex = 2;
            btnSelectEmulator.Text = "Browse";
            btnSelectEmulator.Click += SelectEmulator_Click;
            // 
            // lblCommand
            // 
            lblCommand.AutoSize = true;
            lblCommand.Location = new Point(15, 38);
            lblCommand.Name = "lblCommand";
            lblCommand.Size = new Size(217, 15);
            lblCommand.TabIndex = 3;
            lblCommand.Text = "Additional Command-Line Commands:";
            // 
            // txtCommand
            // 
            tableGeneric.SetColumnSpan(txtCommand, 2);
            txtCommand.Dock = DockStyle.Fill;
            txtCommand.Location = new Point(238, 41);
            txtCommand.Name = "txtCommand";
            txtCommand.Size = new Size(747, 23);
            txtCommand.TabIndex = 4;
            // 
            // lblExtensions
            // 
            lblExtensions.AutoSize = true;
            lblExtensions.Location = new Point(15, 68);
            lblExtensions.Name = "lblExtensions";
            lblExtensions.Size = new Size(169, 15);
            lblExtensions.TabIndex = 5;
            lblExtensions.Text = "File Extensions (e.g., .zip, .chd):";
            // 
            // txtExtensions
            // 
            tableGeneric.SetColumnSpan(txtExtensions, 2);
            txtExtensions.Dock = DockStyle.Fill;
            txtExtensions.Location = new Point(238, 71);
            txtExtensions.Name = "txtExtensions";
            txtExtensions.Size = new Size(747, 23);
            txtExtensions.TabIndex = 6;
            // 
            // lblGameSystem
            // 
            lblGameSystem.AutoSize = true;
            lblGameSystem.Location = new Point(15, 98);
            lblGameSystem.Name = "lblGameSystem";
            lblGameSystem.Size = new Size(125, 15);
            lblGameSystem.TabIndex = 7;
            lblGameSystem.Text = "EMUVR System Name:";
            // 
            // txtGameSystem
            // 
            tableGeneric.SetColumnSpan(txtGameSystem, 2);
            txtGameSystem.Dock = DockStyle.Fill;
            txtGameSystem.Location = new Point(238, 101);
            txtGameSystem.Name = "txtGameSystem";
            txtGameSystem.Size = new Size(747, 23);
            txtGameSystem.TabIndex = 8;
            // 
            // lblInputFolder
            // 
            lblInputFolder.AutoSize = true;
            lblInputFolder.Location = new Point(15, 128);
            lblInputFolder.Name = "lblInputFolder";
            lblInputFolder.Size = new Size(143, 15);
            lblInputFolder.TabIndex = 9;
            lblInputFolder.Text = "Select Input ROMs Folder:";
            // 
            // txtInputFolder
            // 
            txtInputFolder.Dock = DockStyle.Fill;
            txtInputFolder.Location = new Point(238, 131);
            txtInputFolder.Name = "txtInputFolder";
            txtInputFolder.Size = new Size(666, 23);
            txtInputFolder.TabIndex = 10;
            // 
            // btnInputFolder
            // 
            btnInputFolder.AutoSize = true;
            btnInputFolder.Location = new Point(910, 131);
            btnInputFolder.Name = "btnInputFolder";
            btnInputFolder.Size = new Size(75, 24);
            btnInputFolder.TabIndex = 11;
            btnInputFolder.Text = "Browse";
            btnInputFolder.Click += btnInputFolder_Click;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.AutoSize = true;
            lblOutputFolder.Location = new Point(15, 158);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(118, 15);
            lblOutputFolder.TabIndex = 12;
            lblOutputFolder.Text = "Select Output Folder:";
            // 
            // txtOutputFolder
            // 
            txtOutputFolder.Dock = DockStyle.Fill;
            txtOutputFolder.Location = new Point(238, 161);
            txtOutputFolder.Name = "txtOutputFolder";
            txtOutputFolder.Size = new Size(666, 23);
            txtOutputFolder.TabIndex = 13;
            // 
            // btnOutputFolder
            // 
            btnOutputFolder.AutoSize = true;
            btnOutputFolder.Location = new Point(910, 161);
            btnOutputFolder.Name = "btnOutputFolder";
            btnOutputFolder.Size = new Size(75, 24);
            btnOutputFolder.TabIndex = 14;
            btnOutputFolder.Text = "Browse";
            btnOutputFolder.Click += btnOutputFolder_Click;
            // 
            // chkShortPath
            // 
            chkShortPath.AutoSize = true;
            chkShortPath.Location = new Point(98, 3);
            chkShortPath.Name = "chkShortPath";
            chkShortPath.Size = new Size(168, 19);
            chkShortPath.TabIndex = 1;
            chkShortPath.Text = "Only use ROM name in bat";
            // 
            // chkRelativePaths
            // 
            chkRelativePaths.AutoSize = true;
            chkRelativePaths.Location = new Point(272, 3);
            chkRelativePaths.Name = "chkRelativePaths";
            chkRelativePaths.Size = new Size(153, 19);
            chkRelativePaths.TabIndex = 2;
            chkRelativePaths.Text = "Try to use Relative Paths";
            // 
            // btnGenerate
            // 
            btnGenerate.AutoSize = true;
            btnGenerate.Location = new Point(3, 3);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(89, 29);
            btnGenerate.TabIndex = 0;
            btnGenerate.Text = "Generate";
            btnGenerate.Click += btnGenerate_Click;
            // 
            // panelHeader
            // 
            panelHeader.AutoSize = true;
            panelHeader.Controls.Add(tableHeader);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(1000, 42);
            panelHeader.TabIndex = 2;
            // 
            // tableHeader
            // 
            tableHeader.AutoSize = true;
            tableHeader.ColumnCount = 2;
            tableHeader.ColumnStyles.Add(new ColumnStyle());
            tableHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableHeader.Controls.Add(lblSystem, 0, 0);
            tableHeader.Controls.Add(comboSystems, 1, 0);
            tableHeader.Dock = DockStyle.Top;
            tableHeader.Location = new Point(0, 0);
            tableHeader.Name = "tableHeader";
            tableHeader.Padding = new Padding(12, 12, 12, 0);
            tableHeader.RowCount = 1;
            tableHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableHeader.Size = new Size(1000, 42);
            tableHeader.TabIndex = 0;
            // 
            // panelGeneric
            // 
            panelGeneric.AutoSize = true;
            panelGeneric.Controls.Add(tableGeneric);
            panelGeneric.Dock = DockStyle.Top;
            panelGeneric.Location = new Point(0, 42);
            panelGeneric.Name = "panelGeneric";
            panelGeneric.Size = new Size(1000, 229);
            panelGeneric.TabIndex = 1;
            // 
            // tableGeneric
            // 
            tableGeneric.AutoSize = true;
            tableGeneric.ColumnCount = 3;
            tableGeneric.ColumnStyles.Add(new ColumnStyle());
            tableGeneric.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableGeneric.ColumnStyles.Add(new ColumnStyle());
            tableGeneric.Controls.Add(lblEmulatorPath, 0, 0);
            tableGeneric.Controls.Add(txtEmulatorPath, 1, 0);
            tableGeneric.Controls.Add(btnSelectEmulator, 2, 0);
            tableGeneric.Controls.Add(lblCommand, 0, 1);
            tableGeneric.Controls.Add(txtCommand, 1, 1);
            tableGeneric.Controls.Add(lblExtensions, 0, 2);
            tableGeneric.Controls.Add(txtExtensions, 1, 2);
            tableGeneric.Controls.Add(lblGameSystem, 0, 3);
            tableGeneric.Controls.Add(txtGameSystem, 1, 3);
            tableGeneric.Controls.Add(lblInputFolder, 0, 4);
            tableGeneric.Controls.Add(txtInputFolder, 1, 4);
            tableGeneric.Controls.Add(btnInputFolder, 2, 4);
            tableGeneric.Controls.Add(lblOutputFolder, 0, 5);
            tableGeneric.Controls.Add(txtOutputFolder, 1, 5);
            tableGeneric.Controls.Add(btnOutputFolder, 2, 5);
            tableGeneric.Controls.Add(panelGenerateRow, 0, 6);
            tableGeneric.Dock = DockStyle.Top;
            tableGeneric.Location = new Point(0, 0);
            tableGeneric.Name = "tableGeneric";
            tableGeneric.Padding = new Padding(12, 8, 12, 0);
            tableGeneric.RowCount = 7;
            tableGeneric.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableGeneric.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableGeneric.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableGeneric.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableGeneric.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableGeneric.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableGeneric.RowStyles.Add(new RowStyle());
            tableGeneric.Size = new Size(1000, 229);
            tableGeneric.TabIndex = 0;
            tableGeneric.Paint += tableGeneric_Paint;
            // 
            // panelGenerateRow
            // 
            panelGenerateRow.Anchor = AnchorStyles.None;
            panelGenerateRow.AutoSize = true;
            tableGeneric.SetColumnSpan(panelGenerateRow, 3);
            panelGenerateRow.Controls.Add(btnGenerate);
            panelGenerateRow.Controls.Add(chkShortPath);
            panelGenerateRow.Controls.Add(chkRelativePaths);
            panelGenerateRow.Location = new Point(286, 191);
            panelGenerateRow.Name = "panelGenerateRow";
            panelGenerateRow.Size = new Size(428, 35);
            panelGenerateRow.TabIndex = 15;
            panelGenerateRow.WrapContents = false;
            // 
            // panelHost
            // 
            panelHost.Dock = DockStyle.Fill;
            panelHost.Location = new Point(0, 271);
            panelHost.Name = "panelHost";
            panelHost.Size = new Size(1000, 39);
            panelHost.TabIndex = 0;
            panelHost.Paint += panelHost_Paint;
            // 
            // CaptureCoreCompanion
            // 
            ClientSize = new Size(1000, 310);
            Controls.Add(panelHost);
            Controls.Add(panelGeneric);
            Controls.Add(panelHeader);
            Name = "CaptureCoreCompanion";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Capture Core Companion v1.57   - By TeamGT";
            Load += CaptureCoreCompanion_Load;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            tableHeader.ResumeLayout(false);
            tableHeader.PerformLayout();
            panelGeneric.ResumeLayout(false);
            panelGeneric.PerformLayout();
            tableGeneric.ResumeLayout(false);
            tableGeneric.PerformLayout();
            panelGenerateRow.ResumeLayout(false);
            panelGenerateRow.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        // Renamed helper method to avoid ambiguity.
        private void OpenFolderDialog(System.Windows.Forms.TextBox target)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                    target.Text = fbd.SelectedPath;
            }
        }
        private TableLayoutPanel tableLayout;
        private FlowLayoutPanel panelGenerateRow;
        private Panel panelHost;
    }
}
