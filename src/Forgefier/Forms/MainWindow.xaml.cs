using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

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
            ComboBoxLanguage.SelectedIndex = 0;
            Title = $"{Application.ResourceAssembly.GetName().Name} {Application.ResourceAssembly.GetName().Version}";
        }

        private void ExpanderExtendedOptions_Expanded(object sender, RoutedEventArgs e)
        {
            ExpanderExtendedOptions.Height = 83;
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
            CultureInfo cultureInfo = new CultureInfo("en-EN");
            switch (ComboBoxLanguage.SelectedIndex) {
                case 0:
                    cultureInfo = new CultureInfo("en-EN");
                    break;
                case 1:
                    cultureInfo = new CultureInfo("ru-RU");
                    break;

            }

            ResourceDictionary dict = new ResourceDictionary();
            switch (cultureInfo.Name) {
                case "ru-RU":
                    dict.Source = new Uri($"Resources/localization_strings.{cultureInfo.Name}.xaml", UriKind.Relative);
                    break;
                default:
                    dict.Source = new Uri("Resources/localization_strings.xaml", UriKind.Relative);
                    break;
            }

            ResourceDictionary oldDict = (from d in Application.Current.Resources.MergedDictionaries
                where d.Source != null && d.Source.OriginalString.StartsWith("Resources/localization_strings.")
                select d).First();
            if (oldDict != null) {
                int index = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                Application.Current.Resources.MergedDictionaries.Insert(index, dict);
            } else {
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
        }

        private void ButtonInstall_Click(object sender, RoutedEventArgs e)
        {
            McForgeVersion mcForgeVersion = ComboBoxForgeVersions.SelectedItem as McForgeVersion;
            ButtonInstall.IsEnabled = false;
            new InstallationWindow(TextBoxPath.Text, mcForgeVersion, TextBoxCustomVersionId.Text, TextBoxCustomProfileName.Text).ShowDialog();
            ButtonInstall.IsEnabled = true;
        }
    }
}
