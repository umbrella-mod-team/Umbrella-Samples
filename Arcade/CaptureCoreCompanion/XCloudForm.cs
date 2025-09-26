// XboxCloudForm.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CaptureCoreCompanion
{
    public partial class XCloudForm : Form
    {
        private readonly string dataFilePath;
        private readonly string emuVRPath;

        public XCloudForm(string datFile, string emuVRPath, string defaultOutputFolder)
        {
            this.emuVRPath = emuVRPath;
            dataFilePath = datFile;
            InitializeComponent();
            if (string.IsNullOrWhiteSpace(txtOutputFolder.Text))
                txtOutputFolder.Text = Path.GetFullPath(Path.Combine(emuVRPath, @".\Games\Microsoft Xbox One\"));
        }

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select EmuVR/Games Output Folder for XCloud Games";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtOutputFolder.Text = dlg.SelectedPath;
            }
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Activate();
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string outputFolder = txtOutputFolder.Text;
            if (string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show("Please select the output folder.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(dataFilePath))
            {
                MessageBox.Show($"Data file not found: {dataFilePath}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var cloudGames = ReadCloudData(dataFilePath);
            foreach (var (title, url) in cloudGames)
                CreateGameFiles(title, url, outputFolder);

            MessageBox.Show("Capture Core files generated successfully.",
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private List<(string Title, string Url)> ReadCloudData(string filePath)
        {
            var list = new List<(string, string)>();
            foreach (var line in File.ReadAllLines(filePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("http"))
                {
                    var parts = trimmed.Split(new[] { "http" }, 2, StringSplitOptions.None);
                    var title = parts[0].Trim();
                    var url = "http" + parts[1].Trim();
                    list.Add((title, url));
                }
            }
            return list;
        }

        private void CreateGameFiles(string title, string url, string output)
        {
            // sanitize title
            var safe = Regex.Replace(title, @"[<>:""/\\|?*]", " -");
            safe = Regex.Replace(safe, @"\s+", " ").Trim();

            // .bat
            var batPath = Path.Combine(output, $"{safe}.bat");
            using (var w = new StreamWriter(batPath))
                w.WriteLine($@"start msedge --window-size=1920,1080 --kiosk --new-window --app=""{url}""");

            // .win
            var winPath = Path.Combine(output, $"{safe}.win");
            using (var w = new StreamWriter(winPath))
            {
                w.WriteLine("Xbox Cloud Gaming");
                w.WriteLine(title);
            }

            // emuvr_core.txt
            File.WriteAllText(
                Path.Combine(output, "emuvr_core.txt"),
@"media = ""Xbox One""
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
        }
    }
}
