using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MAMEInteropTest
{
    public partial class Form1 : Form
    {
        private MAMEInterop m_MAMEInterop = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
			m_MAMEInterop = new MAMEInterop(this);

			m_MAMEInterop.MAMEStart += OnMAMEStart;
			m_MAMEInterop.MAMEStop += OnMAMEStop;
			m_MAMEInterop.MAMEOutput += OnMAMEOutput;

			m_MAMEInterop.Initialize(2, "C#Test", false);
        }

		private void OnMAMEStart(object sender, MAMEEventArgs e)
		{
			textBox1.AppendText(String.Format("OnMAMEStart: {0}", e.ROMName) + Environment.NewLine);
		}

		private void OnMAMEStop(object sender, EventArgs e)
		{
			textBox1.AppendText("OnMAMEStop" + Environment.NewLine);
		}

		private void OnMAMEOutput(object sender, MAMEOutputEventArgs e)
		{
			textBox1.AppendText(String.Format("OnMAMEOutput Name: {0} State: {1}", e.Name, e.State) + Environment.NewLine);
		}

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
			m_MAMEInterop.Dispose();
        }

		private void Form1_KeyUp(object sender, KeyEventArgs e)
		{
			switch(e.KeyCode)
			{
				case Keys.F1:
					m_MAMEInterop.PauseMAME(1);
					break;
				case Keys.F2:
					m_MAMEInterop.PauseMAME(0);
					break;
				case Keys.F3:
					m_MAMEInterop.SaveState(1);
					break;
				case Keys.F4:
					m_MAMEInterop.SaveState(0);
					break;
			}
		}
    }
}