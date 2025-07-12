using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CaptureCoreCompanion
{
    public partial class Xbox360Form : Form
    {
        private readonly string xmlFilePath;

        public Xbox360Form(string xmlFile)
        {
            xmlFilePath = xmlFile;
            InitializeComponent();
        }

        private void BtnXeniaBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Xenia Executable";
                dlg.Filter = "Executable Files (*.exe)|*.exe";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtXeniaPath.Text = dlg.FileName;
            }
        }

        private void BtnInstalledBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Installed/XBLA Games Folder";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtInstalledFolder.Text = dlg.SelectedPath;
            }
        }

        private void BtnXbox360Browse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Xbox 360 Games Folder";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtXbox360Folder.Text = dlg.SelectedPath;
            }
        }

        private void BtnGodBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Xbox 360 GOD Games Folder";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtGODFolder.Text = dlg.SelectedPath;
            }
        }

        private void BtnOutputXBLA_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Output Folder for XBLA Games";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtOutputXBLA.Text = dlg.SelectedPath;
            }
        }

        private void BtnOutput360_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Output Folder for Xbox 360 Games";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtOutputXbox360.Text = dlg.SelectedPath;
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string xeniaPath = txtXeniaPath.Text;
            string installedFolder = txtInstalledFolder.Text;
            string xbox360Folder = txtXbox360Folder.Text;
            string godFolder = txtGODFolder.Text;
            string outputXBLA = txtOutputXBLA.Text;
            string outputXbox360 = txtOutputXbox360.Text;

            if (new[] { xeniaPath, installedFolder, xbox360Folder, godFolder, outputXBLA, outputXbox360 }
                .Any(string.IsNullOrEmpty))
            {
                MessageBox.Show("Please select all paths and folders before generating.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var installedGames = GetInstalledGames(installedFolder, xmlFilePath);
            var xbox360Games = GetXbox360Games(xbox360Folder);
            var godGames = GetGodGames(godFolder);

            // process XBLA
            foreach (var kv in installedGames)
                CreateGameFiles(kv.Key, kv.Value, outputXBLA, xeniaPath);

            // process Xbox360
            foreach (var kv in xbox360Games)
                CreateGameFiles(kv.Key, kv.Value, outputXbox360, xeniaPath);

            // process GOD
            foreach (var kv in godGames)
                CreateGameFiles(kv.Key, kv.Value, outputXbox360, xeniaPath);

            // write shared config files
            File.WriteAllText(Path.Combine(outputXBLA, "emuvr_core.txt"), CoreText);
            File.WriteAllText(Path.Combine(outputXbox360, "emuvr_core.txt"), CoreText);
            File.WriteAllText(Path.Combine(outputXBLA, "emuvr_override_auto.cfg"), OverrideText);
            File.WriteAllText(Path.Combine(outputXbox360, "emuvr_override_auto.cfg"), OverrideText);

            MessageBox.Show("Capture Core files generated successfully.", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---Game Scanning

        private Dictionary<string, string> GetInstalledGames(string folder, string xmlFile)
        {
            var games = new Dictionary<string, string>();
            var xblaFolders = new[] { "000D0000", "00000002" };
            var gameFiles = FindGameFiles(folder, limitDepth: 3, xblaFolders);

            foreach (var entry in gameFiles)
            {
                string title = entry.GameTitle;
                // XBLA special folders: check for XML title
                if (entry.IsXBLA && entry.GameId != null)
                {
                    string friendly = LookupTitle(entry.GameId, xmlFile);
                    if (!string.IsNullOrEmpty(friendly))
                        title = friendly;
                }
                games[title] = entry.GamePath;
            }
            return games;
        }

        private Dictionary<string, string> GetXbox360Games(string folder)
        {
            var games = new Dictionary<string, string>();
            var gameFiles = FindGameFiles(folder, limitDepth: 3);

            // Multi-disc detection and labeling
            var discs = new Dictionary<string, List<(string path, int disc)>>();

            foreach (var entry in gameFiles)
            {
                string baseTitle = entry.GameTitle;
                int discNum = entry.DiscNumber;
                if (!discs.ContainsKey(baseTitle))
                    discs[baseTitle] = new List<(string path, int disc)>();
                discs[baseTitle].Add((entry.GamePath, discNum));
            }
            foreach (var pair in discs)
            {
                if (pair.Value.Count == 1)
                {
                    games[pair.Key] = pair.Value[0].path;
                }
                else
                {
                    foreach (var (path, disc) in pair.Value)
                    {
                        string discTitle = $"{pair.Key} (Disc {disc})";
                        games[discTitle] = path;
                    }
                }
            }
            return games;
        }

        private Dictionary<string, string> GetGodGames(string folder)
        {
            var games = new Dictionary<string, string>();
            foreach (var dir in Directory.GetDirectories(folder))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    if (string.IsNullOrEmpty(Path.GetExtension(file)))
                    {
                        var dataDir = Path.Combine(Path.GetDirectoryName(file),
                            Path.GetFileNameWithoutExtension(file) + ".data");
                        if (Directory.Exists(dataDir))
                        {
                            var title = Path.GetFileName(dir);
                            games[title] = file;
                            break;
                        }
                    }
                }
            }
            return games;
        }

        private class GameFileEntry
        {
            public string GamePath;
            public string GameTitle;
            public int DiscNumber = 1;
            public bool IsXBLA;
            public string GameId;
        }

        private List<GameFileEntry> FindGameFiles(string rootFolder, int limitDepth, string[] xblaFolders = null)
        {
            var found = new List<GameFileEntry>();
            if (!Directory.Exists(rootFolder)) return found;
            void Walk(string dir, int depth)
            {
                if (depth > limitDepth) return;
                var dirs = Directory.GetDirectories(dir);
                var files = Directory.GetFiles(dir);
                foreach (var file in files)
                {
                    if (file.EndsWith(".xex", StringComparison.OrdinalIgnoreCase))
                    {
                        string relative = Path.GetRelativePath(rootFolder, file);
                        var parts = relative.Split(Path.DirectorySeparatorChar);
                        string title = parts.Length > 1 ? parts[0] : Path.GetFileNameWithoutExtension(file);

                        // XBLA detection
                        bool isXBLA = false;
                        string gameId = null;
                        if (xblaFolders != null)
                        {
                            foreach (var special in xblaFolders)
                            {
                                if (dir.EndsWith(special))
                                {
                                    isXBLA = true;
                                    if (parts.Length > 1)
                                        gameId = parts[parts.Length - 2];
                                    break;
                                }
                            }
                        }

                        // Disc detection
                        int discNum = 1;
                        if (dir != rootFolder)
                        {
                            var match = Regex.Match(dir, @"Disc[ _-]?([0-9]+)", RegexOptions.IgnoreCase);
                            if (match.Success)
                                discNum = int.Parse(match.Groups[1].Value);
                        }

                        found.Add(new GameFileEntry
                        {
                            GamePath = file,
                            GameTitle = title,
                            DiscNumber = discNum,
                            IsXBLA = isXBLA,
                            GameId = gameId
                        });
                    }
                }
                foreach (var sub in dirs)
                    Walk(sub, depth + 1);
            }
            Walk(rootFolder, 0);
            return found;
        }

        private string LookupTitle(string gameId, string xmlFile)
        {
            try
            {
                if (!File.Exists(xmlFile)) return null;
                var doc = XDocument.Load(xmlFile);
                var titleElem = doc.Root?
                    .Elements("Game")
                    .FirstOrDefault(e =>
                        (string)e.Element("ApplicationPath") == gameId
                    )?
                    .Element("Title");
                return titleElem?.Value;
            }
            catch { return null; }
        }

        private void CreateGameFiles(string title, string path, string output, string exe)
        {
            var safe = SanitizeFilename(title);
            File.WriteAllText(Path.Combine(output, $"{safe}.win"),
                              Path.GetFileName(exe));

            using (var w = new StreamWriter(Path.Combine(output, $"{safe}.bat")))
            {
                w.WriteLine("cd ./Games/Arcade (Capture)");
                w.WriteLine("cd ../..");
                w.WriteLine($"\"{exe}\" --fullscreen \"{path}\"");
            }
        }

        private string SanitizeFilename(string filename)
        {
            filename = Regex.Replace(filename, @"[<>:\""/\\|?*]", " -");
            filename = Regex.Replace(filename, @"\\s+", " ").Trim();
            return filename;
        }

        private const string CoreText =
@"media = ""Xbox 360""
core = ""wgc_libretro""
noscanlines = ""true""
aspect_ratio = ""auto""
";
        private const string OverrideText =
@"input_player1_analog_dpad_mode = ""0""
video_shader = ""shaders\shaders_glsl\stock.glslp""
video_threaded = ""false""
video_vsync = ""true""
";
    }
}
