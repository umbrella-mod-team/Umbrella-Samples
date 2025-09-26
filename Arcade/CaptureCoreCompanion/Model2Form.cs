// Model2Form.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CaptureCoreCompanion
{
    public partial class Model2Form : Form
    {
        private readonly string xmlFilePath;
        private readonly string emuVRPath;
        // Constructor now takes the XML file path
        public Model2Form(string xmlFile, string emuVRPath, string defaultOutputFolder)
        {
            xmlFilePath = xmlFile;
            this.emuVRPath = emuVRPath;
            InitializeComponent();
            // Set default emulator path
            if (string.IsNullOrWhiteSpace(txtEmulatorPath.Text))
                txtEmulatorPath.Text = Path.GetFullPath(Path.Combine(emuVRPath, @".\Emulators\M2Emulator\emulator_multicpu.exe"));
            // Set default ROMs folder
            if (string.IsNullOrWhiteSpace(txtRomsPath.Text))
                txtRomsPath.Text = Path.GetFullPath(Path.Combine(emuVRPath, @".\Emulators\M2Emulator\roms"));
            // Set output folder
            if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
                txtOutputPath.Text = Path.GetFullPath(Path.Combine(emuVRPath, @".\Games\Arcade (Capture)"));
        }
        private string MakePathRelativeIfInside(string path, string emuVRRoot)
        {
            string fullPath = Path.GetFullPath(path);
            string rootPath = Path.GetFullPath(emuVRRoot);
            if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                string rel = "." + fullPath.Substring(rootPath.Length).Replace('/', '\\');
                while (rel.StartsWith(".\\\\")) rel = ".\\" + rel.Substring(3);
                return rel;
            }
            return fullPath;
        }

        private void BtnEmulatorBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Multicpu Emulator (emulator_multicpu.exe)";
                dlg.Filter = "Executable Files (*.exe)|*.exe";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtEmulatorPath.Text = dlg.FileName;
            }
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Activate();
        }

        private void BtnRomsBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select ROMs Folder";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtRomsPath.Text = dlg.SelectedPath;
            }
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Activate();
        }

        private void BtnOutputBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select EmuVR/Games Output Folder";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtOutputPath.Text = dlg.SelectedPath;
            }
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Activate();
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string emulator = txtEmulatorPath.Text;
            string romsFolder = txtRomsPath.Text;
            string outputFolder = txtOutputPath.Text;

            if (string.IsNullOrEmpty(emulator)
             || string.IsNullOrEmpty(romsFolder)
             || string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show(
                    "Please select emulator, ROMs folder, and output folder.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                return;
            }

            string emulatorName = Path.GetFileName(emulator);
            string emulatorDir = Path.GetDirectoryName(emulator) ?? "";

            // load mapping from segam2.xml
            var xmlData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var doc = XDocument.Load(xmlFilePath);
                foreach (var game in doc.Root.Elements("Game"))
                {
                    var title = game.Element("Title")?.Value;
                    var app = game.Element("ApplicationPath")?.Value;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(app))
                    {
                        // *** PATCH: always use filename without extension, lowercase ***
                        string key = Path.GetFileNameWithoutExtension(app).ToLowerInvariant();
                        if (!xmlData.ContainsKey(key))
                            xmlData[key] = title;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to read {xmlFilePath}: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                return;
            }

            // iterate .zip files
            foreach (var file in Directory.EnumerateFiles(romsFolder, "*.zip", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                string romBase = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                xmlData.TryGetValue(romBase, out string title);
                if (string.IsNullOrEmpty(title))
                    title = romBase;

                // sanitize
                string safe = Regex.Replace(title, @"[<>:""/\\|?*]", " -");
                safe = Regex.Replace(safe, @"\s+", " ").Trim();

                // .win
                File.WriteAllText(
                    Path.Combine(outputFolder, $"{safe}.win"),
                    $"{title}\n{emulatorName}"
                );

                // .bat
                using (var w = new StreamWriter(Path.Combine(outputFolder, $"{safe}.bat")))
                {
                    w.WriteLine("cd ./Games/Arcade (Capture)");
                    w.WriteLine("cd ../..");
                    string relEmuDir = MakePathRelativeIfInside(emulatorDir, emuVRPath);
                    w.WriteLine($"cd /d \"{relEmuDir}\"");

                    string relRomPath = MakePathRelativeIfInside(file, emuVRPath);
                    w.WriteLine($"{emulatorName} \"{relRomPath}\"");
                }
            }

            // emuvr_core.txt
            File.WriteAllText(
                Path.Combine(outputFolder, "emuvr_core.txt"),
@"media = ""Arcade""
core = ""wgc_libretro""
noscanlines = ""true""
aspect_ratio = ""auto""
"
            );
            // emuvr_override_auto.cfg
            File.WriteAllText(
                Path.Combine(outputFolder, "emuvr_override_auto.cfg"),
@"input_player1_analog_dpad_mode = ""0""
video_shader = ""shaders\shaders_glsl\stock.glslp""
video_threaded = ""false""
video_vsync = ""true""
"
            );

            MessageBox.Show(
                "Capture Core files generated successfully.",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information
            );
        }
    }
}
