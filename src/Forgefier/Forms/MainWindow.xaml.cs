using System;
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
            TextBoxPath.Focus();
            TextBoxPath.Select(TextBoxPath.Text.Length, 0);
            ComboBoxLanguage.Items.Add("EN");
            ComboBoxLanguage.Items.Add("RU");

            int index = 0;
            if (ComboBoxLanguage.Items.Contains(
                CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.ToUpperInvariant())) {
                index = ComboBoxLanguage.Items.IndexOf(CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.ToUpperInvariant());
            }

            ComboBoxLanguage.SelectedIndex = index;
            Title = $"{Application.ResourceAssembly.GetName().Name} {Application.ResourceAssembly.GetName().Version}";
        }

        private void ExpanderExtendedOptions_Expanded(object sender, RoutedEventArgs e)
        {
            ExpanderExtendedOptions.Height = 90;
            Height += ExpanderExtendedOptions.Height - 33;
        }

        private void ExpanderExtendedOptions_Collapsed(object sender, RoutedEventArgs e)
        {
            Height -= ExpanderExtendedOptions.Height - 33;
            ExpanderExtendedOptions.Height = 30;
        }

        private void ComboBoxForgeVersions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_selectedMcForgeVersion == null) {
                return;
            }

            TextBoxCustomVersionId.Text = $"{_selectedMcForgeVersion.McVersion}-forge{_selectedMcForgeVersion.FullVersion}";
        }

        private void CheckBoxCustomVersionId_CheckChanged(object sender, RoutedEventArgs e)
        {
            TextBoxCustomVersionId.IsEnabled = CheckBoxCustomVersionId.IsChecked ?? false;
        }

        private void CheckBoxCustomProfileName_CheckChanged(object sender, RoutedEventArgs e)
        {
            TextBoxCustomProfileName.IsEnabled = CheckBoxCustomProfileName.IsChecked ?? false;
        }

        private void ComboBoxLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            CultureInfo cultureInfo = new CultureInfo("en");
            switch (ComboBoxLanguage.SelectedIndex) {
                case 0:
                    cultureInfo = new CultureInfo("en");
                    break;
                case 1:
                    cultureInfo = new CultureInfo("ru-RU");
                    break;

            }

            ResourceDictionary dictionary = new ResourceDictionary();
            switch (cultureInfo.Name) {
                case "ru-RU":
                    dictionary.Source = new Uri($"Localizations/strings.{cultureInfo.Name}.xaml", UriKind.Relative);
                    break;
                default:
                    dictionary.Source = new Uri("Localizations/strings.xaml", UriKind.Relative);
                    break;
            }

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

            ResourceDictionary oldDictionary = (from d in Application.Current.Resources.MergedDictionaries
                where d.Source != null && d.Source.OriginalString.StartsWith("Localizations/strings.")
                select d).First();
            if (oldDictionary != null) {
                int index = Application.Current.Resources.MergedDictionaries.IndexOf(oldDictionary);
                Application.Current.Resources.MergedDictionaries.Remove(oldDictionary);
                Application.Current.Resources.MergedDictionaries.Insert(index, dictionary);
            } else {
                Application.Current.Resources.MergedDictionaries.Add(dictionary);
            }
        }

        private void ButtonInstall_Click(object sender, RoutedEventArgs e)
        {
            McForgeVersion mcForgeVersion = ComboBoxForgeVersions.SelectedItem as McForgeVersion;
            ButtonInstall.IsEnabled = false;
            new InstallationWindow(TextBoxPath.Text, mcForgeVersion, TextBoxCustomVersionId.Text, TextBoxCustomProfileName.Text).ShowDialog();
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
    }
}
