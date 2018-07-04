using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace Forgefier
{
    public partial class MainWindow
    {
        private McForgeVersion _selectedMcForgeVersion => ComboBoxForgeVersions.SelectedItem as McForgeVersion;

        public MainWindow()
        {
            InitializeComponent();
            TextBoxPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft");

            string[] languageCodes = {
                "en", "ru-RU"
            };
            languageCodes.ToList().ForEach(
                code => {
                    CultureInfo cultureInfo = new CultureInfo(code);
                    System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem {
                        Header = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cultureInfo.NativeName),
                        IsCheckable = true
                    };
                    item.Checked += delegate {
                        foreach (System.Windows.Controls.MenuItem mi in MenuItemLanguage.Items) {
                            if (mi.Header != item.Header) {
                                mi.IsChecked = false;
                            }

                            item.IsEnabled = false;
                            ResourceDictionary dictionary = new ResourceDictionary {
                                Source = new Uri(
                                    cultureInfo.Name == "en" ? "Localizations/strings.xaml" : $"Localizations/strings.{cultureInfo.Name}.xaml",
                                    UriKind.Relative)
                            };

                            if (dictionary.Source != new Uri("Localizations/strings.xaml", UriKind.Relative)) {
                                ResourceDictionary originalDictionary = new ResourceDictionary {
                                    Source = new Uri("Localizations/strings.xaml", UriKind.Relative)
                                };
                                if (dictionary.Keys.Count != originalDictionary.Keys.Count) {
                                    foreach (string key in originalDictionary.Keys) {
                                        if (!dictionary.Contains(key)) {
                                            dictionary.Add(key, originalDictionary[key]);
                                        }
                                    }
                                }
                            }

                            ResourceDictionary oldDictionary = (from resourceDictionary in Application.Current.Resources.MergedDictionaries
                                where resourceDictionary.Source != null &&
                                      resourceDictionary.Source.OriginalString.StartsWith("Localizations/strings.")
                                select resourceDictionary).First();
                            if (oldDictionary != null) {
                                int index = Application.Current.Resources.MergedDictionaries.IndexOf(oldDictionary);
                                Application.Current.Resources.MergedDictionaries.Remove(oldDictionary);
                                Application.Current.Resources.MergedDictionaries.Insert(index, dictionary);
                            } else {
                                Application.Current.Resources.MergedDictionaries.Add(dictionary);
                            }
                        }
                    };
                    item.Unchecked += (sender, args) => { item.IsEnabled = true; };

                    if (!languageCodes.Contains(CultureInfo.InstalledUICulture.Name)) {
                        item.IsChecked = cultureInfo.Name == "en";
                        item.IsEnabled = cultureInfo.Name != "en";
                    } else {
                        item.IsChecked = string.Equals(cultureInfo.Name, CultureInfo.InstalledUICulture.Name,
                            StringComparison.InvariantCultureIgnoreCase);
                    }

                    MenuItemLanguage.Items.Add(item);
                });

            Title = $"{Application.ResourceAssembly.GetName().Name} {Application.ResourceAssembly.GetName().Version}";
        }

        private void ComboBoxForgeVersions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_selectedMcForgeVersion == null) {
                return;
            }

            TextBoxCustomVersionId.Text = $"{_selectedMcForgeVersion.McVersion}-forge{_selectedMcForgeVersion.FullVersion}";
        }

        private void ButtonInstall_Click(object sender, RoutedEventArgs e)
        {
            McForgeVersion mcForgeVersion = ComboBoxForgeVersions.SelectedItem as McForgeVersion;
            ButtonInstall.IsEnabled = false;
            new InstallationWindow(TextBoxPath.Text, mcForgeVersion, TextBoxCustomVersionId.Text, TextBoxCustomProfileName.Text,
                TextBoxCustomJavaExecutable.Text).ShowDialog();
            ButtonInstall.IsEnabled = true;
        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog =
                new FolderBrowserDialog {
                    Description = string.Format(FindResource("r_MessageSelectFolder").ToString()),
                    ShowNewFolderButton = false
                };
            DialogResult dialogResult = dialog.ShowDialog();
            if (dialogResult != System.Windows.Forms.DialogResult.OK) {
                return;
            }

            TextBoxPath.Text = dialog.SelectedPath;
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"https://github.com/dedepete/Forgefier");
        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(_selectedMcForgeVersion?.ChangelogUrl);
        }
    }
}
