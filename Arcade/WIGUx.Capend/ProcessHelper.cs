
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

    static class ProcessHelper
    {
        public static void RemoveChildProcess(string id)
        {
            if (!int.TryParse(id, out int processId))
            {
                LogHelper.Debug("Uso: programa <ProcessId>");
                return;
            }

            var childProcesses = GetChildProcesses(processId);

            LogHelper.Debug($"Found {childProcesses.Count} child process..");

            foreach (var child in childProcesses)
            {
                LogHelper.Debug($"Killing {child} process..");
                try
                {
                    child.Kill();
                    LogHelper.Debug($"Done.");
                }
                catch (Exception ex)
                {
                    LogHelper.Debug($"Error trying to kill the process: " + ex.ToString());
                }
            }
        }

        public static List<Process> GetChildProcesses(int parentId)
        {
            var childProcesses = new List<Process>();
            try
            {
                var searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessId={parentId}");
                var results = searcher.Get();

                foreach (var result in results)
                {
                    int childProcessId = Convert.ToInt32(result["ProcessId"]);
                    var process = Process.GetProcessById(childProcessId);
                    if (process.ProcessName != "conhost")
                    {
                        childProcesses.Add(process);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug("Error obteniendo procesos hijo: " + ex.Message);
            }

            if (childProcesses.Count == 0)
            {
                var process = Process.GetProcessById(parentId);
                if (process != null)
                {
                    childProcesses.Add(process);
                }
            }

            return childProcesses;
        }
    }
