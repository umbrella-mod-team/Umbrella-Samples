using System;
using System.Windows.Forms;

namespace CaptureCoreCompanion
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CaptureCoreCompanion());  // Start the MainForm
        }
    }
}
