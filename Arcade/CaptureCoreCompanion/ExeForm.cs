// ExeForm.cs
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CaptureCoreCompanion
{
    public partial class ExeForm : Form
    {
        private readonly string emuVRPath;
        public ExeForm(string emuVRPath)
        {
            this.emuVRPath = emuVRPath;
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(emuVRPath) && txtOutputFolder != null && string.IsNullOrWhiteSpace(txtOutputFolder.Text))
            {
                txtOutputFolder.Text = Path.Combine(emuVRPath, "Games", "PC (Capture)");
            }
        }

        private void BtnBrowseInput_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Game EXE";
                dlg.Filter = "Executable Files (*.exe)|*.exe";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtInputFile.Text = dlg.FileName;
            }
            RestoreWindow();
        }

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select EmuVR/Games Output Folder";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtOutputFolder.Text = dlg.SelectedPath;
            }
            RestoreWindow();
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string exePath = txtInputFile.Text;
            string title = txtTitle.Text;
            string extra = txtCommands.Text;
            string output = txtOutputFolder.Text;

            if (string.IsNullOrEmpty(exePath) || !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please select a valid .exe file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Please enter a game title.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(output))
            {
                MessageBox.Show("Please select an output folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string safe = Regex.Replace(title, @"[<>:""/\\|?*]", " -");
            safe = Regex.Replace(safe, @"\s+", " ").Trim();

            string exeDir = Path.GetDirectoryName(exePath) ?? "";
            string exeName = Path.GetFileName(exePath);

            // .win
            File.WriteAllText(
                Path.Combine(output, $"{safe}.win"),
                exeName
            );

            // .bat
            using (var w = new StreamWriter(Path.Combine(output, $"{safe}.bat")))
            {
                w.WriteLine($@"cd /d ""{exeDir}""");
                w.WriteLine($@"""{exeName}"" {extra}");
            }

            // emuvr_core.txt
            File.WriteAllText(
                Path.Combine(output, "emuvr_core.txt"),
@"media = ""Arcade""
core = ""wgc_libretro""
noscanlines = ""true""
aspect_ratio = ""auto""
"
            );

            // emuvr_override_auto.cfg
            File.WriteAllText(
                Path.Combine(output, "emuvr_override_auto.cfg"),
@"input_player1_analog_dpad_mode = ""0""
video_shader = ""shaders\shaders_glsl\stock.glslp""
video_threaded = ""false""
video_vsync = ""true""
"
            );

            MessageBox.Show("Capture Core files generated successfully.\nMedia set to Arcade by default.",
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RestoreWindow()
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Activate();
        }
    }
}
