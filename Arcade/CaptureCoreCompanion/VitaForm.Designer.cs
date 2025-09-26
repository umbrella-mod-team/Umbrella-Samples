// VitaForm.Designer.cs
namespace CaptureCoreCompanion
{
    partial class VitaForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;

        private System.Windows.Forms.Label lblVita3KPath;
        private System.Windows.Forms.TextBox txtVita3KPath;
        private System.Windows.Forms.Button btnVita3KBrowse;

        private System.Windows.Forms.Label lblOutputFolder;
        private System.Windows.Forms.TextBox txtOutputFolder;
        private System.Windows.Forms.Button btnOutputBrowse;
        private System.Windows.Forms.Button btnGenerate;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            lblVita3KPath = new Label();
            txtVita3KPath = new TextBox();
            btnVita3KBrowse = new Button();
            lblOutputFolder = new Label();
            txtOutputFolder = new TextBox();
            btnOutputBrowse = new Button();
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
            tableLayoutPanel1.Controls.Add(lblVita3KPath, 0, 0);
            tableLayoutPanel1.Controls.Add(txtVita3KPath, 1, 0);
            tableLayoutPanel1.Controls.Add(btnVita3KBrowse, 2, 0);
            tableLayoutPanel1.Controls.Add(lblOutputFolder, 0, 1);
            tableLayoutPanel1.Controls.Add(txtOutputFolder, 1, 1);
            tableLayoutPanel1.Controls.Add(btnOutputBrowse, 2, 1);
            tableLayoutPanel1.Controls.Add(btnGenerate, 1, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 3, 4, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(12);
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            tableLayoutPanel1.Size = new Size(700, 208);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // lblVita3KPath
            // 
            lblVita3KPath.AutoSize = true;
            lblVita3KPath.Location = new Point(16, 12);
            lblVita3KPath.Margin = new Padding(4, 0, 4, 0);
            lblVita3KPath.Name = "lblVita3KPath";
            lblVita3KPath.Size = new Size(191, 15);
            lblVita3KPath.TabIndex = 0;
            lblVita3KPath.Text = "Select Vita3k Emulator (Vita3K.exe):";
            // 
            // txtVita3KPath
            // 
            txtVita3KPath.Dock = DockStyle.Fill;
            txtVita3KPath.Location = new Point(224, 15);
            txtVita3KPath.Margin = new Padding(4, 3, 4, 3);
            txtVita3KPath.Name = "txtVita3KPath";
            txtVita3KPath.Size = new Size(364, 23);
            txtVita3KPath.TabIndex = 1;
            // 
            // btnVita3KBrowse
            // 
            btnVita3KBrowse.AutoSize = true;
            btnVita3KBrowse.Location = new Point(596, 15);
            btnVita3KBrowse.Margin = new Padding(4, 3, 4, 3);
            btnVita3KBrowse.Name = "btnVita3KBrowse";
            btnVita3KBrowse.Size = new Size(88, 25);
            btnVita3KBrowse.TabIndex = 2;
            btnVita3KBrowse.Text = "Browse...";
            btnVita3KBrowse.Click += BtnVita3KBrowse_Click;
            // 
            // lblOutputFolder
            // 
            lblOutputFolder.AutoSize = true;
            lblOutputFolder.Location = new Point(16, 44);
            lblOutputFolder.Margin = new Padding(4, 0, 4, 0);
            lblOutputFolder.Name = "lblOutputFolder";
            lblOutputFolder.Size = new Size(200, 15);
            lblOutputFolder.TabIndex = 3;
            lblOutputFolder.Text = "Select EmuVR/Games Output Folder:";
            // 
            // txtOutputFolder
            // 
            txtOutputFolder.Dock = DockStyle.Fill;
            txtOutputFolder.Location = new Point(224, 47);
            txtOutputFolder.Margin = new Padding(4, 3, 4, 3);
            txtOutputFolder.Name = "txtOutputFolder";
            txtOutputFolder.Size = new Size(364, 23);
            txtOutputFolder.TabIndex = 4;
            // 
            // btnOutputBrowse
            // 
            btnOutputBrowse.AutoSize = true;
            btnOutputBrowse.Location = new Point(596, 47);
            btnOutputBrowse.Margin = new Padding(4, 3, 4, 3);
            btnOutputBrowse.Name = "btnOutputBrowse";
            btnOutputBrowse.Size = new Size(88, 25);
            btnOutputBrowse.TabIndex = 5;
            btnOutputBrowse.Text = "Browse...";
            btnOutputBrowse.Click += BtnOutputBrowse_Click;
            // 
            // btnGenerate
            // 
            btnGenerate.Anchor = AnchorStyles.Top;
            btnGenerate.AutoSize = true;
            btnGenerate.Location = new Point(362, 82);
            btnGenerate.Margin = new Padding(4, 3, 4, 3);
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Size = new Size(88, 30);
            btnGenerate.TabIndex = 7;
            btnGenerate.Text = "Generate";
            btnGenerate.Click += BtnGenerate_Click;
            // 
            // VitaForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 208);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "VitaForm";
            Text = "PlayStation Vita Capture Core Settings";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
    }
}
