using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipes;
using WIGU;

namespace WIGUx.Modules.MameHookModule
{
    public class MameHookController : MonoBehaviour
    {
        private static MameHookController instance;
        private static readonly IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        public static Dictionary<string, Dictionary<string, int>> LampRegistry = new Dictionary<string, Dictionary<string, int>>();
        private HashSet<string> activeRoms = new HashSet<string>();

        public static List<string> ActiveRomsList => instance != null
            ? instance.activeRoms.ToList()
            : new List<string>();

        public static List<string> currentLampState => instance != null
            ? LampRegistry.SelectMany(r => r.Value.Select(kv => $"{kv.Key}|{kv.Value}")).ToList()
            : new List<string>();

        private Process helperProcess;
        private CancellationTokenSource logCancelSource;
        private Task logReadTask;

        // Named pipe client thread
        private Thread pipeThread;
        private volatile bool stopPipe = false;

        void Awake()
        {
            // Remove the singleton pattern here, or move any static init you really need.

            // Start Capend helper process (this method already checks for duplicates!)
            string thisAssembly = typeof(MameHookController).Assembly.Location;
            string baseDir = System.IO.Path.GetDirectoryName(thisAssembly);
            string dllPath = typeof(MameHookController).Assembly.Location;
            string modulesDir = System.IO.Path.GetDirectoryName(dllPath);
            string wiguxDir = System.IO.Path.GetDirectoryName(modulesDir);
            string capendExePath = System.IO.Path.Combine(wiguxDir, "Bin", "Capend.exe");
            EnsureHelperRunning(capendExePath, "mamehook");
            UnityEngine.Debug.Log("[MAMEHOOK] capendExePath: " + capendExePath);

            // Each instance starts its own named pipe client.
            StartPipeClient();
        }

        void OnDestroy()
        {
            // Only shutdown if this is the singleton
            if (instance == this)
            {
                stopPipe = true;
                pipeThread?.Join(500);
                ShutdownHelperAndFlushLamps();
            }
        }

        private void StartPipeClient()
        {
            if (pipeThread == null)
            {
                pipeThread = new Thread(PipeClientThread);
                pipeThread.IsBackground = true;
                pipeThread.Start();
            }
        }

        private void PipeClientThread()
        {
            while (!stopPipe)
            {
                try
                {
                    using (var pipe = new NamedPipeClientStream(".", "MameHelperPipe", PipeDirection.In))
                    using (var reader = new StreamReader(pipe))
                    {
                        pipe.Connect(); // Wait until Capend.exe is ready
                        logger.Debug("[MAMEHOOK] Connected to MameHooker pipe.");
                        while (pipe.IsConnected && !stopPipe)
                        {
                            string line = reader.ReadLine();
                            if (line != null)
                            {
                                ProcessPipeLine(line);
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    logger.Error("[MAMEHOOK] IOException: " + ex.Message + " (retrying in 1s)");
                    Thread.Sleep(1000); // Wait before reconnecting
                }
                catch (Exception ex)
                {
                    logger.Error("[MAMEHOOK] Exception: " + ex.Message + " (retrying in 1s)");
                    Thread.Sleep(1000);
                }
            }
        }

        private void ProcessPipeLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            // Example: lamp_update|rom|lamp|state
            if (line.StartsWith("lamp_update|"))
            {
                var parts = line.Split('|');
                if (parts.Length == 4)
                {
                    string rom = parts[1];
                    string lamp = parts[2];
                    int state;
                    if (int.TryParse(parts[3], out state))
                    {
                        UpdateLampState(rom, lamp, state);
                    }
                    else
                    {
                        logger.Error("[MAMEHOOK] Invalid lamp state: " + parts[3]);
                    }
                }
            }
            else if (line.StartsWith("log|"))
            {
                logger.Debug("[MAMEHOOK][Capend] " + line.Substring(4));
            }
            else
            {
                logger.Debug("[MAMEHOOK][CapendRaw] " + line);
            }
        }

        private void UpdateLampState(string rom, string lamp, int state)
        {
            lock (LampRegistry)
            {
                if (!LampRegistry.ContainsKey(rom))
                    LampRegistry[rom] = new Dictionary<string, int>();
                LampRegistry[rom][lamp] = state;
                activeRoms.Add(rom);
            }
            logger.Debug($"[LampUpdate] {rom}:{lamp}={state}");
        }

        public static void EnsureHelperRunning(string exePath, string exeArgs)
        {
            var exeName = System.IO.Path.GetFileNameWithoutExtension(exePath);
            var alreadyRunning = Process.GetProcessesByName(exeName).Any();
            if (!alreadyRunning)
            {
                var psi = new ProcessStartInfo(exePath, exeArgs)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
        }

        public static void ShutdownHelperProcess()
        {
            foreach (var proc in Process.GetProcessesByName("Capend"))
            {
                try { proc.Kill(); }
                catch { }
            }
        }

        public static void ShutdownHelperAndFlushLamps()
        {
            foreach (var rom in LampRegistry.Keys.ToList())
            {
                foreach (var lamp in LampRegistry[rom].Keys.ToList())
                {
                    LampRegistry[rom][lamp] = 0;
                }
            }
            foreach (var proc in Process.GetProcessesByName("capend")) // Use actual EXE name
            {
                try { proc.Kill(); }
                catch { }
            }
        }
    }
}
