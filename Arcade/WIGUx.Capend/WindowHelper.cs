
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

    static class WindowHelper
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        public static void SetWindowTitle(Process process, string newTitle)
        {
            // Cambiar el título de la ventana
            IntPtr hWnd = process.MainWindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                SetWindowText(hWnd, newTitle);
            }
            else
            {
                LogHelper.Debug($"El proceso '{process.Id}' no tiene una ventana principal.");
            }
        }

        public static void RenameWindow(string id, string windowTitle)
        {
            if (!int.TryParse(id, out int processId))
            {
                LogHelper.Debug($"error: parsing {id}");
                return;
            }

            const int timeout = 60000; // 1 minuto
            const int interval = 1000; // 1 segundo
            int elapsed = 0;
            bool found = false;
            while (elapsed < timeout && !found)
            {
                var childProcesses = ProcessHelper.GetChildProcesses(processId);
                LogHelper.Debug($"Found {childProcesses.Count} child process..");

                foreach (var child in childProcesses)
                {
                LogHelper.Debug($"Renaming{child.ProcessName}({child.Id}) process..");
                    try
                    {
                        SetWindowTitle(child, windowTitle);
                        found = true;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Debug($"Error trying to rename the process: " + ex.ToString());
                    }
                }

                Thread.Sleep(interval);
                elapsed += interval;
            }
        }

    }
