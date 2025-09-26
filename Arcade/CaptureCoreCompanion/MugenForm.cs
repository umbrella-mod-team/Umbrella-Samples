// MugenForm.cs
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CaptureCoreCompanion
{
    public partial class MugenForm : Form
    {
        private readonly string emuVRPath;

        public MugenForm(string emuVRPath)
        {
            this.emuVRPath = emuVRPath; 
            InitializeComponent();
        }

        private void BtnBrowseInput_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Input Games Folder";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtInputFolder.Text = dlg.SelectedPath;
            }
            EnsureRestored();
        }

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select EmuVR/Games Output Folder";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtOutputFolder.Text = dlg.SelectedPath;
            }
            EnsureRestored();
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            var input = txtInputFolder.Text;
            var output = txtOutputFolder.Text;
            var extra = txtCommands.Text;

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
            {
                MessageBox.Show("Please select both input and output folders.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var dir in Directory.GetDirectories(input))
            {
                var folderName = Path.GetFileName(dir);
                var exes = Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories);
                if (!exes.Any()) continue;

                foreach (var exe in exes)
                {
                    var exeBase = Path.GetFileNameWithoutExtension(exe);
                    var safeName = Sanitize(folderName);
                    if (exes.Length > 1)
                        safeName += $" ({exeBase})";

                    // .win
                    File.WriteAllText(
                        Path.Combine(output, $"{safeName}.win"),
                        Path.GetFileName(exe)
                    );

                    // .bat
                    using (var w = new StreamWriter(Path.Combine(output, $"{safeName}.bat")))
                        w.WriteLine($"\"{exe}\" {extra}");
                }
            }

            // shared configs
            File.WriteAllText(
                Path.Combine(output, "emuvr_core.txt"),
@"media = ""Arcade""
core = ""wgc_libretro""
noscanlines = ""true""
aspect_ratio = ""auto""
"
            );
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

        private string Sanitize(string name)
        {
            var s = Regex.Replace(name, @"[<>:""/\\|?*]", " -");
            return Regex.Replace(s, @"\s+", " ").Trim();
        }

        private void EnsureRestored()
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Activate();
        }
    }
}
