using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace VectorCharMap
{
    public class ThemeColors
    {
        //this.ThemeColors["Background"] = Colors.White;
        //this.ThemeColors["HoverBackground"] = Color.FromRgb(223, 223, 223);
        //this.ThemeColors["Foreground"] = Colors.Black;
        //this.ThemeColors["MenuForeground"] = Color.FromRgb(0x21, 0x21, 0x21);
        public static SolidColorBrush Background { get; set; } =
            new SolidColorBrush(Colors.White);

        public static SolidColorBrush Foreground { get; set; } =
            new SolidColorBrush(Colors.Black);
        public static Brush MenuForeground { get; internal set; } =
            new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x21));
        public static SolidColorBrush HoverBackground { get; internal set; } =
            new SolidColorBrush(Color.FromRgb(0xDF, 0xDF, 0xDF));

        public static SolidColorBrush DataGridCellBackground { get; set; } = Brushes.White;
    }
}
