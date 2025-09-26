// VitaForm.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CaptureCoreCompanion
{
    public partial class VitaForm : Form
    {
        private readonly string xmlFilePath;
        private readonly string emuVRPath;

        public VitaForm(string xmlFile, string emuVRPath, string defaultOutputFolder)
        {
            this.emuVRPath = emuVRPath;
            xmlFilePath = xmlFile;
            InitializeComponent();
            // Set default emulator path
            if (string.IsNullOrWhiteSpace(txtVita3KPath.Text))
                txtVita3KPath.Text = Path.GetFullPath(Path.Combine(emuVRPath, @".\Emulators\Vita3K\Vita3K.exe"));
            // Set output folder
            if (string.IsNullOrWhiteSpace(txtOutputFolder.Text))
                txtOutputFolder.Text = Path.GetFullPath(Path.Combine(emuVRPath, @".\Games\Sony Playstation Vita\"));

        }

        private void BtnVita3KBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Vita3K Executable";
                dlg.Filter = "Executable Files (*.exe)|*.exe";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtVita3KPath.Text = dlg.FileName;
            }
        }

        private void BtnOutputBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select EmuVR/Games Output Folder";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtOutputFolder.Text = dlg.SelectedPath;
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string vita3kPath = txtVita3KPath.Text;
            string outputFolder = txtOutputFolder.Text;

            if (string.IsNullOrEmpty(vita3kPath) || string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show("Please select both Vita3K path and output folder.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check for ux0/app folder
            string appFolder = Path.Combine(Path.GetDirectoryName(vita3kPath), "ux0", "app");
            if (!Directory.Exists(appFolder))
            {
                MessageBox.Show($"The directory {appFolder} does not exist.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Gather installed games
            var installed = new HashSet<string>(
                Directory.GetDirectories(appFolder)
                         .Select(d => Path.GetFileName(d))
            );

            // Load XML
            XElement root;
            try
            {
                root = XDocument.Load(xmlFilePath).Root;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read {xmlFilePath}: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Process each Game element
            foreach (var game in root.Elements("Game"))
            {
                string title = game.Element("Title")?.Value;
                string appPath = game.Element("ApplicationPath")?.Value;

                if (string.IsNullOrEmpty(title) ||
                    string.IsNullOrEmpty(appPath) ||
                    !installed.Contains(appPath))
                    continue;

                // Sanitize
                string safe = Regex.Replace(title, @"[<>:""/\\|?*]", " -");
                safe = Regex.Replace(safe, @"\s+", " ").Trim();

                // .win
                File.WriteAllText(
                    Path.Combine(outputFolder, $"{safe}.win"),
                    Path.GetFileName(vita3kPath)
                );

                // .bat
                using (var w = new StreamWriter(Path.Combine(outputFolder, $"{safe}.bat")))
                {
                    w.WriteLine("cd ./Games/Arcade (Capture)");
                    w.WriteLine("cd ../..");
                    w.WriteLine($"\"{vita3kPath}\" --fullscreen -r {appPath}");
                }

                // emuvr_core.txt
                File.WriteAllText(
                    Path.Combine(outputFolder, "emuvr_core.txt"),
                    "media = \"Vita\"\ncore = \"wgc_libretro\"\n" +
                    "noscanlines = \"true\"\naspect_ratio = \"auto\"\n"
                );

                // emuvr_override_auto.cfg
                File.WriteAllText(
                    Path.Combine(outputFolder, "emuvr_override_auto.cfg"),
                    "input_player1_analog_dpad_mode = \"0\"\n" +
                    "video_shader = \"shaders\\shaders_glsl\\stock.glslp\"\n" +
                    "video_threaded = \"false\"\nvideo_vsync = \"true\"\n"
                );
            }

            MessageBox.Show("Capture Core files generated successfully.",
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
