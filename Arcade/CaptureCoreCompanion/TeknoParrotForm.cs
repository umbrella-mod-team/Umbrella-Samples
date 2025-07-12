// TeknoParrotForm.cs
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CaptureCoreCompanion
{
    public partial class TeknoParrotForm : Form
    {
        private readonly string xmlFilePath;

        public TeknoParrotForm(string xmlFile)
        {
            xmlFilePath = xmlFile;
            InitializeComponent();
        }

        private void BtnTeknoParrotBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select TeknoParrotUi Executable";
                dlg.Filter = "Executable Files (*.exe)|*.exe";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtTeknoParrotPath.Text = dlg.FileName;
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
                    txtOutputFolder.Text = dlg.SelectedPath;
            }
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Activate();
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string tpPath = txtTeknoParrotPath.Text;
            string outputFolder = txtOutputFolder.Text;

            if (string.IsNullOrEmpty(tpPath) || string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show(
                    "Please select both TeknoParrot path and output folder.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            // Check for UserProfiles folder
            string profilesFolder = Path.Combine(
                Path.GetDirectoryName(tpPath) ?? "",
                "UserProfiles"
            );
            if (!Directory.Exists(profilesFolder))
            {
                MessageBox.Show(
                    $"The directory {profilesFolder} does not exist.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            // Gather profile names
            var gameProfiles = Directory
                .EnumerateFiles(profilesFolder, "*.xml")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Load tp.xml
            XElement root;
            try
            {
                root = XDocument.Load(xmlFilePath).Root!;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to read {xmlFilePath}: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            // Create .win and .bat for each matching game
            foreach (var game in root.Elements("Game"))
            {
                string title = game.Element("Title")?.Value ?? "";
                string appPath = game.Element("ApplicationPath")?.Value ?? "";
                string emulator = game.Element("Emulator")?.Value ?? "";

                if (string.IsNullOrEmpty(title) ||
                    string.IsNullOrEmpty(appPath) ||
                    !gameProfiles.Contains(Path.GetFileNameWithoutExtension(appPath)))
                {
                    continue;
                }

                // Sanitize title
                string safe = Regex.Replace(title, @"[<>:""/\\|?*]", " -");
                safe = Regex.Replace(safe, @"\s+", " ").Trim();

                // .win
                File.WriteAllText(
                    Path.Combine(outputFolder, $"{safe}.win"),
                    $"{emulator}\n{Path.GetFileName(tpPath)}"
                );

                // .bat
                using (var w = new StreamWriter(Path.Combine(outputFolder, $"{safe}.bat")))
                {
                    w.WriteLine("cd ./Games/Arcade (Capture)");
                    w.WriteLine("cd ../..");
                    w.WriteLine($"\"{tpPath}\" --profile={appPath}");
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
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }
}
