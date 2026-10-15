using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HowToUseWindow : Window
    {
        public HowToUseWindow()
        {
            InitializeComponent();

            this.Title = LanguageResource.LanguageDictionary["Menu.Help.Help_HowToUse"];

            foreach (TextBlock textBlock in TextBlocks.Children)
            {
                textBlock.Text = new string(' ', 8) + LanguageResource.LanguageDictionary[$"HowToUse.{textBlock.Name}"];
            }

            this.Background = ThemeColors.Background;
            this.Foreground = ThemeColors.Foreground;
        }
    }
}
