using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.WPF
{
    /// <summary>
    /// Colors for app visualization
    /// </summary>
    public static class ColorSet
    {
        public static System.Windows.Media.Color MainColor = System.Windows.Media.Colors.SteelBlue;
        public static System.Windows.Media.Color VolumeColor = System.Windows.Media.Colors.SteelBlue;//.FromRgb(83, 139, 167);
        public static System.Windows.Media.Color PeakColor = System.Windows.Media.Colors.PaleVioletRed;//.FromRgb(144, 175, 197);
        public static System.Windows.Media.Color AverageColor = System.Windows.Media.Colors.PaleGoldenrod;//.FromRgb(154, 161, 146);
        public static System.Windows.Media.Color TargetColor = System.Windows.Media.Colors.Goldenrod;//.FromRgb(182, 150, 38);

        public static System.Windows.Media.Color ForeColor = System.Windows.Media.Colors.LightGray;
        public static System.Windows.Media.Color ForeColorAlt = System.Windows.Media.Colors.Gray;

        public static System.Windows.Media.Color BackColor = System.Windows.Media.Color.FromRgb(64, 64, 64);
        public static System.Windows.Media.Color BackColorAlt = System.Windows.Media.Colors.DimGray;


        public static System.Windows.Media.Brush MainColorBrush { get => new System.Windows.Media.SolidColorBrush(MainColor); }
        public static System.Windows.Media.Brush VolumeColorBrush { get => new System.Windows.Media.SolidColorBrush(VolumeColor); }
        public static System.Windows.Media.Brush PeakColorBrush { get => new System.Windows.Media.SolidColorBrush(PeakColor); }
        public static System.Windows.Media.Brush AverageColorBrush { get => new System.Windows.Media.SolidColorBrush(AverageColor); }
        public static System.Windows.Media.Brush TargetColorBrush { get => new System.Windows.Media.SolidColorBrush(TargetColor); }

        public static System.Windows.Media.Brush ForeColorBrush { get => new System.Windows.Media.SolidColorBrush(ForeColor); }
        public static System.Windows.Media.Brush ForeColorAltBrush { get => new System.Windows.Media.SolidColorBrush(ForeColorAlt); }

        public static System.Windows.Media.Brush BackColorBrush { get => new System.Windows.Media.SolidColorBrush(BackColor); }
        public static System.Windows.Media.Brush BackColorAltBrush { get => new System.Windows.Media.SolidColorBrush(BackColorAlt); }
    }
}
