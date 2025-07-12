// MugenForm.Designer.cs
namespace CaptureCoreCompanion
{
    partial class MugenForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

        private System.Windows.Forms.Label lblInputFolder;
        private System.Windows.Forms.TextBox txtInputFolder;
        private System.Windows.Forms.Button btnBrowseInput;

        private System.Windows.Forms.Label lblCommands;
        private System.Windows.Forms.TextBox txtCommands;

        private System.Windows.Forms.Label lblOutputFolder;
        private System.Windows.Forms.TextBox txtOutputFolder;
        private System.Windows.Forms.Button btnBrowseOutput;
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
            lblInputFolder = new Label();
            txtInputFolder = new TextBox();
            btnBrowseInput = new Button();
            lblCommands = new Label();
            txtCommands = new TextBox();
            lblOutputFolder = new Label();
            txtOutputFolder = new TextBox();
            btnBrowseOutput = new Button();
            lblInfo = new Label();
            btnGenerate = new Button();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.AutoSize = true;
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.Controls.Add(lblInputFolder, 0, 0);
            tableLayoutPanel1.Controls.Add(txtInputFolder, 1, 0);
            tableLayoutPanel1.Controls.Add(btnBrowseInput, 2, 0);
            tableLayoutPanel1.Controls.Add(lblCommands, 0, 1);
            tableLayoutPanel1.Controls.Add(txtCommands, 1, 1);
            tableLayoutPanel1.Controls.Add(lblOutputFolder, 0, 2);
            tableLayoutPanel1.Controls.Add(txtOutputFolder, 1, 2);
            tableLayoutPanel1.Controls.Add(btnBrowseOutput, 2, 2);
            tableLayoutPanel1.Controls.Add(lblInfo, 0, 3);
            tableLayoutPanel1.Controls.Add(btnGenerate, 1, 3);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 3, 4, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(12);
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tableLayoutPanel1.Size = new Size(758, 277);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // lblInputFolder
            // 
            lblInputFolder.AutoSize = true;
            lblInputFolder.Location = new Point(16, 12);
            lblInputFolder.Margin = new Padding(4, 0, 4, 0);
            lblInputFolder.Name = "lblInputFolder";
            lblInputFolder.Size = new Size(147, 15);
            lblInputFolder.TabIndex = 0;
            lblInputFolder.Text = "Select Input Games Folder:";
            // 
            // txtInputFolder
            // 
            txtInputFolder.Dock = DockStyle.Fill;
            txtInputFolder.Location = new Point(241, 15);
            txtInputFolder.Margin = new Padding(4, 3, 4, 3);
            txtInputFolder.Name = "txtInputFolder";
            txtInputFolder.Size = new Size(405, 23);
            txtInputFolder.TabIndex = 1;
            // 
            // btnBrowseInput
            // 
            btnBrowseInput.AutoSize = true;
            btnBrowseInput.Location = new Point(654, 15);
            btnBrowseInput.Margin = new Padding(4, 3, 4, 3);
            btnBrowseInput.Name = "btnBrowseInput";
            btnBrowseInput.Size = new Size(88, 25);
            btnBrowseInput.TabIndex = 2;
            btnBrowseInput.Text = "Browse...";
            btnBrowseInput.Click += BtnBrowseInput_Click;
            // 
            // lblCommands
            // 
            lblCommands.AutoSize = true;
            lblCommands.Location = new Point(16, 44);
            lblCommands.Margin = new Padding(4, 0, 4, 0);
            lblCommands.Name = "lblCommands";
            lblCommands.Size = new Size(217, 15);
            lblCommands.TabIndex = 3;
            lblCommands.Text = "Additional Command-Line Commands:";
            // 
            // txtCommands
            // 
            txtCommands.Dock = DockStyle.Fill;
            txtCommands.Location = new Point(241, 47);
            txtCommands.Margin = new Padding(4, 3, 4, 3);
            txtCommands.Name = "txtCommands";
            txtCommands.Size = new Size(405, 23);
            txtCommands.TabIndex = 4;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.AutoSize = true;
            lblOutputFolder.Location = new Point(16, 75);
            lblOutputFolder.Margin = new Padding(4, 0, 4, 0);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(200, 15);
            lblOutputFolder.TabIndex = 5;
            lblOutputFolder.Text = "Select EmuVR/Games Output Folder:";
            // 
            // txtOutputFolder
            // 
            txtOutputFolder.Dock = DockStyle.Fill;
            txtOutputFolder.Location = new Point(241, 78);
            txtOutputFolder.Margin = new Padding(4, 3, 4, 3);
            txtOutputFolder.Name = "txtOutputFolder";
            txtOutputFolder.Size = new Size(405, 23);
            txtOutputFolder.TabIndex = 6;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Location = new Point(654, 78);
            btnBrowseOutput.Margin = new Padding(4, 3, 4, 3);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new Size(88, 27);
            btnBrowseOutput.TabIndex = 7;
            btnBrowseOutput.Text = "Browse...";
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Location = new Point(16, 108);
            lblInfo.Margin = new Padding(4, 0, 4, 0);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(156, 15);
            lblInfo.TabIndex = 12;
            lblInfo.Text = "Generate Capture Core Files.";
            // 
            // btnGenerate
            // 
            btnGenerate.Anchor = AnchorStyles.Top;
            btnGenerate.AutoSize = true;
            btnGenerate.Location = new Point(399, 111);
            btnGenerate.Margin = new Padding(4, 3, 4, 3);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(88, 25);
            btnGenerate.TabIndex = 8;
            btnGenerate.Text = "Generate";
            btnGenerate.Click += BtnGenerate_Click;
            // 
            // MugenForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(758, 277);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "MugenForm";
            Text = "M.U.G.E.N./OpenBor Capture Core Settings";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
