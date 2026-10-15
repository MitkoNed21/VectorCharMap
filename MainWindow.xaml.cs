using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VectorCharMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FontFamily selectedFont;

        public FontFamily SelectedFont
        {
            get => selectedFont;
            set
            {
                this.selectedFont = value;

                this.CharacterMap = new CharacterMap(selectedFont);
            }
        }
        public CharacterMap CharacterMap { get; set; }

        public int OutputCharSize { get; set; } = 11;

        public int ColumnCount => (int)Math.Floor(this.ActualWidth / (this.PreviewCharSize * 1.61803398875));
        public int RowCount => (int)Math.Floor(this.ActualHeight / (this.PreviewCharSize * 1.61803398875));

        private List<string[,]> groupedCharacters;
        double charSize => PreviewCharSize / 1;
        private System.Collections.Concurrent.ConcurrentDictionary<string, Rect> charactersWithGraphic = new System.Collections.Concurrent.ConcurrentDictionary<string, Rect>();

        private System.ComponentModel.BackgroundWorker loadCharactersBackgroundWorker = new System.ComponentModel.BackgroundWorker();
        private bool loadingNewFont = false;

        private System.Collections.Concurrent.ConcurrentDictionary<string, string> characterNameData;
        private bool isOutlineCheckBoxChecked = false;

        private int PreviewCharSize;

        public MainWindow()
        {
            this.Width = 1014;
            InitializeComponent();

            this.Language_English.IsChecked = true;
            LanguageResource.LoadLanguage("English");
            this.UpdateElementsLanguage();

            this.PreviewCharSize = 72;
            this.OutputCharSize = 72;

            this.fontSelectionComboBox.SelectionChanged += FontSelectionComboBox_SelectionChanged;

            var fontNames = Fonts.SystemFontFamilies.Select(ff =>
            {
                var fontFamilyName = ff.Source;
                var hasGlyphTypeface = new Typeface(fontFamilyName).TryGetGlyphTypeface(out var glyphTypeface);

                if (hasGlyphTypeface)
                    return fontFamilyName;

                return "";
            }).OrderBy(fn => fn).Distinct().ToList();
            fontNames.RemoveAll(element => element == "");

            this.fontSelectionComboBox.ItemsSource = fontNames;

            this.fontWeightComboBox.SelectionChanged += FontWeightComboBox_SelectionChanged;
            this.fontStyleComboBox.SelectionChanged += FontStyleComboBox_SelectionChanged;
            this.fontStretchComboBox.SelectionChanged += FontStretchComboBox_SelectionChanged;

            this.fontSelectionComboBox.SelectedIndex = 0;

            Mouse.AddMouseWheelHandler(this, OnMouseWheelScroll);

            this.previewFontSizeTextBox.KeyUp += PreviewFontSizeTextBox_KeyUp;
            this.previewFontSizeTextBox.Text = this.PreviewCharSize.ToString();

            this.outputFontSizeTextBox.TextChanged += OutputFontSizeTextBox_TextChanged;
            this.outputFontSizeTextBox.Text = this.OutputCharSize.ToString();

            this.MinWidth = 22 +
                            this.fontSelectionComboBox.Width + 22 +
                            this.fontWeightComboBox.Width + 22 +
                            this.fontStyleComboBox.Width + 22 +
                            this.fontStretchComboBox.Width + 22 +
                            this.previewFontSizeTextBox.Width + 22 +
                            this.outputFontSizeTextBox.Width + 22 + 22 + 23;

            

            charViewerScrollBar.Minimum = 1;

            charViewerScrollBar.LargeChange = 1;
            charViewerScrollBar.SmallChange = 1;

            charViewerScrollBar.ViewportSize = 2;

            for (int i = 0; i < 8; i++)
            {
                this.charGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < 3; i++)
            {
                this.charGrid.RowDefinitions.Add(new RowDefinition());
            }

            loadCharactersBackgroundWorker.RunWorkerCompleted += (sender, e) =>
            {
                this.IsEnabled = true;
                characterLoadingProgress.Visibility = Visibility.Collapsed;
                FillCharacterGrid();
                LoadChars();
                Mouse.AddMouseWheelHandler(this, OnMouseWheelScroll);
            };

            loadCharactersBackgroundWorker.DoWork += GetCharactersWithGraphic;

            loadCharactersBackgroundWorker.WorkerSupportsCancellation = true;

            loadCharactersBackgroundWorker.WorkerReportsProgress = true;

            loadCharactersBackgroundWorker.ProgressChanged += (sender, e) =>
            {
                characterLoadingProgress.Value = e.ProgressPercentage;
            };

            this.SizeChanged += MainWindow_SizeChanged;

            this.Theme_Dark_Click(this, null);

            this.Debug_Click(this, null);

            readfile(".\\UCD\\extracted\\DerivedName.txt");
        }

        private void GetCharactersWithGraphic(object sender, DoWorkEventArgs e)
        {
            IEnumerable<int> characters = this.CharacterMap.RequestCurrentMapCharacters();

            var charactersCount = characters.Count();
            var checkedCharacters = 0.0;
            foreach (var character in characters)
            {
                if (loadingNewFont)
                {
                    return;
                }

                var text = char.ConvertFromUtf32(character);

                var pathGeometry = new FormattedText(text,
                                                     CultureInfo.GetCultureInfo("en-US"),
                                                     System.Windows.FlowDirection.LeftToRight,
                                                     new Typeface(SelectedFont,
                                                                  CharacterMap.CurrentFontStyle,
                                                                  CharacterMap.CurrentFontWeight,
                                                                  CharacterMap.CurrentFontStretch),
                                                     emSize: charSize,
                                                     foreground: ThemeColors.Foreground, 1)
                                                         .BuildGeometry(new Point(0, 0));

                loadCharactersBackgroundWorker.ReportProgress((int)(++checkedCharacters / charactersCount * 100));
                if (pathGeometry.ToString() == "")
                {
                    continue;
                }

                charactersWithGraphic.TryAdd(text, pathGeometry.Bounds);
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var colCount = Math.Floor((this.ActualWidth / 122) * (OutputCharSize / (double)PreviewCharSize));
            var rowCount = Math.Floor((this.ActualHeight / 175) * (OutputCharSize / (double)PreviewCharSize));

            var gridChanges = 0;

            if (colCount != this.charGrid.ColumnDefinitions.Count)
            {
                this.charGrid.ColumnDefinitions.Clear();

                for (int i = 0; i < colCount; i++)
                {
                    this.charGrid.ColumnDefinitions.Add(new ColumnDefinition());
                }

                gridChanges++;
            }

            if (rowCount != this.charGrid.RowDefinitions.Count)
            {
                this.charGrid.RowDefinitions.Clear();

                for (int i = 0; i < rowCount; i++)
                {
                    this.charGrid.RowDefinitions.Add(new RowDefinition());
                }

                gridChanges++;
            }

            if (gridChanges > 0 && this.IsLoaded)
            {
                FillCharacterGrid();
                LoadChars((int)charViewerScrollBar.Value);
            }
        }

        private void OnMouseWheelScroll(object sender, MouseWheelEventArgs e)
        {
            charViewerScrollBar.Value = charViewerScrollBar.Value + e.Delta / -120;
            LoadChars((int)charViewerScrollBar.Value);
        }

        private void PreviewFontSizeTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int newCharSize;
                if (int.TryParse(this.previewFontSizeTextBox.Text, out newCharSize))
                {
                    if (newCharSize < 36)
                    {
                        newCharSize = 36;
                        previewFontSizeTextBox.Text = newCharSize.ToString();
                    }

                    if (newCharSize > 144)
                    {
                        newCharSize = 144;
                        previewFontSizeTextBox.Text = newCharSize.ToString();
                    }

                    PreviewCharSize = newCharSize;

                    var colCount = Math.Floor((this.ActualWidth / 122) * (OutputCharSize / (double)PreviewCharSize));
                    var rowCount = Math.Floor((this.ActualHeight / 175) * (OutputCharSize / (double)PreviewCharSize));

                    if (colCount != this.charGrid.ColumnDefinitions.Count)
                    {
                        this.charGrid.ColumnDefinitions.Clear();

                        for (int i = 0; i < colCount; i++)
                        {
                            this.charGrid.ColumnDefinitions.Add(new ColumnDefinition());
                        }
                    }

                    if (rowCount != this.charGrid.RowDefinitions.Count)
                    {
                        this.charGrid.RowDefinitions.Clear();

                        for (int i = 0; i < rowCount; i++)
                        {
                            this.charGrid.RowDefinitions.Add(new RowDefinition());
                        }
                    }

                    FillCharacterGrid();
                    LoadChars((int)charViewerScrollBar.Value);
                }
            }
        }

        private void OutputFontSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int newCharSize;
            if (int.TryParse(this.outputFontSizeTextBox.Text, out newCharSize))
            {
                this.OutputCharSize = newCharSize;
            }
        }

        private void FontWeightComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var result = this.CharacterMap.FontWeights.Find(fw => fw.ToOpenTypeWeight().ToString() == (string)((System.Windows.Controls.ComboBox)sender).SelectedValue);
            this.CharacterMap.CurrentFontWeight = result;

            LoadChars((int)charViewerScrollBar.Value);
        }

        private void FontStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var result = this.CharacterMap.FontStyles.Find(fs => fs.ToString() == (string)((System.Windows.Controls.ComboBox)sender).SelectedValue);
            this.CharacterMap.CurrentFontStyle = result;

            LoadChars((int)charViewerScrollBar.Value);
        }

        private void FontStretchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var result = this.CharacterMap.FontStretches.Find(fs => fs.ToString() == (string)((System.Windows.Controls.ComboBox)sender).SelectedValue);
            this.CharacterMap.CurrentFontStretch = result;

            LoadChars((int)charViewerScrollBar.Value);
        }

        private void FontSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.charGrid.Dispatcher.InvokeAsync(() =>
            {
                loadingNewFont = true;
                charViewerScrollBar.Value = 1;

                var fontName = (string)((System.Windows.Controls.ComboBox)sender).SelectedValue;

                SelectedFont = Fonts.SystemFontFamilies.FirstOrDefault(ff => ff.Source == fontName);

                this.fontWeightComboBox.ItemsSource = CharacterMap.FontWeights.Select(fw => fw.ToOpenTypeWeight().ToString());
                this.fontWeightComboBox.SelectedIndex = 0;
                if (this.fontWeightComboBox.SelectedItem != null)
                {
                    this.CharacterMap.CurrentFontWeight = FontWeight.FromOpenTypeWeight(
                        int.Parse((string)this.fontWeightComboBox.SelectedItem));
                }

                this.fontStyleComboBox.ItemsSource = CharacterMap.FontStyles.Select(fs => fs.ToString());
                this.fontStyleComboBox.SelectedIndex = 0;
                if (this.fontStyleComboBox.SelectedItem != null)
                {
                    this.CharacterMap.CurrentFontStyle = (FontStyle)typeof(FontStyles)
                    .GetProperty(
                            (string)this.fontStyleComboBox.SelectedItem,
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                    .GetValue(null);
                }

                this.fontStretchComboBox.ItemsSource = CharacterMap.FontStretches.Select(fs => fs.ToString());
                this.fontStretchComboBox.SelectedIndex = 0;
                if (this.fontStretchComboBox.SelectedItem != null)
                {
                    this.CharacterMap.CurrentFontStretch = (FontStretch)typeof(FontStretches)
                    .GetProperty(
                            (string)this.fontStretchComboBox.SelectedItem,
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                    .GetValue(null);
                }

                charactersWithGraphic.Clear();

                characterLoadingProgress.Visibility = Visibility.Visible;

                characterLoadingProgress.Value = 0;

                Mouse.RemoveMouseWheelHandler(this, OnMouseWheelScroll);

                loadingNewFont = false;
                this.IsEnabled = false;
                loadCharactersBackgroundWorker.RunWorkerAsync();
            });

            FillCharacterGrid();
            LoadChars();
        }

        private void FillCharacterGrid()
        {
            groupedCharacters = new List<string[,]>();
            groupedCharacters.Add(null);
            int c = 0, r = 0, p = 0;

            charGrid.Dispatcher.Invoke(() =>
            {
                foreach (var character in charactersWithGraphic.OrderBy(c => string.Join("", Encoding.UTF32.GetBytes(c.Key).Select(b => b.ToString("X2")).Reverse())))
                {
                    if (groupedCharacters[p] == null)
                    {
                        groupedCharacters.Insert(p, new string[charGrid.ColumnDefinitions.Count, charGrid.RowDefinitions.Count]);

                        if (groupedCharacters.Where(a => a == null).Count() == 1)
                        {
                            var oldCount = groupedCharacters.Count;

                            while (groupedCharacters.Count == 2 * oldCount + 1)
                            {
                                groupedCharacters.Add(null);
                            }
                        }
                    }

                    var columnsToContainItemContainerCount = (int)Math.Min(Math.Ceiling(character.Value.Width / (this.Width / charGrid.ColumnDefinitions.Count)), charGrid.ColumnDefinitions.Count);

                    if (c + columnsToContainItemContainerCount > charGrid.ColumnDefinitions.Count)
                    {
                        c = 0;
                        r++;

                        if (r == charGrid.RowDefinitions.Count)
                        {
                            r = 0;
                            p++;

                            if (groupedCharacters[p] == null)
                            {
                                groupedCharacters.Insert(p, new string[charGrid.ColumnDefinitions.Count, charGrid.RowDefinitions.Count]);

                                if (groupedCharacters.Where(a => a == null).Count() == 1)
                                {
                                    var oldCount = groupedCharacters.Count;

                                    while (groupedCharacters.Count == 2 * oldCount + 1)
                                    {
                                        groupedCharacters.Add(null);
                                    }
                                }
                            }
                        }
                    }

                    
                    groupedCharacters[p][c++, r] = character.Key;

                    if (columnsToContainItemContainerCount > 1)
                    {
                        c += columnsToContainItemContainerCount - 1;
                    }

                    var colDiff = c - charGrid.ColumnDefinitions.Count;
                    if (colDiff >= 0)
                    {
                        c = colDiff;

                        if (r == charGrid.RowDefinitions.Count - 1)
                        {
                            r = 0;
                            p++;
                            continue;
                        }

                        r++;
                    }
                }
            });


            charViewerScrollBar.Maximum = groupedCharacters.Count - 1;
        }

        private async void LoadChars(int pageNum = 1)
        {
            if (!this.IsLoaded) return;
            await this.charGrid.Dispatcher.InvokeAsync(() =>
            {
                this.charGrid.Children.Clear();

                int c = 0, r = 0;

                while (r != charGrid.RowDefinitions.Count)
                {
                    var textBlock = new TextBlock()
                    {
                        Text = groupedCharacters[pageNum - 1][c, r],
                        FlowDirection = System.Windows.FlowDirection.LeftToRight,
                        FontFamily = SelectedFont,
                        FontStyle = CharacterMap.CurrentFontStyle,
                        FontWeight = CharacterMap.CurrentFontWeight,
                        FontStretch = CharacterMap.CurrentFontStretch,
                        FontSize = charSize,
                        Foreground = ThemeColors.Foreground,

                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(2)
                    };

                    if (Encoding.UTF8.GetBytes(textBlock.Text).Length == 0)
                    {
                        c++;

                        if (c >= charGrid.ColumnDefinitions.Count)
                        {
                            c = 0;
                            r++;
                        }

                        continue;
                    }

                    var pathGeometry = new FormattedText(textBlock.Text,
                                                         CultureInfo.GetCultureInfo("en-US"),
                                                         System.Windows.FlowDirection.LeftToRight,
                                                         new Typeface(SelectedFont,
                                                                      CharacterMap.CurrentFontStyle,
                                                                      CharacterMap.CurrentFontWeight,
                                                                      CharacterMap.CurrentFontStretch),
                                                         emSize: charSize,
                                                         foreground: Brushes.Blue, 1)
                                                             .BuildGeometry(new Point(0, 0));

                    var path = new Path() { Fill = ThemeColors.Foreground, Tag = textBlock.Text };

                    if (FontOutLinedCheckBox.IsChecked.Value)
                    {
                        path.Data = pathGeometry.GetWidenedPathGeometry(new Pen(null, 1));
                    }
                    else
                    {
                        path.Data = pathGeometry.GetOutlinedPathGeometry();
                    }

                    var bounds = pathGeometry.GetOutlinedPathGeometry().Bounds;

                    path.Data.Transform = new TranslateTransform(-bounds.X, -bounds.Y);

                    var itemContainer = new DataGridCell();
                    itemContainer.Content = new Border()
                    {
                        Child = path,
                        ClipToBounds = false,
                        MinHeight = Math.Max(bounds.Height, itemContainer.ActualHeight),
                        MinWidth = Math.Max(bounds.Width, itemContainer.ActualWidth),

                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    itemContainer.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                    itemContainer.Margin = new Thickness(1);

                    var theme = "Light";

                    if (this.Theme_Dark.IsChecked)
                    {
                        theme = "Dark";
                    }

                    itemContainer.Style = (Style)this.FindResource($"DataGridCell{theme}Style");

                    itemContainer.MouseMove += ItemContainer_MouseMove;

                    charGrid.Children.Add(itemContainer);

                    var columnsToContainItemContainerCount = (int)Math.Min(Math.Ceiling(bounds.Width / (this.Width / charGrid.ColumnDefinitions.Count)), charGrid.ColumnDefinitions.Count);

                    if (c + columnsToContainItemContainerCount > charGrid.ColumnDefinitions.Count)
                    {
                        c = 0;
                        r++;
                    }

                    Grid.SetColumn(itemContainer, c++);
                    Grid.SetRow(itemContainer, r);

                    if (columnsToContainItemContainerCount > 1)
                    {
                        Grid.SetColumnSpan(itemContainer, columnsToContainItemContainerCount);
                        c += columnsToContainItemContainerCount - 1;
                    }


                    if (c >= charGrid.ColumnDefinitions.Count)
                    {
                        c = 0;
                        r++;
                    }
                }
            });
        }

        [STAThread]
        private void ItemContainer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                var dataGridCell = (DataGridCell)sender;

                var symbol = (Path)((Border)dataGridCell.Content).Child;

                var directory = new System.IO.DirectoryInfo(Environment.GetEnvironmentVariable("TEMP"));

                var character = (string)symbol.Tag;
                debugText.Text = character + ":" + char.GetUnicodeCategory(Encoding.UTF32.GetChars(Encoding.UTF32.GetBytes(character))[0]) + "\n" +
                    Encoding.UTF32.EncodingName;
                var file = WriteSvgFile((string)symbol.Tag, directory.FullName, "shape");

                var filePaths = new System.Collections.Specialized.StringCollection();
                filePaths.Add(file.FullName);
                var obj = new System.Windows.DataObject();
                obj.SetFileDropList(filePaths);
                DragDrop.DoDragDrop(dataGridCell, obj, System.Windows.DragDropEffects.Copy);
            }
        }

        private System.IO.FileInfo WriteSvgFile(string character, string destinationFolderPath, string fileName)
        {
            var pathGeometry = new FormattedText(character,
                                                 CultureInfo.GetCultureInfo("en-US"),
                                                 System.Windows.FlowDirection.LeftToRight,
                                                 new Typeface(SelectedFont,
                                                              CharacterMap.CurrentFontStyle,
                                                              CharacterMap.CurrentFontWeight,
                                                              CharacterMap.CurrentFontStretch),
                                                 emSize: OutputCharSize,
                                                 foreground: Brushes.Blue, 1)
                                                     .BuildGeometry(new Point(0, 0));

            if (isOutlineCheckBoxChecked)
            {
                pathGeometry = pathGeometry.GetWidenedPathGeometry(new Pen(Brushes.Black, thickness: 1), 0.1, ToleranceType.Absolute);
            }
            else
            {
                pathGeometry = pathGeometry.GetOutlinedPathGeometry();
            }

            //var path = new Path() { Data = pathGeometry };

            var pathData = pathGeometry.ToString();

            if (pathData.StartsWith("F1"))
            {
                pathData = pathData.Substring(2);
            }

            
            var file = new System.IO.FileInfo($"{destinationFolderPath}\\{fileName}.svg");
            using (var fs = file.Create())
            {
                var width = pathGeometry.Bounds.Width;
                var height = pathGeometry.Bounds.Height;

                var svgData = new StringBuilder();
                svgData.Append($"<svg width='{width}' height='{height}' xmlns='http://www.w3.org/2000/svg'>");
                svgData.Append($"<path d='{pathData}' transform='translate({-pathGeometry.Bounds.X} {-pathGeometry.Bounds.Y})'/>");
                svgData.Append(@"</svg>");

                var writeData = Encoding.UTF8.GetBytes(svgData.ToString());
                fs.Write(writeData, 0, svgData.Length);
            }

            return file;
        }

        private void File_Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void HowToUse_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new HowToUseWindow();
            helpWindow.Owner = this;
            helpWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            helpWindow.Show();
        }

        private void Theme_Dark_Click(object sender, RoutedEventArgs e)
        {
            var theme = "Light";

            ThemeColors.Background = Brushes.White;
            ThemeColors.Foreground = Brushes.Black;

            if (this.Theme_Dark.IsChecked)
            {
                ThemeColors.Background = Brushes.Black;
                ThemeColors.Foreground = Brushes.White;

                theme = "Dark";
            }

            SetMenuResources(menu, theme);

            this.menu.ItemContainerStyle = (Style)base.FindResource($"MenuItemContainer{theme}Style");

            foreach (var comboBox in this.controls.Children.OfType<System.Windows.Controls.ComboBox>())
            {
                comboBox.Template = (ControlTemplate)this.FindResource($"ComboBox{theme}Template");
                comboBox.ItemContainerStyle = (Style)this.fontSelectionComboBox.FindResource($"ComboBox{theme}ItemContainerStyle");
                comboBox.Foreground = ThemeColors.Foreground;
            }

            foreach (var textBox in this.controls.Children.OfType<System.Windows.Controls.TextBox>())
            {
                textBox.Style = (Style)this.fontSelectionComboBox.FindResource($"TextBox{theme}Style");
            }

            foreach (var textBlock in this.textBlocks.Children.OfType<TextBlock>())
            {
                textBlock.Foreground = ThemeColors.Foreground;
            }

            FontOutLinedCheckBox.Style = (Style)this.FindResource($"CheckBoxStyle{theme}");

            exportAllButton.Style = (Style)this.FindResource($"ButtonStyle{theme}");

            ((TextBlock)weightHelp.Child).Foreground = ThemeColors.Foreground;

            var backgroundColorNum = (byte)Math.Abs(ThemeColors.Background.Color.R - 20);
            var borderBrushColorNum = (byte)Math.Abs(ThemeColors.Background.Color.R - 50);
            characterLoadingProgress.Background = new SolidColorBrush(Color.FromRgb(backgroundColorNum, backgroundColorNum, backgroundColorNum));
            characterLoadingProgress.BorderBrush = new SolidColorBrush(Color.FromRgb(borderBrushColorNum, borderBrushColorNum, borderBrushColorNum));

            foreach (var itemContainer in charGrid.Children.OfType<DataGridCell>())
            {
                ((Path)((Border)itemContainer.Content).Child).Fill = ThemeColors.Foreground;
                itemContainer.Style = (Style)this.FindResource($"DataGridCell{theme}Style");
            }

            this.Background = ThemeColors.Background;

            foreach (Window window in this.OwnedWindows)
            {
                window.Background = ThemeColors.Background;
                window.Foreground = ThemeColors.Foreground;
            }

            foreach (AboutWindow aboutWindow in this.OwnedWindows.OfType<AboutWindow>())
            {
                foreach (Button button in aboutWindow.LicenseInformationArea.Children.OfType<Button>())
                {
                    button.Style = (Style)this.FindResource($"ButtonStyle{theme}");
                }

                aboutWindow.InformationLabel.Foreground = ThemeColors.Foreground;
            }

            this.weightHelpToolTip.Background = ThemeColors.Background;
            this.weightHelpToolTip.Foreground = ThemeColors.Foreground;
        }

        private void SetMenuResources(Menu menu, string theme)
        {
            foreach (MenuItem menuItem in menu.Items)
            {
                if (menuItem.HasItems)
                {
                    SetMenuResources(menuItem, theme);
                }
            }
        }

        private void SetMenuResources(MenuItem menuItem, string theme)
        {
            foreach (MenuItem menuItem2 in menuItem.Items)
            {
                menuItem2.Style = (Style)this.FindResource($"MenuItemContainer{theme}Style");

                if (menuItem2.HasItems)
                {
                    SetMenuResources(menuItem2, theme);
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            aboutWindow.ResizeMode = ResizeMode.NoResize;

            var theme = "Light";

            if (this.Theme_Dark.IsChecked)
            {
                theme = "Dark";
            }

            foreach (Button button in aboutWindow.LicenseInformationArea.Children.OfType<Button>())
            {
                button.Style = (Style)this.FindResource($"ButtonStyle{theme}");
            }

            aboutWindow.InformationLabel.Foreground = ThemeColors.Foreground;

            aboutWindow.Show();
        }

        private void Language_Name_Click(object sender, RoutedEventArgs e)
        {
            var selectedLanguageItem = (MenuItem)sender;

            if (selectedLanguageItem == null)
            {
                return;
            }

            var languageMenuItem = (MenuItem)((MenuItem)sender).Parent;

            foreach (MenuItem menuSubItem in languageMenuItem.Items)
            {
                menuSubItem.IsChecked = false;
            }

            selectedLanguageItem.IsChecked = true;

            var lang = selectedLanguageItem.Language.ToString();
            switch (lang) {
                default:
                case "en":
                    LanguageResource.LoadLanguage("English");
                    break;
                case "bg":
                    LanguageResource.LoadLanguage("Bulgarian");
                    break;
            }

            this.UpdateElementsLanguage();
        }

        private void UpdateElementsLanguage()
        {
            foreach (MenuItem menuItem in menu.Items)
            {
                if (menuItem.Name == "Debug")
                {
                    continue;
                }

                menuItem.Header = LanguageResource.LanguageDictionary[$"Menu.{menuItem.Name}"];

                if (menuItem.HasItems && menuItem.Name != "Lang")
                {
                    foreach (MenuItem menuItemSubItem in menuItem.Items)
                    {
                        menuItemSubItem.Header = LanguageResource.LanguageDictionary[$"Menu.{menuItem.Name}.{menuItemSubItem.Name}"];
                    }
                }
            }

            foreach (var textBlock in textBlocks.Children.OfType<TextBlock>())
            {
                textBlock.Text = LanguageResource.LanguageDictionary[$"MainView.{textBlock.Name}"];
            }

            exportAllButton.Content = LanguageResource.LanguageDictionary["MainView.ExportAllButtonLabel"];

            openTypeWeightHelp.Text = LanguageResource.LanguageDictionary["Tooltip.OpenTypeInfo"] + ":";

            if (this.OwnedWindows.Count > 0)
            {
                var aboutWindows = this.OwnedWindows.OfType<AboutWindow>();
                if (aboutWindows.Any())
                {
                    foreach (AboutWindow window in aboutWindows)
                    {
                        window.Title = LanguageResource.LanguageDictionary["Menu.Help.Help_About"];
                        window.versionText.Text = LanguageResource.LanguageDictionary["About.Version"] + ":";
                        window.developedBy.Text = LanguageResource.LanguageDictionary["About.DevelopedBy"];
                        window.unicodeLicenseText.Text = LanguageResource.LanguageDictionary["About.LicenseInformationAreaText"];

                        foreach (ContentControl element in window.LicenseInformationArea.Children)
                        {
                            element.Content = LanguageResource.LanguageDictionary[$"About.{element.Name}"];
                        }
                    }
                }

                var howToUseWindows = this.OwnedWindows.OfType<HowToUseWindow>();
                if (howToUseWindows.Any())
                {
                    foreach (HowToUseWindow window in howToUseWindows)
                    {
                        window.Title = LanguageResource.LanguageDictionary["Menu.Help.Help_HowToUse"];

                        foreach (TextBlock textBlock in window.TextBlocks.Children)
                        {
                            textBlock.Text = new string(' ', 8) + LanguageResource.LanguageDictionary[$"HowToUse.{textBlock.Name}"];
                        }
                    }
                }
            }
        }

        private void charViewerScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            var element = (System.Windows.Controls.Primitives.ScrollBar)sender;

            if (e.ScrollEventType == System.Windows.Controls.Primitives.ScrollEventType.ThumbTrack)
            {
                element.Value = (int)e.NewValue;
            }

            LoadChars((int)element.Value);
        }

        private void FontOutLinedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            isOutlineCheckBoxChecked = true;

            LoadChars((int)charViewerScrollBar.Value);
        }

        private void FontOutLinedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            isOutlineCheckBoxChecked = false;

            LoadChars((int)charViewerScrollBar.Value);
        }

        private void Debug_Click(object sender, RoutedEventArgs e)
        {
            if (Debug.IsChecked)
            {
                debugTextViewer.Visibility = Visibility.Visible;
            }
            else
            {
                debugTextViewer.Visibility = Visibility.Collapsed;
            }
        }

        private string destinationPath = "";
        BackgroundWorker exportCharactersBackgroundWorker;

        private void exportAllButton_Click(object sender, RoutedEventArgs e)
        {
            var folderSelectDialog = new System.Windows.Forms.FolderBrowserDialog();

            folderSelectDialog.ShowNewFolderButton = true;
            folderSelectDialog.RootFolder = Environment.SpecialFolder.MyDocuments;
            var dialogResult = folderSelectDialog.ShowDialog();

            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                destinationPath = folderSelectDialog.SelectedPath;

                this.exportAllButton.Tag = "exporting";
                this.exportAllButton.Click -= exportAllButton_Click;
                this.exportAllButton.Cursor = Cursors.Wait;

                exportCharactersBackgroundWorker = new BackgroundWorker();

                exportCharactersBackgroundWorker.DoWork += ExportCharacters;

                loadCharactersBackgroundWorker.WorkerSupportsCancellation = false;

                loadCharactersBackgroundWorker.WorkerReportsProgress = true;

                exportCharactersBackgroundWorker.RunWorkerCompleted += (sender, e) =>
                {
                    this.exportAllButton.Tag = null;
                    this.exportAllButton.Click += exportAllButton_Click;
                    this.exportAllButton.Cursor = Cursors.Arrow;
                };

                exportCharactersBackgroundWorker.RunWorkerAsync();
            }

            // to select multiple items try System.Windows.Controls.Primitives.Selector
        }

        private void ExportCharacters(object sendet, DoWorkEventArgs e)
        {
            var charactersToExport = new List<string>(charactersWithGraphic.Keys);

            var destinationFolder = new System.IO.DirectoryInfo(destinationPath);

            if (!destinationFolder.Exists)
            {
                destinationFolder.Create();
            }
            
            foreach (var file in destinationFolder.EnumerateFiles())
            {
                file.Delete();
            }

            var exportedCharacters = 0.0;
            var charactersCount = charactersToExport.Count;

            foreach (var character in charactersToExport)
            {
                var characterCodePoint = string.Join("", Encoding.UTF32.GetBytes(character).Select(b => b.ToString("X2")).Reverse());
                characterCodePoint = int.Parse(characterCodePoint, NumberStyles.HexNumber).ToString("X4");

                string fileName;
                if (characterNameData.TryGetValue(characterCodePoint, out string characterName))
                {
                    fileName = characterName.ToLower();
                }
                else if (int.Parse(characterCodePoint, NumberStyles.HexNumber) <= 0xFFFF)
                {
                    var characterCategory = char.GetUnicodeCategory(char.Parse(character));
                    if (characterCategory == UnicodeCategory.PrivateUse)
                    {
                        fileName = "private use character " + characterCodePoint;
                    }
                    else if (char.IsLowSurrogate(char.Parse(character)))
                    {
                        fileName = "low surrogate character " + characterCodePoint;
                    }
                    else if (char.IsHighSurrogate(char.Parse(character)))
                    {
                        fileName = "high surrogate character " + characterCodePoint;
                    }
                    else
                    {
                        fileName = $"character " + characterCodePoint;
                    }
                }
                else
                {
                    fileName = $"character " + characterCodePoint;
                }

                _ = WriteSvgFile(character, destinationFolder.FullName, fileName);

                exportedCharacters++;

                //loadCharactersBackgroundWorker.ReportProgress((int)(exportedCharacters / charactersCount * 100));
            }
        }

        public async void readfile(string path)
        {
            var log = new StringBuilder();
            log.AppendLine("Entered");

            var file = new System.IO.FileInfo(path);
            using (var reader = file.OpenText())
            {
                var allowedBeginnings = "0123456789ABCDEF".ToCharArray();

                var line = "";

                var characters = new Dictionary<string, string>();

                while (!reader.EndOfStream)
                {
                    line = await reader.ReadLineAsync();
                    if (line.Length == 0) continue;

                    if (allowedBeginnings.Contains(line.First()))
                    {
                        var parts = line.Split(';').Select(s => s.Trim()).ToArray();

                        var range = new int[2];
                        if (parts[0].Length > 6)
                        {
                            range = parts[0].Split("..").Select(r => int.Parse(r, NumberStyles.HexNumber)).ToArray();
                        }

                        if (range[1] - range[0] > 0)
                        {
                            for (int i = range[0]; i <= range[1]; i++)
                            {
                                characters.Add(i.ToString("X"), parts[1].Replace("*", i.ToString("X")));
                            }

                            continue;
                        }
                        
                        characters.Add(parts[0], parts[1]);
                    }
                }

                characterNameData = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
                await Task.Run(() =>
                {
                    foreach (var character in characters)
                    {
                        characterNameData.TryAdd(character.Key, character.Value.ToLower());
                    }
                });
            }

            log.AppendLine("Finished");
            debugText.Text = log.ToString();
        }
    }
}