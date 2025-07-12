// TeknoParrotForm.Designer.cs
namespace CaptureCoreCompanion
{
    partial class TeknoParrotForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

        private System.Windows.Forms.Label lblTeknoParrotPath;
        private System.Windows.Forms.TextBox txtTeknoParrotPath;
        private System.Windows.Forms.Button btnTeknoParrotBrowse;

        private System.Windows.Forms.Label lblOutputFolder;
        private System.Windows.Forms.TextBox txtOutputFolder;
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
            lblTeknoParrotPath = new Label();
            txtTeknoParrotPath = new TextBox();
            btnTeknoParrotBrowse = new Button();
            lblOutputFolder = new Label();
            txtOutputFolder = new TextBox();
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
            tableLayoutPanel1.Controls.Add(lblTeknoParrotPath, 0, 0);
            tableLayoutPanel1.Controls.Add(txtTeknoParrotPath, 1, 0);
            tableLayoutPanel1.Controls.Add(btnTeknoParrotBrowse, 2, 0);
            tableLayoutPanel1.Controls.Add(lblOutputFolder, 0, 1);
            tableLayoutPanel1.Controls.Add(txtOutputFolder, 1, 1);
            tableLayoutPanel1.Controls.Add(btnOutputBrowse, 2, 1);
            tableLayoutPanel1.Controls.Add(lblInfo, 0, 2);
            tableLayoutPanel1.Controls.Add(btnGenerate, 1, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 3, 4, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(12);
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            tableLayoutPanel1.Size = new Size(700, 208);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // lblTeknoParrotPath
            // 
            lblTeknoParrotPath.AutoSize = true;
            lblTeknoParrotPath.Location = new Point(16, 12);
            lblTeknoParrotPath.Margin = new Padding(4, 0, 4, 0);
            lblTeknoParrotPath.Name = "lblTeknoParrotPath";
            lblTeknoParrotPath.Size = new Size(262, 15);
            lblTeknoParrotPath.TabIndex = 0;
            lblTeknoParrotPath.Text = "Select TeknoParrot Emulator (TeknoParrotUi.exe)";
            // 
            // txtTeknoParrotPath
            // 
            txtTeknoParrotPath.Dock = DockStyle.Fill;
            txtTeknoParrotPath.Location = new Point(286, 15);
            txtTeknoParrotPath.Margin = new Padding(4, 3, 4, 3);
            txtTeknoParrotPath.Name = "txtTeknoParrotPath";
            txtTeknoParrotPath.Size = new Size(302, 23);
            txtTeknoParrotPath.TabIndex = 1;
            // 
            // btnTeknoParrotBrowse
            // 
            btnTeknoParrotBrowse.AutoSize = true;
            btnTeknoParrotBrowse.Location = new Point(596, 15);
            btnTeknoParrotBrowse.Margin = new Padding(4, 3, 4, 3);
            btnTeknoParrotBrowse.Name = "btnTeknoParrotBrowse";
            btnTeknoParrotBrowse.Size = new Size(88, 25);
            btnTeknoParrotBrowse.TabIndex = 2;
            btnTeknoParrotBrowse.Text = "Browse...";
            btnTeknoParrotBrowse.Click += BtnTeknoParrotBrowse_Click;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.AutoSize = true;
            lblOutputFolder.Location = new Point(16, 45);
            lblOutputFolder.Margin = new Padding(4, 0, 4, 0);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(166, 15);
            lblOutputFolder.TabIndex = 3;
            lblOutputFolder.Text = "EmuVR/Games Output Folder:";
            // 
            // txtOutputFolder
            // 
            txtOutputFolder.Dock = DockStyle.Fill;
            txtOutputFolder.Location = new Point(286, 48);
            txtOutputFolder.Margin = new Padding(4, 3, 4, 3);
            txtOutputFolder.Name = "txtOutputFolder";
            txtOutputFolder.Size = new Size(302, 23);
            txtOutputFolder.TabIndex = 4;
            // 
            // btnOutputBrowse
            // 
            btnOutputBrowse.AutoSize = true;
            btnOutputBrowse.Location = new Point(596, 48);
            btnOutputBrowse.Margin = new Padding(4, 3, 4, 3);
            btnOutputBrowse.Name = "btnOutputBrowse";
            btnOutputBrowse.Size = new Size(88, 25);
            btnOutputBrowse.TabIndex = 5;
            btnOutputBrowse.Text = "Browse...";
            btnOutputBrowse.Click += BtnOutputBrowse_Click;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Location = new Point(16, 80);
            lblInfo.Margin = new Padding(4, 0, 4, 0);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(156, 15);
            lblInfo.TabIndex = 6;
            lblInfo.Text = "Generate Capture Core Files.";
            // 
            // btnGenerate
            // 
            btnGenerate.Anchor = AnchorStyles.Top;
            btnGenerate.AutoSize = true;
            btnGenerate.Location = new Point(393, 83);
            btnGenerate.Margin = new Padding(4, 3, 4, 3);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(88, 25);
            btnGenerate.TabIndex = 7;
            btnGenerate.Text = "Generate";
            btnGenerate.Click += BtnGenerate_Click;
            // 
            // TeknoParrotForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 208);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "TeknoParrotForm";
            Text = "TeknoParrot Capture Core Settings";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }
    }
}
