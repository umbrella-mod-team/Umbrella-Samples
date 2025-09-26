// XboxCloudForm.Designer.cs
namespace CaptureCoreCompanion
{
    partial class XCloudForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

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
            tableLayoutPanel1.Controls.Add(lblOutputFolder, 0, 0);
            tableLayoutPanel1.Controls.Add(txtOutputFolder, 1, 0);
            tableLayoutPanel1.Controls.Add(btnBrowseOutput, 2, 0);
            tableLayoutPanel1.Controls.Add(btnGenerate, 1, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 3, 4, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(12);
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            tableLayoutPanel1.Size = new Size(700, 162);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.AutoSize = true;
            lblOutputFolder.Location = new Point(16, 12);
            lblOutputFolder.Margin = new Padding(4, 0, 4, 0);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(220, 15);
            lblOutputFolder.TabIndex = 0;
            lblOutputFolder.Text = "Select Folder to Create XCloud Shortcuts";
            // 
            // txtOutputFolder
            // 
            txtOutputFolder.Dock = DockStyle.Fill;
            txtOutputFolder.Location = new Point(244, 15);
            txtOutputFolder.Margin = new Padding(4, 3, 4, 3);
            txtOutputFolder.Name = "txtOutputFolder";
            txtOutputFolder.Size = new Size(344, 23);
            txtOutputFolder.TabIndex = 1;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Location = new Point(596, 15);
            btnBrowseOutput.Margin = new Padding(4, 3, 4, 3);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new Size(88, 27);
            btnBrowseOutput.TabIndex = 2;
            btnBrowseOutput.Text = "Browse...";
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            // 
            // btnGenerate
            // 
            btnGenerate.Anchor = AnchorStyles.Top;
            btnGenerate.AutoSize = true;
            btnGenerate.Location = new Point(372, 48);
            btnGenerate.Margin = new Padding(4, 3, 4, 3);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(88, 25);
            btnGenerate.TabIndex = 4;
            btnGenerate.Text = "Generate";
            btnGenerate.Click += BtnGenerate_Click;
            // 
            // XCloudForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 162);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "XCloudForm";
            Text = "XCloud Capture Core Settings";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }
    }
}
