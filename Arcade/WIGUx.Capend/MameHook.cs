using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Capend
{
    public class NamedPipeServer
    {
        private string pipeName;
        private Thread serverThread;
        private StreamWriter writer;
        public volatile bool HasConnection = false;

        public NamedPipeServer(string pipeName) { this.pipeName = pipeName; }

        public void Start()
        {
            serverThread = new Thread(PipeThread) { IsBackground = true };
            serverThread.Start();
        }
        private void PipeThread()
        {
            while (true)
            {
                using (var server = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                {
                    server.WaitForConnection();
                    lock (this)
                    {
                        writer = new StreamWriter(server) { AutoFlush = true };
                    }
                    DebugLog("[Pipe] Connected to Unity client.");
                    
                    while (server.IsConnected)
                    {
                        Thread.Sleep(500);
                    }
                    lock (this)
                    {
                        writer = null;
                    }
                    DebugLog("[Pipe] Unity client disconnected.");
                }
            }
        }

        // Use this to send messages to the Unity client
        public void Send(string message)
        {
            try
            {
                if (writer != null)
                {
                    writer.WriteLine(message);
                    // No Flush needed! AutoFlush does it.
                }
            }
            catch (IOException)
            {
                DebugLog("[Pipe] Write failed: client likely disconnected.");
                // writer = null; // Already reset in finally
            }
            catch (ObjectDisposedException)
            {
                // Writer already cleaned up, ignore.
            }
        }

        private void DebugLog(string msg)
        {
            Console.WriteLine(msg); // or your logging system
            Send("log|" + msg);
        }
    }


    public class MameHookWorker
    {
        private readonly Dictionary<int, string> pendingLampValidations = new Dictionary<int, string>();
        private readonly Dictionary<int, string> lampNameMap = new Dictionary<int, string>();
        private readonly Queue<KeyValuePair<int, int>> pendingUpdatesQueue = new Queue<KeyValuePair<int, int>>();
        private readonly Dictionary<string, Dictionary<string, int>> LampRegistry = new Dictionary<string, Dictionary<string, int>>();
        private readonly HashSet<string> activeRoms = new HashSet<string>();
        private readonly Dictionary<int, string> pidToRom = new Dictionary<int, string>();

        private int activeSessionCount = 0;
        private string lastRom = null;
        private static Dictionary<string, int> lastLampStates = new Dictionary<string, int>();

        private readonly NamedPipeServer pipe;

        private delegate int MAME_START(IntPtr hwnd);
        private delegate int MAME_STOP();
        private delegate int MAME_COPYDATA(int id, IntPtr name);
        private delegate int MAME_OUTPUT(IntPtr name, int value);

        private MAME_START startDel;
        private MAME_STOP stopDel;
        private MAME_COPYDATA copydataDel;
        private MAME_OUTPUT outputDel;

        [DllImport("kernel32", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);
        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("MAME64.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "init_mame")]
        private static extern int init_mame64(int clientId, string name, MAME_START start, MAME_STOP stop, MAME_COPYDATA copydata, MAME_OUTPUT output, bool useNetworkOutput);
        [DllImport("MAME64.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "message_mame")]
        private static extern int message_mame64(int id, int value);
        [DllImport("MAME64.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "close_mame")]
        private static extern int close_mame64();

        public MameHookWorker(NamedPipeServer pipe) { this.pipe = pipe; }

        private void DebugLog(string msg)
        {
            Console.WriteLine(msg); // or use your logger
            pipe.Send("log|" + msg);
        }

        public void Run()
        {
            DebugLog("Awake: setting up delegates before init");

            startDel = OnMameStart;
            stopDel = OnMameStop;
            copydataDel = OnMameCopydata;
            outputDel = OnMameOutput;

            string dir = Path.GetDirectoryName(typeof(MameHookWorker).Assembly.Location);
            SetDllDirectory(dir);
            var lib = LoadLibrary(Path.Combine(dir, "MAME64.dll"));
            DebugLog(lib != IntPtr.Zero ? "[MAME_HOOK] Loaded MAME64.dll" : "[mamehook] Failed to load (err " + Marshal.GetLastWin32Error() + ")");

            int hwnd = init_mame64(1, "Capend", startDel, stopDel, copydataDel, outputDel, true);
            DebugLog("[MAME_HOOK] init_mame64 returned " + hwnd);

            int portResult = message_mame64(2, 8000); // Or your actual port number
            DebugLog("[MAME_HOOK] message_mame64(SET_PORT,8000) returned " + portResult);

            Thread.Sleep(Timeout.Infinite);
        }

        private int OnMameStart(IntPtr hwnd)
        {
            activeSessionCount++;
            DebugLog("[MAME_START] Game Start Detected");
            DebugLog("[MAME_START] session count = " + activeSessionCount);
            return 1;
        }

        private int OnMameStop()
        {
            // decrement and clamp at zero
            activeSessionCount = Math.Max(0, activeSessionCount - 1);
            DebugLog("[MAME_STOP] session count = " + activeSessionCount);

            if (activeSessionCount == 0)
            {
                DebugLog("[MAME_STOP] All sessions ended, all lamps zeroed and state cleared.");
                activeRoms.Clear();
                LampRegistry.Clear();
                lampNameMap.Clear();
                pendingLampValidations.Clear();
                pendingUpdatesQueue.Clear();
                foreach (var lamps in LampRegistry.Values)
                    foreach (var key in lamps.Keys.ToList())
                        lamps[key] = 0;
                // Set all lamp states to zero

                return 1; // NO EXIT HERE!
            }
            else
            {
                DebugLog("[MAME_STOP]");
            }

            return 1;

        }

        private int OnMameCopydata(int id, IntPtr namePtr)
        {
            string name = Marshal.PtrToStringAnsi(namePtr) ?? "";
            DebugLog("[MAME_COPYDATA] CALLBACK id=" + id + " name='" + name + "'");

            if (id == 0)
            {
                lastRom = name;
                pidToRom[System.Diagnostics.Process.GetCurrentProcess().Id] = name;
                if (!activeRoms.Contains(name))
                {
                    activeRoms.Add(name);
                    if (!LampRegistry.ContainsKey(name))
                        LampRegistry[name] = new Dictionary<string, int>();
                    DebugLog("[MAME_START]: " + name + " (PID=" + System.Diagnostics.Process.GetCurrentProcess().Id + ")");
                }
                else
                {
                    DebugLog("[MAME_START][Ignored duplicate rom]: " + name);
                }
            }
            else
            {
                if (!pendingLampValidations.ContainsKey(id))
                {
                    pendingLampValidations[id] = name;
                    DebugLog("[MAME_COPYDATA] ID=" + id + " Name='" + name + "' [pending, requesting validation]");
                    RequestLampName(id);
                }
                else
                {
                    string pendingName = pendingLampValidations[id];
                    if (pendingName == name)
                    {
                        lampNameMap[id] = name;
                        DebugLog("[MAME_COPYDATA][Validation Confirmed] ID=" + id + ", Name='" + name + "'");
                    }
                    else
                    {
                        DebugLog("[MAME_COPYDATA][Validation MISMATCH] ID=" + id + ", pending='" + pendingName + "' confirmed='" + name + "'");
                    }
                    pendingLampValidations.Remove(id);
                    ProcessPendingUpdates();
                }
            }
            return 1;
        }
        private int OnMameOutput(IntPtr namePtr, int state)
        {
            string name = Marshal.PtrToStringAnsi(namePtr) ?? "";
            string lampName = name;
            string rom = lastRom ?? "";

            // Filter: only log if different from last value!
            bool shouldLog = true;
            string lastKey = lampName;
            lock (lastLampStates)
            {
                if (lastLampStates.ContainsKey(lastKey) && lastLampStates[lastKey] == state)
                {
                    shouldLog = false; // Do not log or send duplicate
                }
                else
                {
                    lastLampStates[lastKey] = state; // Update cache
                }
            }
            if (!shouldLog) return 1;

            int id;
            if (int.TryParse(name, out id))
            {
                if (lampNameMap.ContainsKey(id))
                {
                    lampName = lampNameMap[id];
                }
                else
                {
                    pendingUpdatesQueue.Enqueue(new KeyValuePair<int, int>(id, state));
                    DebugLog("[MAME_OUTPUT][Update Deferred] ID=" + id + ", State=" + state + ", waiting for validation.");
                    RequestLampName(id);
                    return 1;
                }
            }

            foreach (var r in activeRoms)
            {
                if (!LampRegistry.ContainsKey(r))
                    LampRegistry[r] = new Dictionary<string, int>();
                LampRegistry[r][lampName] = state;
                SendLampUpdateToPipe(r, lampName, state);
            }
            DebugLog("[MAME_OUTPUT] " + lampName + " = " + state);
            return 1;
        }

        private void SendLampUpdateToPipe(string rom, string lamp, int state)
        {
            var msg = "lamp_update|" + rom + "|" + lamp + "|" + state;
           // DebugLog("[DEBUG][PIPE_SEND] " + msg);
            pipe.Send(msg);
        }

        private void RequestLampName(int lampId)
        {
            DebugLog("[LampValidation] Request validation for ID=" + lampId);
        }

        private void ProcessPendingUpdates()
        {
            int processed = 0;
            int maxLoops = pendingUpdatesQueue.Count + 10;
            while (pendingUpdatesQueue.Count > 0 && processed < maxLoops)
            {
                var update = pendingUpdatesQueue.Dequeue();
                int id = update.Key;
                int state = update.Value;
                if (lampNameMap.ContainsKey(id))
                {
                    string confirmedName = lampNameMap[id];
                    foreach (var r in activeRoms)
                    {
                        if (!LampRegistry.ContainsKey(r))
                            LampRegistry[r] = new Dictionary<string, int>();
                        LampRegistry[r][confirmedName] = state;
                        SendLampUpdateToPipe(r, confirmedName, state);
                    }
                    DebugLog("[Pending Update Processed] ID=" + id + ", Name='" + confirmedName + "', State=" + state);
                }
                else
                {
                    pendingUpdatesQueue.Enqueue(update);
                }
                processed++;
            }
            if (processed >= maxLoops)
                DebugLog("[ProcessPendingUpdates] Max loop safety break triggered, check for stuck IDs.");
        }
    }

    public static class MameHookEntry
    {
            public static void RunMameHook(bool emuvrMode)
            {
                var pipeServer = new NamedPipeServer("MameHelperPipe");
                pipeServer.Start();
                var worker = new MameHookWorker(pipeServer);

                // No timer, no monitoring, no auto-exit—just stay open!
                worker.Run();
            }
        }
    }