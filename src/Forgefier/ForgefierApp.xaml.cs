using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Xml;

namespace Forgefier
{
    public partial class ForgefierApp
    {
        private ForgefierApp()
        {
            InitializeComponent();
            CosturaUtility.Initialize();
        }

        // TODO: Remove these shitdozens of shitcode.
        [STAThread]
        private static void Main()
        {
            ForgefierApp app = new ForgefierApp();
            MainWindow window = new MainWindow();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                                                   SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // https://files.minecraftforge.net/maven/net/minecraftforge/forge/json
            // http://files.minecraftforge.net/maven/net/minecraftforge/forge/promotions.json
            // http://files.minecraftforge.net/maven/net/minecraftforge/forge/promotions_slim.json
            // https://files.minecraftforge.net/maven/net/minecraftforge/forge/maven-metadata.xml

            if (!CheckForInternetConnection()) {
                MessageBox.Show(window.FindResource("r_MessageNoInternet").ToString(), window.FindResource("r_TitleError").ToString(), MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                return;
            }

            WebClient wc = new WebClient();
            string xml =
                wc.DownloadString(
                    @"https://files.minecraftforge.net/maven/net/minecraftforge/forge/maven-metadata.xml");
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);

            Newtonsoft.Json.Linq.JObject builds = Newtonsoft.Json.Linq.JObject.Parse(wc.DownloadString(
                @"https://files.minecraftforge.net/maven/net/minecraftforge/forge/json"));
            builds = Newtonsoft.Json.Linq.JObject.FromObject(builds["number"]);

            Dictionary<McVersion, List<McForgeVersion>> list = new Dictionary<McVersion, List<McForgeVersion>>();

            foreach (XmlNode node in document.SelectNodes("metadata")) {
                foreach (XmlNode child in node.ChildNodes) {
                    if (child.Name != "versioning") {
                        continue;
                    }

                    foreach (XmlNode node2 in child.SelectNodes("versions")) {
                        foreach (XmlNode child2 in node2.ChildNodes) {
                            McForgeVersion mcForgeVersion = new McForgeVersion(child2.InnerText);
                            if (!list.ContainsKey(mcForgeVersion.McVersion)) {
                                list.Add(mcForgeVersion.McVersion, new List<McForgeVersion>());
                            }

                            if (builds[$"{mcForgeVersion.Version.Revision}"] == null) {
                                continue;
                            }

                            mcForgeVersion.JObjectBuild = Newtonsoft.Json.Linq.JObject.FromObject(builds[$"{mcForgeVersion.Version.Revision}"]);
                            list[mcForgeVersion.McVersion].Add(mcForgeVersion);
                        }
                    }

                }
            }

            list = list.OrderByDescending(item => item.Key).ToDictionary(item => item.Key, item => item.Value);
            foreach (KeyValuePair<McVersion, List<McForgeVersion>> lVersions in list) {
                list[lVersions.Key].Sort((a, b) => -1 * a.CompareTo(b));
            }


            Newtonsoft.Json.Linq.JObject promo = Newtonsoft.Json.Linq.JObject.Parse(new WebClient().DownloadString(
                @"http://files.minecraftforge.net/maven/net/minecraftforge/forge/promotions.json"));
            Dictionary<string, List<Tuple<McForgePromoVersion, McForgeVersion>>> promoVersions =
                new Dictionary<string, List<Tuple<McForgePromoVersion, McForgeVersion>>>();
            foreach (Newtonsoft.Json.Linq.JProperty property in promo["promos"]) {
                string mcVersion = property.Value["mcversion"].ToString();
                if (!promoVersions.ContainsKey(mcVersion)) {
                    promoVersions.Add(mcVersion, new List<Tuple<McForgePromoVersion, McForgeVersion>>());
                }

                McForgePromoVersion mcForgePromoVersion;
                if (!McForgePromoVersion.TryParse(property.Name, out mcForgePromoVersion)) {
                    continue;
                }

                McForgeVersion mcForgeVersion = new McForgeVersion($"{mcVersion}-{property.Value["version"]}");
                if (builds[$"{mcForgeVersion.Version.Revision}"] == null) {
                    continue;
                }

                mcForgeVersion.JObjectBuild = Newtonsoft.Json.Linq.JObject.FromObject(builds[$"{mcForgeVersion.Version.Revision}"]);

                promoVersions[mcVersion].Add(new Tuple<McForgePromoVersion, McForgeVersion>(mcForgePromoVersion,
                    mcForgeVersion));
            }

            window.СomboBoxPromo.SelectionChanged += delegate {
                if (window.СomboBoxPromo.SelectedItem == null) {
                    return;
                }

                window.ComboBoxForgeVersions.Items.Clear();

                if (!window.CheckBoxDisplayOnlyRecommended.IsChecked ?? false) {
                    foreach (McForgeVersion version in list[new McVersion(window.СomboBoxPromo.SelectedItem.ToString())]) {
                        window.ComboBoxForgeVersions.Items.Add(version);
                    }
                } else {
                    foreach (List<Tuple<McForgePromoVersion, McForgeVersion>> version in promoVersions.Values) {
                        foreach (Tuple<McForgePromoVersion, McForgeVersion> tuple in version) {
                            if (tuple.Item1 != window.СomboBoxPromo.SelectedItem) {
                                continue;
                            }

                            window.ComboBoxForgeVersions.Items.Add(list[tuple.Item2.McVersion]
                                .First(i => i.Version == tuple.Item2.Version));
                        }
                    }
                }

                window.ComboBoxForgeVersions.SelectedIndex = 0;
            };
            window.СomboBoxPromo.SelectedIndex = 0;

            window.CheckBoxDisplayOnlyRecommended.Checked += delegate {
                window.ComboBoxForgeVersions.IsEnabled = false;
                List<McForgePromoVersion> versions = new List<McForgePromoVersion>();
                foreach (List<Tuple<McForgePromoVersion, McForgeVersion>> version in promoVersions.Values) {
                    foreach (Tuple<McForgePromoVersion, McForgeVersion> tuple in version) {
                        versions.Add(tuple.Item1);
                    }
                }

                versions.Sort();

                window.СomboBoxPromo.ItemsSource = versions;
                window.СomboBoxPromo.SelectedIndex = 0;
            };

            window.CheckBoxDisplayOnlyRecommended.Unchecked += delegate {
                window.ComboBoxForgeVersions.IsEnabled = true;
                window.СomboBoxPromo.ItemsSource = list.Keys;
                window.СomboBoxPromo.SelectedIndex = 0;
            };

            window.CheckBoxDisplayOnlyRecommended.IsChecked = true;
            app.Run(window);
        }

        private static bool CheckForInternetConnection()
        {
            try {
                using (WebClient client = new WebClient()) {
                    using (client.OpenRead("https://captive.apple.com/generate_204")) {
                        return true;
                    }
                }
            }
            catch {
                return false;
            }
        }
    }

}