using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VectorCharMap
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            var file = new System.IO.FileInfo("logo-256_256.png");

            if (file.Exists)
            {
                Uri uriSource = new Uri(file.FullName);

                logo.Source = new BitmapImage(uriSource);
            }

            this.Title = LanguageResource.LanguageDictionary["Menu.Help.Help_About"];

            this.versionText.Text = LanguageResource.LanguageDictionary["About.Version"] + ":";
            this.developedBy.Text = LanguageResource.LanguageDictionary["About.DevelopedBy"];
            this.unicodeLicenseText.Text = LanguageResource.LanguageDictionary["About.LicenseInformationAreaText"];

            foreach (ContentControl element in this.LicenseInformationArea.Children)
            {
                element.Content = LanguageResource.LanguageDictionary[$"About.{element.Name}"];
            }

            this.Background = ThemeColors.Background;
            this.Foreground = ThemeColors.Foreground;

            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.Owner.Activate();
            base.OnClosing(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var process = new System.Diagnostics.Process();

            process.StartInfo = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = "cmd",
                Arguments = "/c start " + "https://www.unicode.org/copyright.html",
                CreateNoWindow = true
            };

            process.Start();
        }

        private void LicenseButton_Click(object sender, RoutedEventArgs e)
        {
            var process = new System.Diagnostics.Process();

            process.StartInfo = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = "notepad",
                Arguments = ".\\UCD\\Unicode License Agreement.txt"
            };

            process.Start();
        }
    }
}
