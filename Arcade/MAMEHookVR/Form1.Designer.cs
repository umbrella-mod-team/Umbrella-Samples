namespace MAMEHookVR
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnSelectOutput;
        private System.Windows.Forms.CheckBox chkMinimizeToTray;
        private System.Windows.Forms.CheckBox chkRawOutput;
        private System.Windows.Forms.TextBox textBoxOutput;


        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.chkMinimizeToTray = new System.Windows.Forms.CheckBox();
            this.btnSelectOutput = new System.Windows.Forms.Button();
            this.chkRawOutput = new System.Windows.Forms.CheckBox();
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.chkMinimizeToTray);
            this.panelTop.Controls.Add(this.btnSelectOutput);
            this.panelTop.Controls.Add(this.chkRawOutput);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(599, 30);
            this.panelTop.TabIndex = 0;
            // 
            // chkMinimizeToTray
            // 
            this.chkMinimizeToTray.AutoSize = true;
            this.chkMinimizeToTray.Location = new System.Drawing.Point(412, 7);
            this.chkMinimizeToTray.Name = "chkMinimizeToTray";
            this.chkMinimizeToTray.Size = new System.Drawing.Size(98, 17);
            this.chkMinimizeToTray.TabIndex = 2;
            this.chkMinimizeToTray.Text = "Minimize to tray";
            this.chkMinimizeToTray.UseVisualStyleBackColor = true;
            // 
            // btnSelectOutput
            // 
            this.btnSelectOutput.Location = new System.Drawing.Point(3, 2);
            this.btnSelectOutput.Name = "btnSelectOutput";
            this.btnSelectOutput.Size = new System.Drawing.Size(403, 24);
            this.btnSelectOutput.TabIndex = 0;
            this.btnSelectOutput.Text = "Select Output Folder…";
            this.btnSelectOutput.UseVisualStyleBackColor = true;
            this.btnSelectOutput.Click += new System.EventHandler(this.btnSelectOutput_Click);
            // 
            // chkRawOutput
            // 
            this.chkRawOutput.AutoSize = true;
            this.chkRawOutput.Location = new System.Drawing.Point(516, 7);
            this.chkRawOutput.Name = "chkRawOutput";
            this.chkRawOutput.Size = new System.Drawing.Size(83, 17);
            this.chkRawOutput.TabIndex = 1;
            this.chkRawOutput.Text = "Raw Output";
            this.chkRawOutput.UseVisualStyleBackColor = true;
            // 
            // textBoxOutput
            // 
            this.textBoxOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxOutput.Font = new System.Drawing.Font("Consolas", 10F);
            this.textBoxOutput.Location = new System.Drawing.Point(0, 30);
            this.textBoxOutput.Multiline = true;
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.ReadOnly = true;
            this.textBoxOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxOutput.Size = new System.Drawing.Size(599, 494);
            this.textBoxOutput.TabIndex = 2;
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(599, 524);
            this.Controls.Add(this.textBoxOutput);
            this.Controls.Add(this.panelTop);
            this.Name = "Form1";
            this.Text = "MAMEHook VR";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
