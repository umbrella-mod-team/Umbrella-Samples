using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MAMEInteropWindow;  // your interop namespace

namespace MAMEHookVR
{
    public partial class Form1 : Form
    {
        // Interop and I/O
        private MAMEInterop interop;
        private string outputFolder;
        private readonly string headerText;
        // State tracking
        private readonly List<string> logLines = new();
        private readonly Dictionary<int, string> pidToRom = new();
        private readonly HashSet<string> activeRoms = new();
        // === New Dictionaries for validation ===
        private readonly Dictionary<int, string> pendingLampValidations = new();
        private readonly Queue<(int id, int state)> pendingUpdatesQueue = new();
        private readonly Dictionary<string, Dictionary<string, int>> romLampStates = new();
        private readonly Dictionary<int, string> lampNameMap = new();
        private int activeSessionCount = 0;
        // UI & single-instance
        private NotifyIcon trayIcon;
        private Mutex singleInstanceMutex;

        public Form1()
        {
            InitializeComponent();
            // Set the window‑corner icon from your EXE
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            // === Create & configure trayIcon ===
            trayIcon = new NotifyIcon
            {
                Icon = this.Icon,                        // your EXE’s icon
                Visible = Settings.Default.MinimizeToTray,
                ContextMenuStrip = new ContextMenuStrip()
            };
            bool createdNew;            // Enforce single instance
            singleInstanceMutex = new Mutex(true, "MamehookVR_SingleInstance", out createdNew);
            if (!createdNew)
            {
                //       MessageBox.Show(
                //        "Another instance of MAMEHookVR is already running.",
                //        "Instance already running",
                //            MessageBoxButtons.OK,
                //           MessageBoxIcon.Warning);
                Environment.Exit(0);
            }

            // Load saved settings
            outputFolder = Settings.Default.OutputFolder;
            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "outputs");
            Directory.CreateDirectory(outputFolder);

            // Build the immutable header
            headerText =
                "===============================================================\r\n" +
                "                    MAMEHOOK VR +                              \r\n" +
                "       MAME64.dll with updated Retroarch suppprt By TeamGT     \r\n" +
                "===============================================================\r\n" +
                "       Original MAME64.dll by [MAME Team / HeadKaze]           \r\n" +
                "===============================================================\r\n" +
                $"Output folder: {outputFolder}\r\n\r\n";

            // Show header once
            textBoxOutput.Text = headerText;

            // Wire up settings UI
            chkRawOutput.Checked = Settings.Default.RawOutput;
            chkRawOutput.CheckedChanged += (s, e) =>
            {
                Settings.Default.RawOutput = chkRawOutput.Checked;
                Settings.Default.Save();
            };
            chkMinimizeToTray.Checked = Settings.Default.MinimizeToTray;
            chkMinimizeToTray.CheckedChanged += (s, e) =>
            {
                Settings.Default.MinimizeToTray = chkMinimizeToTray.Checked;
                Settings.Default.Save();
            };
            // Designer’s trayIcon is already created – just set visibility & events:
            trayIcon.Visible = Settings.Default.MinimizeToTray;
            trayIcon.ContextMenuStrip.Items.Add("Restore", null, (s, e) => RestoreFromTray());
            trayIcon.DoubleClick += (s, e) => RestoreFromTray();

            // Minimize handling
            this.Resize += (s, e) =>
            {
                if (WindowState == FormWindowState.Minimized && chkMinimizeToTray.Checked)
                {
                    Hide();
                    trayIcon.Visible = true;
                }
            };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Keep this print
            AppendLine("Initializing MAMEInterop...");
            logLines.Add("Initializing MAMEInterop...");

            interop = new MAMEInterop(this);
            interop.MAMEStart += OnMAMEStart;
            interop.MAMEStop += OnMAMEStop;
            interop.MAMEOutput += OnMAMEOutput;
            interop.MAMECopydata += OnMAMECopydata;
            // true=TCP mode, false=WM_COPYDATA mode
            interop.Initialize(1, "ThunderRT6TextBox", true);
            // Keep this print
            AppendLine("init_mame Successful; now waiting for MAME Outputs...");
            logLines.Add("init_mame Successful; now waiting for MAME Outputs...");
        }

        private void btnSelectOutput_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                SelectedPath = outputFolder,
                Description = "Choose output folder for lamp snapshots"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                outputFolder = dlg.SelectedPath;
                Directory.CreateDirectory(outputFolder);
                AppendLine($"Output folder changed to: {outputFolder}");
                logLines.Add($"Output folder changed to: {outputFolder}");
                Settings.Default.OutputFolder = outputFolder;
                Settings.Default.Save();
            }
        }


        private void OnMAMEStart(object sender, MAMEEventArgs e)
        {
            // increment for each ROM-start
            activeSessionCount++;
            logLines.Add($"[MAME_START]: session count = {activeSessionCount}");
            string rom = e.ROMName ?? "UnknownROM";
            UpdateDisplay();
        }

        private void OnMAMEStop(object sender, EventArgs e)
        {
            // decrement and clamp at zero
            activeSessionCount = Math.Max(0, activeSessionCount - 1);
            logLines.Add($"[MAME_STOP]: session count = {activeSessionCount}");

            if (activeSessionCount == 0)
            {
                // fully clear everything when no sessions left
                logLines.Add("All sessions ended—clearing all ROMs and lamps");
                activeRoms.Clear();
                romLampStates.Clear();
                lampNameMap.Clear();
                pendingLampValidations.Clear();
                pendingUpdatesQueue.Clear();
            }
            else
            {
                logLines.Add($"[MAME_STOP]");
            }
            UpdateDisplay();
        }

        // === Modified OnMAMECopydata ===
        private void OnMAMECopydata(object sender, MAMECopydataEventArgs e)
        {
            // Temporarily store in pending validations
            pendingLampValidations[e.Id] = e.Name;
            logLines.Add($"[MAME_COPYDATA] ID={e.Id} = '{e.Name}'");
            // Send validation request back to MAME immediately
            interop.RequestLampName(e.Id);

            logLines.Add($"[MAME_COPYDATA][Validation Requested] ID={e.Id}, Name='{e.Name}'");
            UpdateDisplay();
        }

        // === Modified OnMAMEOutput ===
        private void OnMAMEOutput(object sender, MAMEOutputEventArgs e)
        {
            string name = e.Name;
            if (int.TryParse(name, out var id))
            {
                if (lampNameMap.ContainsKey(id))
                {
                    name = lampNameMap[id];
                }
                else
                {
                    // Queue the update for later processing
                    pendingUpdatesQueue.Enqueue((id, e.State));
                    logLines.Add($"[MAME_OUTPUT][Update Deferred] ID={id}, State={e.State}, waiting for validation.");
                    interop.RequestLampName(id); // Retry validation request
                    return;
                }
            }

            // Regular update processing
            logLines.Add($"[MAME_OUTPUT] {name} = {e.State}");

            foreach (var rom in activeRoms)
            {
                romLampStates[rom][name] = e.State;
                FlushSnapshot(rom);
            }
            UpdateDisplay();
        }


        // === New handler for lamp validation confirmation ===
        private void OnLampValidationResponse(int id, string confirmedName)
        {
            if (pendingLampValidations.TryGetValue(id, out var pendingName))
            {
                if (pendingName == confirmedName)
                {
                    lampNameMap[id] = confirmedName;
                    logLines.Add($"[Validation Confirmed] ID={id}, Name='{confirmedName}' added to lamps.");
                }
                else
                {
                    logLines.Add($"[Validation Mismatch] ID={id}, Pending='{pendingName}', Confirmed='{confirmedName}'");
                }

                pendingLampValidations.Remove(id);
            }
            else
            {
                logLines.Add($"[Validation Error] Unexpected validation for ID={id}, Name='{confirmedName}'");
            }

            // Process any pending updates
            ProcessPendingUpdates();
            UpdateDisplay();
        }

        // === Process Pending Updates ===
        private void ProcessPendingUpdates()
        {
            int queueCount = pendingUpdatesQueue.Count;

            for (int i = 0; i < queueCount; i++)
            {
                var update = pendingUpdatesQueue.Dequeue();

                if (lampNameMap.TryGetValue(update.id, out var confirmedName))
                {
                    logLines.Add($"[Pending Update Processed] ID={update.id}, Name='{confirmedName}', State={update.state}");
                    foreach (var rom in activeRoms)
                    {
                        romLampStates[rom][confirmedName] = update.state;
                        FlushSnapshot(rom);
                    }
                }
                else
                {
                    // Still pending, re-queue
                    pendingUpdatesQueue.Enqueue(update);
                }
            }
        }

        private void FlushSnapshot(string rom)
        {
            var path = Path.Combine(outputFolder, rom + ".txt");
            using var ofs = new StreamWriter(path, false);
            foreach (var kv in romLampStates[rom])
                ofs.WriteLine($"{kv.Key} = {kv.Value}");
        }
        private void UpdateDisplay()
        {
            if (chkRawOutput.Checked)
            {
                // raw scrolling log
                if (logLines.Count > 0)
                    textBoxOutput.AppendText(logLines[logLines.Count - 1] + Environment.NewLine);
            }
            else
            {
                // full dashboard rebuild
                RedrawDashboard();
            }
        }


        private void RedrawDashboard()
        {
            var sb = new StringBuilder();
            sb.Append(headerText);
            sb.AppendLine("==== Dashboard ====");
            sb.AppendLine("Active ROMs:");
            foreach (var r in activeRoms) sb.AppendLine("  " + r);
            sb.AppendLine();
            sb.AppendLine("Active Outputs:");
            var all = new Dictionary<string, int>();
            foreach (var kv in romLampStates)
                foreach (var lamp in kv.Value)
                    all[lamp.Key] = lamp.Value;
            foreach (var lamp in all)
                sb.AppendLine($"  {lamp.Key} = {lamp.Value}");
            sb.AppendLine("===================");
            textBoxOutput.Text = sb.ToString();
        }

        private void AppendLine(string line)
        {
            textBoxOutput.AppendText(line + Environment.NewLine);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            interop.Dispose();
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
        }

        private void panelTop_Paint(object sender, PaintEventArgs e)
        {
            // reserved
        }
    }
}