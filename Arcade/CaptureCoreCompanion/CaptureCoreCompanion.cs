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

        public CaptureCoreCompanion()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            AskForEmuVRLocation();
            LoadSystemSettings();
            PopulateComboBox();
        }

        /// <summary>
        /// Initialize UI components – updated to match the Python labels and add input/output folder controls.
        /// </summary>


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
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(exeDir, "data", "systems.dat");

            // Special systems (keep yours as-is)
            systemSettings["PS3 (RPCS3)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "PS3Form", XmlFile = "./data/ps3.xml", DefaultOutputFolder = "Sony PlayStation 3" };
            systemSettings["Xbox 360 (Xenia)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "Xbox360Form", XmlFile = "./data/360.xml", DefaultOutputFolder = "Microsoft Xbox 360" };
            systemSettings["PSVita (Vita3k)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "VitaForm", XmlFile = "./data/vita.xml", DefaultOutputFolder = "Sony Playstation Vita" };
            systemSettings["Arcade (TeknoParrot)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "TeknoParrotForm", XmlFile = "./data/teknoparrot.xml", DefaultOutputFolder = "Arcade (Capture)" };
            systemSettings["Arcade Sega Hikaru (Demul)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "HikaruForm", XmlFile = "./data/hikaru.xml", DefaultOutputFolder = "Arcade (Capture)" };
            systemSettings["Arcade Sega Model 2 (M2Emulator)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "Model2Form", XmlFile = "./data/segam2.xml", DefaultOutputFolder = "Arcade (Capture)" };
            systemSettings["Arcade Sega Model 3 (Supermodel)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "Model3Form", XmlFile = "./data/segam3.xml", DefaultOutputFolder = "Arcade (Capture)" };
            systemSettings["PinballFX Series (PinballFX)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "PinballFxForm", XmlFile = "./data/pinballfx.xml", DefaultOutputFolder = "Pinball FX" };
            systemSettings["Microsoft XCloud (XCloud)"] = new SystemInfo { IsSpecial = true, SpecialFormName = "XCloudForm", XmlFile = "./data/xcloud.dat", DefaultOutputFolder = "Microsoft Xbox One" };
            systemSettings["MUGEN-OpenBor"] = new SystemInfo { IsSpecial = true, SpecialFormName = "MugenForm", XmlFile = ".xml", DefaultOutputFolder = "MUGEN-OpenBor" };
            systemSettings["EXE Program"] = new SystemInfo { IsSpecial = true, SpecialFormName = "ExeForm", XmlFile = ".xml", DefaultOutputFolder = "EXE Program" };

            if (!File.Exists(filePath))
            {
                MessageBox.Show("systems.dat not found at:\n" + filePath,
                    "Missing systems.dat", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Split('\t');
                if (parts.Length >= 8)
                {
                    string system = parts[0].Trim();
                    systemSettings[system] = new SystemInfo
                    {
                        Command = parts[1].Trim(),
                        Extensions = parts[2].Trim().Split(',').Select(s => s.Trim()).ToArray(),
                        RecommendedExe = parts[3].Trim(),
                        ShortPathDefault = parts[4].Trim() == "1",
                        GameSystem = parts[5].Trim(),
                        DefaultOutputFolder = parts[6].Trim(),
                        IsSpecial = false,
                        DefaultRomFolder = parts[7].Trim()
                    };
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
                    string emulatorPath = MakePathRelativeIfInside(emulator, emuVRPath);
                    string romPath = MakePathRelativeIfInside(file, emuVRPath);

                    using (var w = new StreamWriter(batFilePath))
                    {
                        w.WriteLine("cd ./Games/Arcade (Capture)");
                        w.WriteLine("cd ../..");
                        w.WriteLine($"cd .\\Emulators\\{Path.GetFileNameWithoutExtension(emulatorPath)}");

                        if (useShortPath)
                        {
                            w.WriteLine($"\"{emulatorPath}\" {additionalCommands} \"{fileName}\"");
                        }
                        else
                        {
                            w.WriteLine($"\"{emulatorPath}\" {additionalCommands} \"{romPath}\"");
                        }
                    }
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

        // updated to handle special systems with subforms
        private void ComboSystems_SelectedIndexChanged(object sender, EventArgs e)
        {
            string key = comboSystems.SelectedItem?.ToString();
            if (key == null || !systemSettings.TryGetValue(key, out var info)) return;

            if (info.IsSpecial)
            {
                // hide generic UI, show special subform
                panelGeneric.Visible = false;
                panelHost.Visible = true;

                panelHost.Controls.Clear();
                Form sub = null;
                switch (info.SpecialFormName)
                {
                    case "PS3Form": sub = new PS3Form(info.XmlFile, emuVRPath, info.DefaultOutputFolder); break;
                    case "Xbox360Form": sub = new Xbox360Form(info.XmlFile, emuVRPath, info.DefaultOutputFolder); break;
                    case "VitaForm": sub = new VitaForm(info.XmlFile, emuVRPath, info.DefaultOutputFolder); break;
                    case "TeknoParrotForm": sub = new TeknoParrotForm(info.XmlFile, emuVRPath, info.DefaultOutputFolder); break;
                    case "HikaruForm": sub = new HikaruForm(info.XmlFile, emuVRPath, info.DefaultOutputFolder); break;
                    case "Model2Form": sub = new Model2Form(info.XmlFile, emuVRPath, info.DefaultOutputFolder); break;
                    case "Model3Form": sub = new Model3Form(info.XmlFile, emuVRPath, info.DefaultOutputFolder); break;
                    case "PinballFxForm": sub = new PinballFxForm(info.XmlFile, emuVRPath, info.DefaultOutputFolder); break;
                    case "XCloudForm": sub = new XCloudForm(info.XmlFile, emuVRPath, info.DefaultOutputFolder); break;
                    case "MugenForm": sub = new MugenForm(emuVRPath); break;
                    case "ExeForm": sub = new ExeForm(emuVRPath); break;
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
                // show generic UI, hide special subform
                if (activeSubform != null)
                {
                    activeSubform.Close();
                    activeSubform = null;
                }
                panelHost.Controls.Clear();
                panelHost.Visible = false;
                panelGeneric.Visible = true;

                txtCommand.Text = info.Command;
                txtExtensions.Text = string.Join(", ", info.Extensions);

                // Emulator path
                if (!string.IsNullOrEmpty(info.RecommendedExe))
                {
                    string exePath = info.RecommendedExe.StartsWith(".\\") || info.RecommendedExe.StartsWith("./")
                        ? Path.GetFullPath(Path.Combine(emuVRPath, info.RecommendedExe))
                        : info.RecommendedExe;

                    txtEmulatorPath.Text = MakePathRelativeIfInside(exePath, emuVRPath);
                }

                chkShortPath.Checked = info.ShortPathDefault;
                txtGameSystem.Text = info.GameSystem;

                // Output folder
                if (!string.IsNullOrEmpty(emuVRPath) && !string.IsNullOrEmpty(info.DefaultOutputFolder))
                {
                    string outPath = Path.Combine(emuVRPath, "Games", info.DefaultOutputFolder);
                    txtOutputFolder.Text = MakePathRelativeIfInside(outPath, emuVRPath);
                }

                // Input folder
                if (!string.IsNullOrEmpty(info.DefaultRomFolder))
                {
                    string romPath = info.DefaultRomFolder.StartsWith(".\\") || info.DefaultRomFolder.StartsWith("./")
                        ? Path.GetFullPath(Path.Combine(emuVRPath, info.DefaultRomFolder))
                        : info.DefaultRomFolder;

                    txtInputFolder.Text = MakePathRelativeIfInside(romPath, emuVRPath);
                }
            }
        }

        private string MakePathRelativeIfInside(string path, string emuVRPath)
        {
            // Only try to make relative if the checkbox is checked
            if (chkRelativePaths != null && chkRelativePaths.Checked)
            {
                string fullPath = Path.GetFullPath(path);
                string fullEmuVR = Path.GetFullPath(emuVRPath);

                if (fullPath.StartsWith(fullEmuVR, StringComparison.OrdinalIgnoreCase))
                {
                    string rel = "." + fullPath.Substring(fullEmuVR.Length).Replace('/', '\\');
                    while (rel.StartsWith(".\\\\")) rel = ".\\" + rel.Substring(3);
                    return rel;
                }
            }

            // Otherwise return full path
            return path;
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
            public string DefaultOutputFolder { get; set; }
            public string DefaultRomFolder { get; set; }
        }
        private void CaptureCoreCompanion_Load(object sender, EventArgs e)
        {

        }

        private void tableGeneric_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panelHost_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}