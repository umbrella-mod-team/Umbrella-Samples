// PS3Form.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CaptureCoreCompanion
{
    public partial class PS3Form : Form
    {
        private readonly string emuVRPath;
        private readonly string xmlFilePath;

        public PS3Form(string xmlFile, string emuVRPath, string defaultOutputFolder)
        {
            xmlFilePath = xmlFile;
            this.emuVRPath = emuVRPath;
            InitializeComponent();
            // Set recommended output for both
            // Set output folders
            txtOutputPS3Folder.Text = Path.GetFullPath(Path.Combine(emuVRPath, @".\Games\Sony PlayStation 3\"));
            txtOutputPSNFolder.Text = Path.GetFullPath(Path.Combine(emuVRPath, @".\Games\Sony PS3-PSN\"));
            // Set default emulator path if empty
            if (string.IsNullOrWhiteSpace(txtPS3Path.Text))
            {
                txtPS3Path.Text = Path.GetFullPath(Path.Combine(emuVRPath, @".\Emulators\RPCS3\rpcs3.exe"));
            }
            // Set default games/ROMs folder if empty
            if (string.IsNullOrWhiteSpace(txtPS3GamesFolder.Text))
            {
                txtPS3GamesFolder.Text = Path.GetFullPath(Path.Combine(emuVRPath, @".\Roms\Sony PlayStation 3\"));
            }

        }
        private string MakePathRelativeIfInside(string path, string emuVRRoot)
        {
            if (chkRelativePaths != null && chkRelativePaths.Checked)
            {
                string fullPath = Path.GetFullPath(path);
                string rootPath = Path.GetFullPath(emuVRRoot);
                if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    string rel = "." + fullPath.Substring(rootPath.Length).Replace('/', '\\');
                    while (rel.StartsWith(".\\\\")) rel = ".\\" + rel.Substring(3);
                    return rel;
                }
            }
            return path;
        }
        private void BtnPS3Browse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select RPCS3 Executable";
                dlg.Filter = "Executable Files (*.exe)|*.exe";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtPS3Path.Text = dlg.FileName;
            }
        }

        private void BtnPS3GamesBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select PS3 Games Folder";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtPS3GamesFolder.Text = dlg.SelectedPath;
            }
        }

        private void BtnOutputPSNBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Output Folder for PSN Games";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtOutputPSNFolder.Text = dlg.SelectedPath;
            }
        }

        private void BtnOutputPS3Browse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Output Folder for PS3 Games";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtOutputPS3Folder.Text = dlg.SelectedPath;
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string exePath = txtPS3Path.Text;
            string gamesFolder = txtPS3GamesFolder.Text;
            string outPSN = txtOutputPSNFolder.Text;
            string outPS3 = txtOutputPS3Folder.Text;

            if (string.IsNullOrEmpty(exePath)
             || string.IsNullOrEmpty(gamesFolder)
             || string.IsNullOrEmpty(outPSN)
             || string.IsNullOrEmpty(outPS3))
            {
                MessageBox.Show("Please select emulator path, PS3 games folder, and both output folders.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Scan PSN games inside emulator's dev_hdd0/game
            var baseDir = Path.GetDirectoryName(exePath);
            var psnFolder = Path.Combine(baseDir, "dev_hdd0", "game");
            var psnGames = Directory.Exists(psnFolder)
                              ? GetInstalledGames(psnFolder)
                              : new Dictionary<string, string>();

            if (!psnGames.Any())
                MessageBox.Show("No PSN games found in dev_hdd0/game.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Scan user-selected PS3 games folder
            var ps3Games = Directory.Exists(gamesFolder)
                           ? GetInstalledGames(gamesFolder)
                           : new Dictionary<string, string>();
            if (!ps3Games.Any())
                MessageBox.Show("No PS3 games found in specified folder.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Load and sanitize XML
            XElement xmlRoot;
            try
            {
                var raw = File.ReadAllText(xmlFilePath);
                raw = Regex.Replace(raw, @"[^\x09\x0A\x0D\x20-\x7F]", "-");
                xmlRoot = XDocument.Parse(raw).Root;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load XML: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ProcessGameFiles(psnGames, exePath, outPSN, xmlRoot);
            ProcessGameFiles(ps3Games, exePath, outPS3, xmlRoot);

            MessageBox.Show("Capture Core files generated successfully.", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Dictionary<string, string> GetInstalledGames(string folder)
        {
            var result = new Dictionary<string, string>();
            foreach (var dir in Directory.GetDirectories(folder))
            {
                var id = Path.GetFileName(dir);
                var bin = Directory
                    .EnumerateFiles(dir, "*.bin", SearchOption.AllDirectories)
                    .FirstOrDefault(f =>
                    {
                        var n = Path.GetFileName(f).ToLower();
                        return n == "eboot.bin" || n == "boot.bin";
                    });
                if (bin != null) result[id] = bin;
            }
            return result;
        }

        private void ProcessGameFiles(
            Dictionary<string, string> games,
            string exePath,
            string outFolder,
            XElement xmlRoot)
        {
            foreach (var kv in games)
            {
                var gameId = kv.Key;
                var binPath = kv.Value;
                var title = LookupTitle(xmlRoot, gameId);

                var safe = SanitizeFilename(title);

                File.WriteAllText(
                    Path.Combine(outFolder, $"{safe}.win"),
                    Path.GetFileName(exePath)
                );

                using (var w = new StreamWriter(Path.Combine(outFolder, $"{safe}.bat")))
                {


                    w.WriteLine("cd ./Games/Arcade (Capture)");
                    w.WriteLine("cd ../..");
                    string relEmuFolder = MakePathRelativeIfInside(Path.GetDirectoryName(exePath), emuVRPath);
                    w.WriteLine($"cd \"{relEmuFolder}\"");
                   // emulator executable name only
                    string exeName = Path.GetFileName(exePath);
                    string relBinPath = MakePathRelativeIfInside(binPath, emuVRPath);
                    // relative ROM path
                    w.WriteLine($"\"{exeName}\" --no-gui \"{relBinPath}\"");
                }

                File.WriteAllText(
                    Path.Combine(outFolder, "emuvr_core.txt"),
                    "media = \"PlayStation 3\"\ncore = \"wgc_libretro\"\nnoscanlines = \"true\"\naspect_ratio = \"auto\"\n"
                );

                File.WriteAllText(
                    Path.Combine(outFolder, "emuvr_override_auto.cfg"),
                    "input_player1_analog_dpad_mode = \"0\"\nvideo_shader = \"shaders\\shaders_glsl\\stock.glslp\"\nvideo_threaded = \"false\"\nvideo_vsync = \"true\"\n"
                );
            }
        }


        private string LookupTitle(XElement xmlRoot, string gameId)
        {
            var title = xmlRoot
                .Elements("Game")
                .FirstOrDefault(g => (string)g.Element("ApplicationPath") == gameId)?
                .Element("Title")?.Value;
            if (!string.IsNullOrEmpty(title))
                return title;

            // Handle [TITLEID] in folder names
            var match = Regex.Match(gameId, @"\[([A-Z0-9]+)\]");
            if (match.Success)
            {
                var altId = match.Groups[1].Value;
                title = xmlRoot
                    .Elements("Game")
                    .FirstOrDefault(g => (string)g.Element("ApplicationPath") == altId)?
                    .Element("Title")?.Value;
                if (!string.IsNullOrEmpty(title))
                    return title;
            }
            return gameId;
        }

        private string SanitizeFilename(string filename)
        {
            filename = Regex.Replace(filename, @"[<>:""/\\|?*]", " -");
            filename = Regex.Replace(filename, @"\\s+", " ").Trim();
            return filename;
        }
    }
}
