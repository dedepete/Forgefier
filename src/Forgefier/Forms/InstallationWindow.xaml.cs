﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        private McForgeVersion _mcForgeVersion { get; }
        private string _customVersionId { get; }
        private string _customProfileName { get; }
        private readonly WebClient _webClient = new WebClient();
        private bool _allowQuit { get; set; }

        public InstallationWindow(string mcDirectory, McForgeVersion forgeVersion, string versionId, string profileName)
        {
            InitializeComponent();
            _mcDirectory = mcDirectory;
            _mcVersions = _mcDirectory + "/versions/";
            _mcLibs = _mcDirectory + "/libraries/";
            _mcForgeVersion = forgeVersion;
            _customVersionId = versionId;
            _customProfileName = profileName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
                string mcVersions = _mcDirectory + "/versions/",
                    destDir = Path.Combine(mcVersions, _customVersionId + @"\"),
                    originDir = Path.Combine(mcVersions, _mcForgeVersion.McVersion + @"\");
                string versionJar = Path.Combine(destDir, _customVersionId + ".jar"),
                    versionJson = Path.Combine(destDir, _customVersionId + ".json");
                DownloadVersion(_mcForgeVersion.McVersion.ToString());
                if (!Directory.Exists(destDir)) {
                    Directory.CreateDirectory(destDir);
                }

                if (_mcForgeVersion.InstallationMethod == McForgeInstallationType.LEGACY) {
                    SetProgressMax(5);
                    AppendLog("Installing Forge for LEGACY version...");
                    AppendLog("Downloading Forge...");
                    _webClient.DownloadFile(_mcForgeVersion.DownloadUrl, Path.Combine(destDir, "temp.zip"));

                    AppendLog($"Creating custom `{_customVersionId}` version from `{_mcForgeVersion.McVersion}`...");
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
                    AppendLog("Pathing JAR...");
                    using (ZipFile zip = ZipFile.Read(Path.Combine(destDir, "temp.zip"))) {
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
                    AppendLog($"Pathing `{_customVersionId}`.json...");
                    JObject jo = JObject.Parse(File.ReadAllText(Path.Combine(destDir, _customVersionId + ".json")));
                    jo["id"] = _customVersionId;
                    File.WriteAllText(Path.Combine(destDir, _customVersionId + ".json"), jo.ToString(Formatting.Indented));
                    File.Delete(Path.Combine(destDir, "temp.zip"));
                    IncreaseProgressValue();
                }

                if (_mcForgeVersion.InstallationMethod == McForgeInstallationType.INSTALLER) {
                    SetProgressMax(6);
                    AppendLog("Installing Forge for version with INSTALLER...");
                    AppendLog("Downloading Forge...");
                    _webClient.DownloadFile(_mcForgeVersion.DownloadUrl, Path.Combine(destDir, "installer.zip"));
                    IncreaseProgressValue();
                    JObject jobject = new JObject();
                    AppendLog("Processing installer...");
                    using (ZipFile zipInstaller = ZipFile.Read(Path.Combine(destDir, "installer.zip"))) {
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
                                AppendLog(" Found Forge universal. Copying...");
                                entry.Extract(destDir, ExtractExistingFileAction.OverwriteSilently);
                            }
                        }
                    }

                    File.Delete(Path.Combine(destDir, "installer.zip"));

                    jobject["versionInfo"]["id"] = _customVersionId;
                    if (jobject["versionInfo"]["inheritsFrom"] == null) {
                        AppendLog("Inheritable version not setted. Copying original JAR...");
                        if (File.Exists(versionJar)) {
                            File.Delete(versionJar);
                        }

                        File.Copy(Path.Combine(originDir, _mcForgeVersion.McVersion + ".jar"),
                            Path.Combine(destDir, _customVersionId + ".jar"));
                    }

                    IncreaseProgressValue();

                    AppendLog("Creating custom version JSON...");
                    File.WriteAllText(versionJson, jobject["versionInfo"].ToString(Formatting.Indented));

                    IncreaseProgressValue();
                    Lib forgeUniversal = new Lib {
                        Name = jobject["install"]["path"].ToString()
                    };
                    if (!Directory.Exists(new FileInfo(_mcLibs + forgeUniversal.GetPath()).DirectoryName)) {
                        Directory.CreateDirectory(new FileInfo(_mcLibs + forgeUniversal.GetPath()).DirectoryName);
                    }

                    AppendLog("Copying Forge universal into libraries...");
                    File.Copy(Path.Combine(destDir, jobject["install"]["filePath"].ToString()),
                        _mcLibs + forgeUniversal.GetPath(), true);
                    File.Delete(Path.Combine(destDir, jobject["install"]["filePath"].ToString()));
                    IncreaseProgressValue();
                    DownloadLibraries(_customVersionId);
                }

                IncreaseProgressValue();
                AppendLog("Updating launcher profiles...");
                JObject profiles = JObject.Parse(File.ReadAllText(_mcDirectory + "/launcher_profiles.json"));
                if (profiles["profiles"][_customProfileName] != null) {
                    profiles["profiles"][_customProfileName]["lastVersionId"] = _customVersionId;
                } else {
                    (profiles["profiles"] as JObject).Add(_customProfileName, new JObject {
                        {"name", _customProfileName},
                        {"name", _customVersionId}
                    });
                }

                File.WriteAllText(_mcDirectory + "/launcher_profiles.json", profiles.ToString(Formatting.Indented));
                AppendLog("Done!");
                IncreaseProgressValue();
                SetExitState(true);
                Dispatcher.Invoke(() => {
                    MessageBox.Show(this, string.Format(FindResource("r_MessageSuccess").ToString(), _mcForgeVersion.VersionWithTag, _mcForgeVersion.McVersion), FindResource("r_TitleDone").ToString(), MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Close();
                });

            }
            catch (Exception exeption) {
                AppendLog($"An exception has occured: \n{exeption}");
                SetExitState(true);
            }
        }

        private void DownloadVersion(string version)
        {
            string path = Path.Combine(_mcVersions, version + @"\");
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            AppendLog($"Validating version `{version}`...");

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
                if (File.Exists(_mcLibs + @"\" + entry.Path)) continue;
                AppendLog($"Processing {entry.Path}...");
                string directory = Path.GetDirectoryName(_mcLibs + @"\" + entry.Path);
                if (!File.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                try {
                    if (entry.Url != null && entry.Url.Contains("minecraftforge")) {
                        AppendLog(" Detected library from Forge repository. Downloading compressed file...");
                        try {
                            _webClient.DownloadFile(entry.Url + ".pack.xz",
                                _mcLibs + @"\" + entry.Path + ".pack.xz");
                            AppendLog(" Decompressing with external process...");
                            Process process = new Process {
                                StartInfo = {
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    FileName = @".\ForgefierData\unpacker\unpack.bat",
                                    Arguments =
                                        $"\"{_mcLibs + @"\" + entry.Path + ".pack.xz"}\" \"{_mcLibs + @"\" + entry.Path}\""
                                }
                            };
                            process.Start();
                            process.WaitForExit();
                            AppendLog($"  Process exited with code {process.ExitCode}.");
                            AppendLog(" Removing .PACK.XZ...");
                            File.Delete(_mcLibs + @"\" + entry.Path + ".pack.xz");
                            continue;
                        }
                        catch {
                            AppendLog(" Failed to download compressed file.");
                        }
                    }

                    AppendLog(" Downloading decompressed JAR...");
                    _webClient.DownloadFile(entry.Url ?? @"https://libraries.minecraft.net/" + entry.Path,
                        _mcLibs + @"\" + entry.Path);
                }
                catch (WebException exception) {
                    AppendLog($" Download failure. {exception.Message}");
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
        }
    }
}
