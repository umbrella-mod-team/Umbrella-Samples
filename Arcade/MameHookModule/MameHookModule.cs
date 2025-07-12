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
using System.Collections;

namespace WIGUx.Modules.MameHookModule
{
    public class MameHookController : MonoBehaviour
    {
        private static MameHookController instance;
        private static readonly IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();

        public static Dictionary<string, int> LampRegistry = new Dictionary<string, int>();
        public static HashSet<string> activeRoms = new HashSet<string>();
        public static List<string> ActiveRomsList => activeRoms.ToList();
        public static List<string> currentLampState
        {
            get
            {
                // 1) Take a locked snapshot of the registry
                List<KeyValuePair<string, int>> snapshot;
                lock (LampRegistry)
                {
                    snapshot = LampRegistry.ToList();
                }

                // 2) Project that snapshot to your “key|value” strings
                return snapshot
                    .Select(kv => $"{kv.Key}|{kv.Value}")
                    .ToList();
            }
        }

        private Process helperProcess;
        private CancellationTokenSource logCancelSource;
        private Task logReadTask;

        // Named pipe client thread
        private Thread pipeThread;
        private volatile bool stopPipe = false;

        void Awake()
        { 
            /*
            if (instance != null && instance != this)
            {
            logger.Debug("[MAMEHOOK] Extra MameHookController destroyed only one needed.");
                Destroy(this); // Only remove this script/component
                return;
            }
            */
            instance = this;
        }
        void Start()
        {
            // Start Capend helper process (this method checks for duplicates!)

            string thisAssembly = typeof(MameHookController).Assembly.Location;
            string baseDir = System.IO.Path.GetDirectoryName(thisAssembly);
            string dllPath = typeof(MameHookController).Assembly.Location;
            string modulesDir = System.IO.Path.GetDirectoryName(dllPath);
            string wiguxDir = System.IO.Path.GetDirectoryName(modulesDir);
            string capendExePath = System.IO.Path.Combine(wiguxDir, "Bin", "Capend.exe");
            EnsureHelperRunning(capendExePath, "mamehook");
            UnityEngine.Debug.Log("[MAMEHOOK] capendExePath: " + capendExePath);
            StartPipeClient();
          //  StartCoroutine(TryStartCapendWithRetry(capendExePath, "mamehook"));
        }
        void OnApplicationQuit()
        {
            MameHookController.ShutdownHelperProcess();
        }

        void OnDestroy()
        {
            stopPipe = true;
            pipeThread?.Join(500);
            FlushLamps(); // Only zero lamps, don't kill Capend.exe here
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
            int tries = 1;
            while (!stopPipe)
            {
                tries++;
                try
                {
                    Console.WriteLine($"[MAMEHOOK] PipeClientThread: Attempt {tries} to connect to pipe.");
                    using (var pipe = new NamedPipeClientStream(".", "MameHelperPipe", PipeDirection.In))
                    using (var reader = new StreamReader(pipe))
                    {
                        pipe.Connect(5000); // Wait max 2s for Capend.exe/pipe to be ready
                        Console.WriteLine("[MAMEHOOK] Connected to MameHooker pipe.");
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
                catch (TimeoutException)
                {
                    logger.Debug("[MAMEHOOK] Pipe connection timed out (Capend not running yet?) Retrying in 1s...");
                    break;
                    //Thread.Sleep(1000);
                }
                catch (IOException ex)
                {
                    logger.Error("[MAMEHOOK] IOException: " + ex.Message + " (retrying in 1s)");
                    break;
                    // Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    logger.Error("[MAMEHOOK] Exception: " + ex.Message + " (retrying in 1s)");
                    break;
                    // Thread.Sleep(1000);
                }
            }
        }


        private void ProcessPipeLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            logger.Debug("[MAMEHOOK] Pipe received line: " + line);
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
                        // ADD THIS:
                        if (!activeRoms.Contains(rom))
                            activeRoms.Add(rom);

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
                logger.Debug("[MAMEHOOK] " + line.Substring(4));
            }
            else
            {
                logger.Debug("[MAMEHOOK][Raw] " + line);
            }
        }

        private void UpdateLampState(string rom, string lamp, int state)
        {
            lock (LampRegistry)
            {
                LampRegistry[lamp] = state;
            }
            // Console.WriteLine($"[MAME_OUTPUT] {lamp} = {state}"); done in capend
        }
        public static void EnsureHelperRunning(string exePath, string exeArgs)
        {
            var exeName = System.IO.Path.GetFileNameWithoutExtension(exePath);
            // Check for existing Capend.exe processes
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
            else
            {
                UnityEngine.Debug.Log("[MAMEHOOK] Capend.exe is already running, will not start another instance.");
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

        public static void FlushLamps()
        {
            foreach (var lamp in LampRegistry.Keys.ToList())
            {
                LampRegistry[lamp] = 0;
            }
        }
        private IEnumerator TryStartCapendWithRetry(string exePath, string exeArgs, int maxAttempts = 10)
        {
            int attempts = 2;
            while (attempts < maxAttempts)
            {
                var exeName = System.IO.Path.GetFileNameWithoutExtension(exePath);
                var alreadyRunning = System.Diagnostics.Process.GetProcessesByName(exeName).Any();
                if (!alreadyRunning)
                {
                    bool error = false;
                    Exception lastEx = null;
                    try
                    {
                        logger.Debug($"[MAMEHOOK] Attempt {attempts + 1}: Trying to launch Capend.exe at {exePath}");
                        var psi = new System.Diagnostics.ProcessStartInfo(exePath, exeArgs)
                        {
                            UseShellExecute = false,
                            CreateNoWindow = false, //true,
                            WorkingDirectory = System.IO.Path.GetDirectoryName(exePath)
                        };
                        System.Diagnostics.Process.Start(psi);
                        logger.Debug("[MAMEHOOK] Capend.exe launched successfully.");
                        yield break;
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                        error = true;
                    }
                    if (error)
                    {
                        logger.Debug($"[MAMEHOOK] Launch failed: {lastEx.Message} (retrying in 1s, attempt {attempts + 1})");
                        yield return new WaitForSeconds(1f);
                        attempts++;
                    }
                }
                else
                {
                    logger.Debug("[MAMEHOOK] Capend.exe already running.");
                    yield break;
                }
            }
            logger.Debug("[MAMEHOOK] Failed to launch Capend.exe after multiple attempts.");
        }
    }
}