using System.Windows.Media;

namespace Wale.WPF
{
    /// <summary>
    /// Colors for app visualization
    /// </summary>
    public static class ColorSet
    {
        public static Color MainColor = Colors.SteelBlue;
        public static Color VolumeColor = Colors.SteelBlue;//.FromRgb(83, 139, 167);
        public static Color PeakColor = Colors.PaleVioletRed;//.FromRgb(144, 175, 197);
        public static Color AverageColor = Colors.PaleGoldenrod;//.FromRgb(154, 161, 146);
        public static Color TargetColor = Colors.Goldenrod;//.FromRgb(182, 150, 38);
        public static Color LimitColor = Colors.OrangeRed;

        public static Color ForeColor = Colors.LightGray;
        public static Color ForeColorAlt = Colors.Gray;

        public static Color BackColor = Color.FromRgb(64, 64, 64);
        public static Color BackColorAlt = Colors.DimGray;


        public static Brush MainColorBrush => new SolidColorBrush(MainColor);
        public static Brush VolumeColorBrush => new SolidColorBrush(VolumeColor);
        public static Brush PeakColorBrush => new SolidColorBrush(PeakColor);
        public static Brush AverageColorBrush => new SolidColorBrush(AverageColor);
        public static Brush TargetColorBrush => new SolidColorBrush(TargetColor);
        public static Brush LimitColorBrush => new SolidColorBrush(LimitColor);

        public static Brush ForeColorBrush => new SolidColorBrush(ForeColor);
        public static Brush ForeColorAltBrush => new SolidColorBrush(ForeColorAlt);

        public static Brush BackColorBrush => new SolidColorBrush(BackColor);
        public static Brush BackColorAltBrush => new SolidColorBrush(BackColorAlt);
    }
}
