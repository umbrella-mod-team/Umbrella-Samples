// PinballFXForm.cs
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CaptureCoreCompanion
{
    public partial class PinballFxForm : Form
    {
        private readonly string xmlFilePath;

        public PinballFxForm(string xmlFile)
        {
            xmlFilePath = xmlFile;
            InitializeComponent();
        }

        private void BtnBrowseEmulator_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Pinball FX/M Executable";
                dlg.Filter = "Executable Files (*.exe)|*.exe";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtEmulatorPath.Text = dlg.FileName;
            }
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;
            Activate();
        }

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
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

        private string GetSteamAppId(string exeName)
        {
            switch (exeName.ToLowerInvariant())
            {
                case "pinballfx-win64-shipping.exe":
                    return "2328760"; // Pinball FX
                case "pinballm-win64-shipping.exe":
                    return "2337640"; // Pinball M
                case "pinballfx3.exe":
                case "pinball fx3.exe":
                    return "442120";  // Pinball FX 3
                case "pinballfx2.exe":
                case "pinball fx2.exe":
                    return "226980";  // Pinball FX 2
                default:
                    return null;
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string exePath = txtEmulatorPath.Text;
            string outputFolder = txtOutputFolder.Text;

            if (string.IsNullOrEmpty(exePath) || string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show("Please select both the Pinball FX/M executable and the output folder.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string exeName = Path.GetFileName(exePath);
            string baseFolder = Path.GetDirectoryName(exePath) ?? "";
            string xmlToLoad = null;

            // Choose XML based on exe name
            if (exeName.Equals("PinballFX-Win64-Shipping.exe", StringComparison.OrdinalIgnoreCase))
                xmlToLoad = xmlFilePath;                           // pinballfx.xml
            else if (exeName.Equals("PinballM-Win64-Shipping.exe", StringComparison.OrdinalIgnoreCase))
                xmlToLoad = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                         "pinballm.xml");         // override to pinballm.xml

            if (!string.IsNullOrEmpty(xmlToLoad) && File.Exists(xmlToLoad))
            {
                // parse the XML and generate per <Game>
                XElement root;
                try
                {
                    root = XDocument.Load(xmlToLoad).Root;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load XML ({xmlToLoad}): {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                foreach (var game in root.Elements("Game"))
                {
                    var title = game.Element("Title")?.Value ?? "";
                    var app = game.Element("ApplicationPath")?.Value ?? "";
                    var steamIdXml = game.Element("SteamID")?.Value;
                    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(app))
                        continue;

                    string safe = Regex.Replace(title, @"[<>:""/\\|?*]", " -");
                    safe = Regex.Replace(safe, @"\s+", " ").Trim();

                    // .win
                    File.WriteAllText(
                        Path.Combine(outputFolder, $"{safe}.win"),
                        exeName + "\n"
                    );

                    // .bat
                    using (var w = new StreamWriter(Path.Combine(outputFolder, $"{safe}.bat")))
                    {
                        w.WriteLine("cd ./Games/Arcade (Capture)");
                        w.WriteLine("cd ../..");
                        if (chkUseSteamLaunch.Checked)
                        {
                            string steamPath = null;
                            try
                            {
                                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                                {
                                    if (key != null)
                                    {
                                        object value = key.GetValue("SteamPath");
                                        if (value != null)
                                            steamPath = Path.Combine(value.ToString().Replace('/', '\\'), "steam.exe");
                                    }
                                }
                            }
                            catch { }
                            if (string.IsNullOrEmpty(steamPath))
                                steamPath = "steam.exe"; // fallback, must be in PATH

                            string steamAppId = !string.IsNullOrEmpty(steamIdXml) ? steamIdXml : GetSteamAppId(exeName);
                            if (!string.IsNullOrEmpty(steamAppId))
                                w.WriteLine($"\"{steamPath}\" -applaunch {steamAppId} -table {app} -offline");
                            else
                                w.WriteLine($"REM Unable to determine Steam App ID for {exeName}");
                        }
                        else
                        {
                            w.WriteLine($"\"{exePath}\" -Table {app}");
                        }
                    }
                }
            }
            else
            {
                // fallback: scan for .pxp profiles
                var ignored = new[]
                {
                    "game_cfg","gui","fonts","sfx","gui_cfg","gui_sfx","preview","txt",
                    "Customization","WMSAssets","WMS6Assets","WMS1Assets","WMS2Assets",
                    "WMS3Assets","WMS4Assets","WMS5Assets","WMSIndyAssets","WMSMONSTERAssets",
                    "RedCupAssets","SpaceBearAssets","PFXPromoStarTrekAssets","NewsFeedAssets","PFXPromoAssets"
                };
                var profiles = Directory
                    .EnumerateFiles(baseFolder, "*.pxp", SearchOption.AllDirectories)
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(n => !ignored.Contains(n, StringComparer.OrdinalIgnoreCase));

                foreach (var table in profiles)
                {
                    string safe = Regex.Replace(table, @"[<>:""/\\|?*]", " -");
                    safe = Regex.Replace(safe, @"\s+", " ").Trim();

                    // .win
                    File.WriteAllText(
                        Path.Combine(outputFolder, $"{safe}.win"),
                        exeName + "\n"
                    );
                    // .bat
                    using (var w = new StreamWriter(Path.Combine(outputFolder, $"{safe}.bat")))
                    {
                        w.WriteLine("cd ./Games/Arcade (Capture)");
                        w.WriteLine("cd ../..");
                        if (chkUseSteamLaunch.Checked)
                        {
                            string steamPath = null;
                            try
                            {
                                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                                {
                                    if (key != null)
                                    {
                                        object value = key.GetValue("SteamPath");
                                        if (value != null)
                                            steamPath = Path.Combine(value.ToString().Replace('/', '\\'), "steam.exe");
                                    }
                                }
                            }
                            catch { }
                            if (string.IsNullOrEmpty(steamPath))
                                steamPath = "steam.exe"; // fallback, must be in PATH

                            string steamAppId = GetSteamAppId(exeName);
                            if (!string.IsNullOrEmpty(steamAppId))
                                w.WriteLine($"\"{steamPath}\" -applaunch {steamAppId} -table {table} -offline");
                            else
                                w.WriteLine($"REM Unable to determine Steam App ID for {exeName}");
                        }
                        else
                        {
                            w.WriteLine($"\"{exePath}\" -offline -table_{table}");
                        }
                    }
                }
            }

            // shared config files
            File.WriteAllText(
                Path.Combine(outputFolder, "emuvr_core.txt"),
@"media = ""Pinball""
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

            MessageBox.Show("Capture Core files generated successfully.",
                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
