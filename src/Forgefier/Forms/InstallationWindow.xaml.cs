using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using dotMCLauncher.Versioning;
using Ionic.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Forgefier
{
    public partial class InstallationWindow
    {
        private string _mcDirectory { get; }
        private string _mcVersions { get; }
        private string _mcLibs { get; }
        private string _tempDir { get; set; }
        private McForgeVersion _mcForgeVersion { get; }
        private string _customVersionId { get; }
        private string _customProfileName { get; }
        private string _javaPath { get; }
        private readonly WebClient _webClient = new WebClient();
        private bool _allowQuit { get; set; }

        public InstallationWindow(string mcDirectory, McForgeVersion forgeVersion, string versionId, string profileName, string javaPath)
        {
            InitializeComponent();
            _mcDirectory = mcDirectory;
            _mcVersions = _mcDirectory + "/versions/";
            _mcLibs = _mcDirectory + "/libraries/";
            _mcForgeVersion = forgeVersion;
            _customVersionId = versionId;
            _customProfileName = profileName;
            _javaPath = javaPath;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _tempDir = GetTempDirectory();
            AppendLog($"Created temp directory {_tempDir}.");
            Thread thread = new Thread(BeginInstallation);
            thread.Start();
        }

        private delegate void MethodInvoker();

        private delegate void StringMethodInvoker(string arg);

        private delegate void BoolMethodInvoker(bool arg);

        private delegate void IntMethodInvoker(int arg);

        private void AppendLog(string text)
        {
            if (!Dispatcher.CheckAccess()) {
                Dispatcher.Invoke(new StringMethodInvoker(AppendLog), text);
                return;
            }

            TextBox.AppendText(string.Format(
                (string.IsNullOrEmpty(TextBox.Text) ? string.Empty : Environment.NewLine) +
                "[{0}] {1}",
                DateTime.Now.ToString("dd-MM-yy HH:mm:ss"), text));
            TextBox.ScrollToEnd();
        }

        private void SetStatusLabel(string text)
        {
            if (!Dispatcher.CheckAccess()) {
                Dispatcher.Invoke(new StringMethodInvoker(SetStatusLabel), text);
                return;
            }

            Label.Content = text;
        }

        private void SetExitState(bool allow)
        {
            if (!Dispatcher.CheckAccess()) {
                Dispatcher.Invoke(new BoolMethodInvoker(SetExitState), allow);
                return;
            }

            _allowQuit = true;
        }

        private void SetProgressMax(int value)
        {
            if (!Dispatcher.CheckAccess()) {
                Dispatcher.Invoke(new IntMethodInvoker(SetProgressMax), value);
                return;
            }

            ProgressBar.Maximum = value;
        }

        private void IncreaseProgressValue()
        {
            if (!Dispatcher.CheckAccess()) {
                Dispatcher.Invoke(new MethodInvoker(IncreaseProgressValue));
                return;
            }

            ProgressBar.Value++;
        }

        private void BeginInstallation()
        {
            try {
                SetProgressMax(_mcForgeVersion.InstallationMethod == McForgeInstallationType.LEGACY ? 6 : 7);
                string mcVersions = _mcDirectory + "/versions/",
                    destDir = Path.Combine(mcVersions, _customVersionId + @"\"),
                    originDir = Path.Combine(mcVersions, _mcForgeVersion.McVersion + @"\");
                string versionJar = Path.Combine(destDir, _customVersionId + ".jar"),
                    versionJson = Path.Combine(destDir, _customVersionId + ".json");
                DownloadVersion(_mcForgeVersion.McVersion.ToString());
                if (!Directory.Exists(destDir)) {
                    Directory.CreateDirectory(destDir);
                }

                IncreaseProgressValue();

                if (_mcForgeVersion.InstallationMethod == McForgeInstallationType.LEGACY) {
                    AppendLog("Installing Forge for LEGACY version...");
                    AppendLog("Downloading Forge...");
                    SetStatusLabel(FindResource("r_LabelStatusDownloadingForge").ToString());
                    _webClient.DownloadFile(_mcForgeVersion.DownloadUrl, Path.Combine(_tempDir, "temp.zip"));

                    AppendLog($"Creating custom `{_customVersionId}` version from `{_mcForgeVersion.McVersion}`...");
                    SetStatusLabel(FindResource("r_LabelStatusCreatingVersion").ToString());
                    if (File.Exists(versionJar)) {
                        File.Delete(versionJar);
                    }

                    AppendLog(" Copying JAR...");

                    File.Copy(Path.Combine(originDir, _mcForgeVersion.McVersion + ".jar"), Path.Combine(destDir, _customVersionId + ".jar"));

                    if (File.Exists(versionJson)) {
                        File.Delete(versionJson);
                    }

                    AppendLog(" Copying JSON...");

                    File.Copy(Path.Combine(originDir, _mcForgeVersion.McVersion + ".json"), Path.Combine(destDir, _customVersionId + ".json"));

                    IncreaseProgressValue();
                    AppendLog("Patching JAR...");
                    SetStatusLabel(FindResource("r_LabelStatusPatchingJar").ToString());
                    using (ZipFile zip = ZipFile.Read(Path.Combine(_tempDir, "temp.zip"))) {
                        using (ZipFile zipVersion = ZipFile.Read(Path.Combine(destDir, _customVersionId + ".jar"))) {
                            foreach (ZipEntry entry in zip) {
                                using (MemoryStream ms = new MemoryStream()) {
                                    entry.Extract(ms);
                                    ms.Position = 0;
                                    AppendLog($" Updating {entry.FileName}...");
                                    zipVersion.UpdateEntry(entry.FileName, ms);
                                    zipVersion.Save();
                                }
                            }

                            AppendLog(" Removing META-INF...");
                            zipVersion.RemoveSelectedEntries("META-INF/*");
                            zipVersion.Save();
                        }
                    }

                    IncreaseProgressValue();
                    AppendLog("Patching JSON...");
                    SetStatusLabel(FindResource("r_LabelStatusPatchingJson").ToString());
                    JObject jo = JObject.Parse(File.ReadAllText(Path.Combine(destDir, _customVersionId + ".json")));
                    jo["id"] = _customVersionId;
                    File.WriteAllText(Path.Combine(destDir, _customVersionId + ".json"), jo.ToString(Formatting.Indented));
                    IncreaseProgressValue();
                }

                if (_mcForgeVersion.InstallationMethod == McForgeInstallationType.INSTALLER) {
                    AppendLog("Installing Forge for version with INSTALLER...");
                    AppendLog("Downloading Forge...");
                    SetStatusLabel(FindResource("r_LabelStatusDownloadingForge").ToString());
                    _webClient.DownloadFile(_mcForgeVersion.DownloadUrl, Path.Combine(_tempDir, "installer.zip"));
                    IncreaseProgressValue();
                    JObject jobject = new JObject();
                    AppendLog("Processing installer...");
                    SetStatusLabel(FindResource("r_LabelStatusProcessingInstaller").ToString());
                    using (ZipFile zipInstaller = ZipFile.Read(Path.Combine(_tempDir, "installer.zip"))) {
                        foreach (ZipEntry entry in zipInstaller) {
                            if (entry.FileName == "install_profile.json") {
                                AppendLog(" Found install_profile.json. Getting version manifest...");
                                using (MemoryStream ms = new MemoryStream()) {
                                    entry.Extract(ms);
                                    ms.Position = 0;
                                    StreamReader reader = new StreamReader(ms);
                                    jobject = JObject.Parse(reader.ReadToEnd());
                                }
                            }

                            if (entry.FileName.Contains(".jar")) {
                                AppendLog(" Found Forge-universal.jar. Copying...");
                                entry.Extract(destDir, ExtractExistingFileAction.OverwriteSilently);
                            }
                        }
                    }

                    jobject["versionInfo"]["id"] = _customVersionId;
                    if (jobject["versionInfo"]["inheritsFrom"] == null) {
                        AppendLog("Inheritable version not setted. Copying original JAR...");
                        SetStatusLabel(FindResource("r_LabelStatusPatchingJar").ToString());
                        if (File.Exists(versionJar)) {
                            File.Delete(versionJar);
                        }

                        File.Copy(Path.Combine(originDir, _mcForgeVersion.McVersion + ".jar"),
                            Path.Combine(destDir, _customVersionId + ".jar"));

                        using (ZipFile zipJar = ZipFile.Read(Path.Combine(destDir, _customVersionId + ".jar"))) {
                            AppendLog(" Removing META-INF...");
                            zipJar.RemoveSelectedEntries("META-INF/*");
                            zipJar.Save();
                        }

                        SetStatusLabel(FindResource("r_LabelStatusPatchingJson").ToString());
                        JObject jo = JObject.Parse(File.ReadAllText(Path.Combine(originDir, _mcForgeVersion.McVersion + ".json")));
                        foreach (JObject obj in jo["libraries"]) {
                            (jobject["versionInfo"]["libraries"] as JArray).Add(obj);
                        }
                    }

                    IncreaseProgressValue();

                    AppendLog("Creating custom manifest...");
                    SetStatusLabel(FindResource("r_LabelStatusCreatingManifest").ToString());
                    File.WriteAllText(versionJson, jobject["versionInfo"].ToString(Formatting.Indented));

                    IncreaseProgressValue();
                    Lib forgeUniversal = new Lib {
                        Name = jobject["install"]["path"].ToString()
                    };
                    if (!Directory.Exists(new FileInfo(_mcLibs + forgeUniversal.GetPath()).DirectoryName)) {
                        Directory.CreateDirectory(new FileInfo(_mcLibs + forgeUniversal.GetPath()).DirectoryName);
                    }

                    AppendLog("Copying Universal into libraries...");
                    SetStatusLabel(FindResource("r_LabelStatusCopyingUniversal").ToString());
                    File.Copy(Path.Combine(destDir, jobject["install"]["filePath"].ToString()),
                        _mcLibs + forgeUniversal.GetPath(), true);
                    File.Delete(Path.Combine(destDir, jobject["install"]["filePath"].ToString()));
                    IncreaseProgressValue();

                    string[] files = {
                        "xz-1.8.jar", "LibraryUnpacker.jar"
                    };
                    foreach (string filename in files) {
                        using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Forgefier.Embedded.{filename}")) {
                            using (FileStream file = new FileStream(Path.Combine(_tempDir, filename), FileMode.Create, FileAccess.Write)) {
                                resource.CopyTo(file);
                            }
                        }
                    }

                    DownloadLibraries(_customVersionId);
                }

                IncreaseProgressValue();
                AppendLog("Updating profiles...");
                SetStatusLabel(FindResource("r_LabelStatusUpdatingProfiles").ToString());
                JObject profiles = JObject.Parse(File.ReadAllText(_mcDirectory + "/launcher_profiles.json"));
                profiles["selectedProfile"] = _customProfileName;
                if (profiles["profiles"][_customProfileName] != null) {
                    profiles["profiles"][_customProfileName]["lastVersionId"] = _customVersionId;
                } else {
                    (profiles["profiles"] as JObject).Add(_customProfileName, new JObject {
                        {"name", _customProfileName},
                        {"lastVersionId", _customVersionId}
                    });
                }

                File.WriteAllText(_mcDirectory + "/launcher_profiles.json", profiles.ToString(Formatting.Indented));
                AppendLog("Done!");
                SetStatusLabel(FindResource("r_LabelStatusDone").ToString());
                IncreaseProgressValue();
                SetExitState(true);
                Dispatcher.Invoke(() => {
                    MessageBox.Show(this,
                        string.Format(FindResource("r_MessageSuccess").ToString(), _mcForgeVersion.VersionWithTag, _mcForgeVersion.McVersion),
                        FindResource("r_TitleDone").ToString(), MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Close();
                });

            }
            catch (Exception exeption) {
                AppendLog($"An exception has occured: \n{exeption}");
                SetStatusLabel(FindResource("r_LabelStatusException").ToString());
                SetExitState(true);
            }
        }

        private void DownloadVersion(string version)
        {
            string path = Path.Combine(_mcVersions, version + @"\");
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            AppendLog($"Validating files for Minecraft {version}...");
            SetStatusLabel(FindResource("r_LabelStatusValidatingVersion").ToString());

            if (!File.Exists($@"{path}\{version}.json")) {
                RawVersionListManifest versionList =
                    RawVersionListManifest.ParseList(_webClient.DownloadString(@"https://launchermeta.mojang.com/mc/game/version_manifest.json"));
                AppendLog(" Downloading JSON...");
                _webClient.DownloadFile(
                    new Uri(versionList.GetVersion(version)?.ManifestUrl ?? string.Format(
                                "https://s3.amazonaws.com/Minecraft.Download/versions/{0}/{0}.json", version)),
                    string.Format(@"{0}\{1}\{1}.json", _mcVersions, version));
            }

            VersionManifest selectedVersionManifest = VersionManifest.ParseVersion(
                new DirectoryInfo(_mcVersions + @"\" + version), false);
            if ((!File.Exists($"{path}/{version}.jar")) && selectedVersionManifest.InheritsFrom == null) {
                AppendLog(" Downloading JAR...");
                _webClient.DownloadFile(new Uri(selectedVersionManifest.DownloadInfo?.Client.Url
                                                ?? string.Format(
                                                    "https://s3.amazonaws.com/Minecraft.Download/versions/{0}/{0}.jar",
                                                    version)),
                    string.Format("{0}/{1}/{1}.jar", _mcVersions, version));
            }

            if (selectedVersionManifest.InheritsFrom != null) {
                DownloadVersion(selectedVersionManifest.InheritsFrom);
            }
        }

        private void DownloadLibraries(string version)
        {
            VersionManifest selectedVersionManifest = VersionManifest.ParseVersion(
                new DirectoryInfo(_mcVersions + @"\" + version));
            Dictionary<DownloadEntry, bool> libsToDownload = new Dictionary<DownloadEntry, bool>();
            AppendLog("Validating libraries...");
            SetStatusLabel(FindResource("r_LabelStatusValidatingLibraries").ToString());
            foreach (Lib a in selectedVersionManifest.Libs) {
                if (!a.IsForWindows()) {
                    continue;
                }

                if (a.DownloadInfo == null) {
                    libsToDownload.Add(new DownloadEntry {
                        Path = a.GetPath(),
                        Url = a.GetUrl()
                    }, false);
                    continue;
                }

                foreach (DownloadEntry entry in a.DownloadInfo?.GetDownloadsEntries(dotMCLauncher.OperatingSystem.WINDOWS)) {
                    if (entry == null) {
                        continue;
                    }

                    entry.Path = entry.Path ?? a.GetPath();
                    entry.Url = entry.Url ?? a.Url;
                    libsToDownload.Add(entry, entry.IsNative);
                }
            }

            foreach (DownloadEntry entry in libsToDownload.Keys) {
                if (File.Exists(_mcLibs + @"\" + entry.Path)) {
                    continue;
                }
                AppendLog($"Processing {entry.Path}...");
                string directory = Path.GetDirectoryName(_mcLibs + @"\" + entry.Path);
                if (!File.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                try {
                    if (entry.Url != null && entry.Url.Contains("minecraftforge")) {
                        AppendLog(" Downloading compressed library...");
                        try {
                            _webClient.DownloadFile(entry.Url + ".pack.xz",
                                _mcLibs + @"\" + entry.Path + ".pack.xz");
                            AppendLog(" Decompressing with external process...");
                            Process process = new Process {
                                StartInfo = {
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    WorkingDirectory = _tempDir,
                                    FileName = _javaPath,
                                    Arguments =
                                        "-cp \"xz-1.8.jar;LibraryUnpacker.jar\" ru.dedepete.forgefier.LibraryUnpacker " +
                                        $"\"{_mcLibs + @"\" + entry.Path + ".pack.xz"}\" \"{_mcLibs + @"\" + entry.Path}\""
                                }
                            };
                            process.Start();
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();
                            AppendLog($"  Process exited with code {process.ExitCode}.");
                            if (process.ExitCode != 0) {
                                AppendLog($"  Output:\n{output}");
                            }

                            AppendLog(" Removing .PACK.XZ...");
                            File.Delete(_mcLibs + @"\" + entry.Path + ".pack.xz");
                            continue;
                        }
                        catch (Exception exception) {
                            AppendLog($" Failed to download compressed library: {exception.Message}");
                        }
                    }

                    AppendLog(" Downloading library...");
                    _webClient.DownloadFile(entry.Url ?? @"https://libraries.minecraft.net/" + entry.Path,
                        _mcLibs + @"\" + entry.Path);
                }
                catch (WebException exception) {
                    AppendLog($" Failed to download library: {exception.Message}");
                    AppendLog(" Removing corrupted file...");
                    File.Delete(_mcLibs + @"\" + entry.Path);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_allowQuit) {
                e.Cancel = true;
            }

            Directory.Delete(_tempDir, true);
        }

        private static string GetTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));

            if (Directory.Exists(path)) {
                Directory.Delete(path);
            }

            Directory.CreateDirectory(path);
            return path;
        }
    }
}
