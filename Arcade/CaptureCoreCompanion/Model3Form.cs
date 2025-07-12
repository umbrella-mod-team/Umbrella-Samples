// Model3Form.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CaptureCoreCompanion
{
    public partial class Model3Form : Form
    {
        private readonly string xmlFilePath;

        public Model3Form(string xmlFile)
        {
            xmlFilePath = xmlFile;
            InitializeComponent();
        }

        private void BtnEmulatorBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Supermodel Executable";
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

            // load mapping from segam3.xml
            var xmlData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var doc = XDocument.Load(xmlFilePath);
                foreach (var game in doc.Root.Elements("Game"))
                {
                    var title = game.Element("Title")?.Value;
                    var app = game.Element("ApplicationPath")?.Value;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(app))
                        xmlData[app + ".zip"] = title;
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
                string romBase = Path.GetFileNameWithoutExtension(file);
                xmlData.TryGetValue(fileName, out string title);
                if (string.IsNullOrEmpty(title))
                    title = "Unknown Title";

                // sanitize
                string safe = Regex.Replace(title, @"[<>:""/\\|?*]", " -");
                safe = Regex.Replace(safe, @"\s+", " ").Trim();

                // stripped title for filename
                string stripped = title.Replace("Supermodel - ", "").Trim();

                // handle duplicates
                var winPath = Path.Combine(outputFolder, $"{stripped}.win");
                var batPath = Path.Combine(outputFolder, $"{stripped}.bat");
                if (File.Exists(winPath))
                    winPath = Path.Combine(outputFolder, $"{stripped} ({romBase}).win");
                if (File.Exists(batPath))
                    batPath = Path.Combine(outputFolder, $"{stripped} ({romBase}).bat");

                // .win
                File.WriteAllText(
                    winPath,
                    $"{title}\n{emulatorName}"
                );

                // .bat
                using (var w = new StreamWriter(batPath))
                {
                    w.WriteLine("cd ./Games/Arcade (Capture)");
                    w.WriteLine("cd ../..");
                    w.WriteLine($"cd /d \"{emulatorDir}\"");
                    w.WriteLine($"supermodel \"{file}\" -outputs=win");
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
