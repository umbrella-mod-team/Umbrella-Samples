using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Text;

namespace CaptureCoreCompanion
{
    public partial class CaptureCoreCompanion : Form
    {
        private Dictionary<string, SystemInfo> systemSettings = new Dictionary<string, SystemInfo>();
        private string emuVRPath = string.Empty;
        private Form activeSubform;
        private Panel panelHost;

        public CaptureCoreCompanion()
        {
            InitializeCustomComponents();
            AskForEmuVRLocation();
            LoadSystemSettings();
            PopulateComboBox();
        }

        /// <summary>
        /// Initialize UI components – updated to match the Python labels and add input/output folder controls.
        /// </summary>
        private void InitializeCustomComponents()
        {
            // System selection controls
            comboSystems = new ComboBox { Left = 150, Top = 20, Width = 400 };
            comboSystems.SelectedIndexChanged += new EventHandler(ComboSystems_SelectedIndexChanged);
            Label lblSystem = new Label { Left = 20, Top = 23, Width = 120, Text = "Select System:" };

            // Emulator selection controls
            Label lblEmulatorPath = new Label { Left = 20, Top = 60, Width = 120, Text = "Select Emulator (.exe):" };
            txtEmulatorPath = new TextBox { Left = 150, Top = 60, Width = 300 };
            Button btnSelectEmulator = new Button { Left = 460, Top = 60, Width = 75, Text = "Browse" };
            btnSelectEmulator.Click += SelectEmulator_Click;

            // Additional command-line commands (matches Python's label)
            Label lblCommand = new Label { Left = 20, Top = 100, Width = 120, Text = "Additional Command-Line Commands:" };
            txtCommand = new TextBox { Left = 150, Top = 100, Width = 400 };

            // File extensions entry
            Label lblExtensions = new Label { Left = 20, Top = 140, Width = 120, Text = "File Extensions (e.g., .zip, .chd):" };
            txtExtensions = new TextBox { Left = 150, Top = 140, Width = 400 };

            // EMUVR System Name entry
            Label lblGameSystem = new Label { Left = 20, Top = 180, Width = 120, Text = "EMUVR System Name:" };
            txtGameSystem = new TextBox { Left = 150, Top = 180, Width = 400 };

            // Input folder selection
            Label lblInputFolder = new Label { Left = 20, Top = 220, Width = 120, Text = "Select Input ROMs Folder:" };
            TextBox txtInputFolder = new TextBox { Left = 150, Top = 220, Width = 300, Name = "txtInputFolder" };
            Button btnInputFolder = new Button { Left = 460, Top = 220, Width = 75, Text = "Browse" };
            btnInputFolder.Click += (s, e) => { BrowseFolder(txtInputFolder); };

            // Output folder selection
            Label lblOutputFolder = new Label { Left = 20, Top = 260, Width = 120, Text = "Select Output Folder:" };
            TextBox txtOutputFolder = new TextBox { Left = 150, Top = 260, Width = 300, Name = "txtOutputFolder" };
            Button btnOutputFolder = new Button { Left = 460, Top = 260, Width = 75, Text = "Browse" };
            btnOutputFolder.Click += (s, e) => { BrowseFolder(txtOutputFolder); };

            // Checkbox for short path option
            chkShortPath = new CheckBox { Left = 150, Top = 300, Width = 200, Text = "Use short ROM path in .bat files" };

            // Generate button
            Button btnGenerate = new Button { Left = 150, Top = 340, Width = 120, Text = "Generate" };
            btnGenerate.Click += btnGenerate_Click;

            // Add controls to form
            Controls.Add(lblSystem);
            Controls.Add(comboSystems);
            Controls.Add(lblEmulatorPath);
            Controls.Add(txtEmulatorPath);
            Controls.Add(btnSelectEmulator);
            Controls.Add(lblCommand);
            Controls.Add(txtCommand);
            Controls.Add(lblExtensions);
            Controls.Add(txtExtensions);
            Controls.Add(lblGameSystem);
            Controls.Add(txtGameSystem);
            Controls.Add(lblInputFolder);
            Controls.Add(txtInputFolder);
            Controls.Add(btnInputFolder);
            Controls.Add(lblOutputFolder);
            Controls.Add(txtOutputFolder);
            Controls.Add(btnOutputFolder);
            Controls.Add(chkShortPath);
            Controls.Add(btnGenerate);

            // Panel host for special subforms
            panelHost = new Panel
            {
                Left = 0,
                Top = comboSystems.Bottom + 5,
                Width = 560,
                Height = 350,
                Visible = false
            };
            Controls.Add(panelHost);
            panelHost.BringToFront();

            // Set form properties
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(600, 400);
            this.Text = "Capture Core Companion 1.5 beta";
        }

        private void AskForEmuVRLocation()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select EmuVR.exe";
                ofd.Filter = "EmuVR Executable|EmuVR.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    emuVRPath = Path.GetDirectoryName(ofd.FileName);
                }
            }
        }
        private void LoadSystemSettings()
        {
            string filePath = "./data/systems.dat";

            // Special systems – leave these if you have extra handling.
            systemSettings["PS3 (RPCS3)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "PS3Form", XmlFile = "./data/ps3.xml" };
            systemSettings["Xbox 360 (Xenia)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "Xbox360Form", XmlFile = "./data/360.xml" };
            systemSettings["PSVita (Vita3k)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "VitaForm", XmlFile = "./data/vita.xml" };
            systemSettings["Arcade (TeknoParrot)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "TeknoParrotForm", XmlFile = "./data/teknoparrot.xml" };
            systemSettings["Arcade Sega Hikaru (Demul)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "HikaruForm", XmlFile = "./data/hikaru.xml" };
            systemSettings["Arcade Sega Model 2 (M2Emulator)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "Model2Form", XmlFile = "./data/segam2.xml" };
            systemSettings["Arcade Sega Model 3 (Supermodel)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "Model2Form", XmlFile = "./data/segam3.xml" };
            systemSettings["PinballFX Series (PinballFX)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "PinballFxForm", XmlFile = "./data/pinballfx.xml" };
            systemSettings["Microsoft XCloud (XCloud)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "XCloudForm", XmlFile = "./data/xcloud.dat" };
            systemSettings["MUGEN-OpenBor"] = new SystemInfo { IsSpecial = true, SpecialFormName = "MugenForm", XmlFile = ".xml" };
            systemSettings["EXE Program"] = new SystemInfo { IsSpecial = true, SpecialFormName = "ExeForm", XmlFile = ".xml" };
            if (File.Exists(filePath))
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    var parts = line.Split('\t');
                    if (parts.Length >= 6)
                    {
                        string system = parts[0].Trim();
                        systemSettings[system] = new SystemInfo
                        {
                            Command = parts[1].Trim(),
                            Extensions = parts[2].Trim().Split(','),
                            RecommendedExe = parts[3].Trim(),
                            ShortPathDefault = parts[4].Trim() == "1",
                            GameSystem = parts[5].Trim(),
                            IsSpecial = false
                        };
                    }
                }
            }
        }

        private void PopulateComboBox()
        {
            comboSystems.Items.Clear();
            foreach (var key in systemSettings.Keys)
            {
                comboSystems.Items.Add(key);
            }
            if (comboSystems.Items.Count > 0)
                comboSystems.SelectedIndex = 0;
        }

        /// <summary>
        /// Browse for a folder and set the provided TextBox's text.
        /// </summary>
        private void BrowseFolder(TextBox target)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    target.Text = fbd.SelectedPath;
                }
            }
        }
        private void btnInputFolder_Click(object sender, EventArgs e)
        {
            OpenFolderDialog(txtInputFolder);
        }

        private void btnOutputFolder_Click(object sender, EventArgs e)
        {
            OpenFolderDialog(txtOutputFolder);
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (comboSystems.SelectedItem == null)
                return;

            // Get current system settings (if not special)
            string selectedSystem = comboSystems.SelectedItem.ToString();
            if (!systemSettings.TryGetValue(selectedSystem, out SystemInfo info) || info.IsSpecial)
            {
                MessageBox.Show("Special system handling not implemented in this version.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Get values from controls
            string emulator = txtEmulatorPath.Text.Trim();
            string additionalCommands = txtCommand.Text.Trim();
            string[] extensions = txtExtensions.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(s => s.Trim()).ToArray();
            string gameSystem = txtGameSystem.Text.Trim();

            // Retrieve input/output folder paths by name
            TextBox txtInputFolder = Controls.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtInputFolder");
            TextBox txtOutputFolder = Controls.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtOutputFolder");
            if (txtInputFolder == null || txtOutputFolder == null)
            {
                MessageBox.Show("Input or output folder controls not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string inputFolder = txtInputFolder.Text.Trim();
            string outputFolder = txtOutputFolder.Text.Trim();
            bool useShortPath = chkShortPath.Checked;

            // Validate folders
            if (!Directory.Exists(inputFolder) || !Directory.Exists(outputFolder))
            {
                MessageBox.Show("Please select valid input and output folders.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Process each ROM file in the input folder that matches the provided extensions.
            foreach (string file in Directory.GetFiles(inputFolder))
            {
                if (extensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    string fileName = Path.GetFileName(file).Replace('/', '\\');
                    string romName = Path.GetFileNameWithoutExtension(file);
                    string sanitizedRomName = SanitizeFilename(romName);
                    string batFilePath = Path.Combine(outputFolder, sanitizedRomName + ".bat");

                    // Create .bat file content
                    string batContent;
                    if (useShortPath)
                    {
                        batContent = $"cd ./Games/Arcade (Capture)\r\ncd ../..\r\ncd .\\Emulators\\{Path.GetFileNameWithoutExtension(emulator)}\r\n\"{emulator}\" {additionalCommands} \"{fileName}\"";
                    }
                    else
                    {
                        string romPath = file.Replace('/', '\\');
                        batContent = $"cd ./Games/Arcade (Capture)\r\ncd ../..\r\ncd .\\Emulators\\{Path.GetFileNameWithoutExtension(emulator)}\r\n\"{emulator}\" {additionalCommands} \"{romPath}\"";
                    }

                    File.WriteAllText(batFilePath, batContent);

                    // Optionally, create a .win file (if needed)
                    string winFilePath = Path.Combine(outputFolder, sanitizedRomName + ".win");
                    string emulatorExeName = Path.GetFileName(emulator).Trim('"');     // e.g. "Dolphin.exe"

                    File.WriteAllText(winFilePath, emulatorExeName + Environment.NewLine);

                    // Generate emuvr_core.txt (one file per output folder)
                    string coreFilePath = Path.Combine(outputFolder, "emuvr_core.txt");
                    File.WriteAllText(coreFilePath,
                        $"media = \"{gameSystem}\"\r\n" +
                        $"core = \"wgc_libretro\"\r\n" +
                        $"noscanlines = \"true\"\r\n" +
                        $"aspect_ratio = \"auto\"\r\n");

                    // Generate emuvr_override_auto.cfg
                    string overrideFilePath = Path.Combine(outputFolder, "emuvr_override_auto.cfg");
                    File.WriteAllText(overrideFilePath,
                        "input_player1_analog_dpad_mode = \"0\"\r\n" +
                        "video_shader = \"shaders\\shaders_glsl\\stock.glslp\"\r\n" +
                        "video_threaded = \"false\"\r\n" +
                        "video_vsync = \"true\"\r\n");
                }
            }

            MessageBox.Show($"Capture Core files generated successfully.\nMedia set to {gameSystem}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// A helper to sanitize filenames by replacing invalid characters.
        /// </summary>
        private string SanitizeFilename(string filename)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, '-');
            }
            return filename.Trim();
        }

        private void SelectEmulator_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Executable Files|*.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtEmulatorPath.Text = ofd.FileName;
                }
            }
        }


        // (Optional) Update UI when system selection changes
        private void ComboSystems_SelectedIndexChanged(object sender, EventArgs e)
        {
            string key = comboSystems.SelectedItem?.ToString();
            if (key == null || !systemSettings.TryGetValue(key, out var info)) return;
            if (info.IsSpecial)
            {
                panelHost.Visible = true;
                panelHost.Controls.Clear();
                Form sub = null;
                switch (info.SpecialFormName)
                {
                    case "PS3Form": sub = new PS3Form(info.XmlFile); break;
                    case "Xbox360Form": sub = new Xbox360Form(info.XmlFile); break;
                    case "VitaForm": sub = new VitaForm(info.XmlFile); break;
                    case "TeknoParrotForm": sub = new TeknoParrotForm(info.XmlFile); break;
                    case "HikaruForm": sub = new HikaruForm(info.XmlFile); break;
                    case "Model2Form": sub = new Model2Form(info.XmlFile); break;
                    case "Model3Form": sub = new Model3Form(info.XmlFile); break;
                    case "PinballFxForm": sub = new PinballFxForm(info.XmlFile); break;
                    case "XCloudForm": sub = new XCloudForm(info.XmlFile); break;
                    case "MugenForm": sub = new MugenForm(); break;
                    case "ExeForm": sub = new ExeForm(); break;
                }
                if (sub != null)
                {
                    activeSubform = sub;
                    sub.TopLevel = false;
                    sub.FormBorderStyle = FormBorderStyle.None;
                    sub.Dock = DockStyle.Fill;
                    panelHost.Controls.Add(sub);
                    sub.Show();
                }
            }
            else
            {
                if (activeSubform != null)
                {
                    activeSubform.Close(); activeSubform = null;
                }
                panelHost.Controls.Clear(); panelHost.Visible = false;
                txtCommand.Text = info.Command;
                txtExtensions.Text = string.Join(", ", info.Extensions);
                txtEmulatorPath.Text = info.RecommendedExe;
                chkShortPath.Checked = info.ShortPathDefault;
                txtGameSystem.Text = info.GameSystem;
            }
        }

        // Class to hold system settings
        public class SystemInfo
        {
            public string Command { get; set; }
            public string[] Extensions { get; set; }
            public string RecommendedExe { get; set; }
            public bool ShortPathDefault { get; set; }
            public string GameSystem { get; set; }
            public bool IsSpecial { get; set; }
            public string SpecialFormName { get; set; }
            public string XmlFile { get; set; }
        }

        private void CaptureCoreCompanion_Load(object sender, EventArgs e)
        {

        }
    }
}