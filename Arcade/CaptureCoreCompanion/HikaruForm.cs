// HikaruForm.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CaptureCoreCompanion
{
    public partial class HikaruForm : Form
    {
        private readonly string xmlFilePath;

        // Now takes the XML path in its constructor
        public HikaruForm(string xmlFile)
        {
            xmlFilePath = xmlFile;
            InitializeComponent();
        }

        private void BtnEmulatorBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Demul Executable";
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
                    "Please select the emulator, ROMs folder, and output folder.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                return;
            }

            string emulatorName = Path.GetFileName(emulator);
            string emulatorDir = Path.GetDirectoryName(emulator) ?? "";

            var xmlData = LoadXmlData();

            foreach (var file in Directory.EnumerateFiles(romsFolder, "*.zip", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                if (!xmlData.TryGetValue(fileName, out var title))
                    continue;

                string safe = Regex.Replace(title, @"[<>:""/\\|?*]", " -");
                safe = Regex.Replace(safe, @"\s+", " ").Trim();

                File.WriteAllText(
                    Path.Combine(outputFolder, $"{safe}.win"),
                    $"{title}\n{emulatorName}"
                );

                using (var w = new StreamWriter(Path.Combine(outputFolder, $"{safe}.bat")))
                {
                    w.WriteLine("cd ./Games/Arcade (Capture)");
                    w.WriteLine("cd ../..");
                    w.WriteLine($"cd /d \"{emulatorDir}\"");
                    w.WriteLine($"{emulatorName} -run=hikaru -rom={Path.GetFileNameWithoutExtension(fileName)}");
                }
            }

            File.WriteAllText(
                Path.Combine(outputFolder, "emuvr_core.txt"),
@"media = ""Arcade""
core = ""wgc_libretro""
noscanlines = ""true""
aspect_ratio = ""auto""
"
            );
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

        private Dictionary<string, string> LoadXmlData()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var doc = XDocument.Load(xmlFilePath);
                foreach (var game in doc.Root.Elements("Game"))
                {
                    var title = game.Element("Title")?.Value;
                    var app = game.Element("ApplicationPath")?.Value;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(app))
                        data[app + ".zip"] = title;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load XML ({xmlFilePath}): {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                );
            }
            return data;
        }
    }
}
