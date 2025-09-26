// ExeForm.Designer.cs
namespace CaptureCoreCompanion
{
    partial class ExeForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

        private System.Windows.Forms.Label lblInputFile;
        private System.Windows.Forms.TextBox txtInputFile;
        private System.Windows.Forms.Button btnBrowseInput;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TextBox txtTitle;

        private System.Windows.Forms.Label lblCommands;
        private System.Windows.Forms.TextBox txtCommands;

        private System.Windows.Forms.Label lblOutputFolder;
        private System.Windows.Forms.TextBox txtOutputFolder;
        private System.Windows.Forms.Button btnBrowseOutput;
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
            lblInputFile = new Label();
            txtInputFile = new TextBox();
            btnBrowseInput = new Button();
            lblTitle = new Label();
            txtTitle = new TextBox();
            lblCommands = new Label();
            txtCommands = new TextBox();
            lblOutputFolder = new Label();
            txtOutputFolder = new TextBox();
            btnBrowseOutput = new Button();
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
            tableLayoutPanel1.Controls.Add(lblInputFile, 0, 0);
            tableLayoutPanel1.Controls.Add(txtInputFile, 1, 0);
            tableLayoutPanel1.Controls.Add(btnBrowseInput, 2, 0);
            tableLayoutPanel1.Controls.Add(lblTitle, 0, 1);
            tableLayoutPanel1.Controls.Add(txtTitle, 1, 1);
            tableLayoutPanel1.Controls.Add(lblCommands, 0, 2);
            tableLayoutPanel1.Controls.Add(txtCommands, 1, 2);
            tableLayoutPanel1.Controls.Add(lblOutputFolder, 0, 3);
            tableLayoutPanel1.Controls.Add(txtOutputFolder, 1, 3);
            tableLayoutPanel1.Controls.Add(btnBrowseOutput, 2, 3);
            tableLayoutPanel1.Controls.Add(btnGenerate, 1, 4);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 3, 4, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(12);
            tableLayoutPanel1.RowCount = 5;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 29F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 13F));
            tableLayoutPanel1.Size = new Size(758, 346);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // lblInputFile
            // 
            lblInputFile.AutoSize = true;
            lblInputFile.Location = new Point(16, 12);
            lblInputFile.Margin = new Padding(4, 0, 4, 0);
            lblInputFile.Name = "lblInputFile";
            lblInputFile.Size = new Size(149, 15);
            lblInputFile.TabIndex = 0;
            lblInputFile.Text = "Select Input Game EXE File:";
            // 
            // txtInputFile
            // 
            txtInputFile.Dock = DockStyle.Fill;
            txtInputFile.Location = new Point(241, 15);
            txtInputFile.Margin = new Padding(4, 3, 4, 3);
            txtInputFile.Name = "txtInputFile";
            txtInputFile.Size = new Size(405, 23);
            txtInputFile.TabIndex = 1;
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
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(16, 43);
            lblTitle.Margin = new Padding(4, 0, 4, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(168, 15);
            lblTitle.TabIndex = 3;
            lblTitle.Text = "Game Title (Shown in EmuVR):";
            // 
            // txtTitle
            // 
            txtTitle.Dock = DockStyle.Fill;
            txtTitle.Location = new Point(241, 46);
            txtTitle.Margin = new Padding(4, 3, 4, 3);
            txtTitle.Name = "txtTitle";
            txtTitle.Size = new Size(405, 23);
            txtTitle.TabIndex = 4;
            // 
            // lblCommands
            // 
            lblCommands.AutoSize = true;
            lblCommands.Location = new Point(16, 72);
            lblCommands.Margin = new Padding(4, 0, 4, 0);
            lblCommands.Name = "lblCommands";
            lblCommands.Size = new Size(217, 15);
            lblCommands.TabIndex = 5;
            lblCommands.Text = "Additional Command-Line Commands:";
            // 
            // txtCommands
            // 
            txtCommands.Dock = DockStyle.Fill;
            txtCommands.Location = new Point(241, 75);
            txtCommands.Margin = new Padding(4, 3, 4, 3);
            txtCommands.Name = "txtCommands";
            txtCommands.Size = new Size(405, 23);
            txtCommands.TabIndex = 6;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.AutoSize = true;
            lblOutputFolder.Location = new Point(16, 100);
            lblOutputFolder.Margin = new Padding(4, 0, 4, 0);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(200, 15);
            lblOutputFolder.TabIndex = 7;
            lblOutputFolder.Text = "Select EmuVR/Games Output Folder:";
            // 
            // txtOutputFolder
            // 
            txtOutputFolder.Dock = DockStyle.Fill;
            txtOutputFolder.Location = new Point(241, 103);
            txtOutputFolder.Margin = new Padding(4, 3, 4, 3);
            txtOutputFolder.Name = "txtOutputFolder";
            txtOutputFolder.Size = new Size(405, 23);
            txtOutputFolder.TabIndex = 8;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.AutoSize = true;
            btnBrowseOutput.Location = new Point(654, 103);
            btnBrowseOutput.Margin = new Padding(4, 3, 4, 3);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new Size(88, 25);
            btnBrowseOutput.TabIndex = 9;
            btnBrowseOutput.Text = "Browse...";
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            // 
            // btnGenerate
            // 
            btnGenerate.Anchor = AnchorStyles.Top;
            btnGenerate.AutoSize = true;
            btnGenerate.Location = new Point(399, 135);
            btnGenerate.Margin = new Padding(4, 3, 4, 3);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(88, 25);
            btnGenerate.TabIndex = 10;
            btnGenerate.Text = "Generate";
            btnGenerate.Click += BtnGenerate_Click;
            // 
            // ExeForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(758, 346);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "ExeForm";
            Text = "EXE Capture Core Settings";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }
    }
}
