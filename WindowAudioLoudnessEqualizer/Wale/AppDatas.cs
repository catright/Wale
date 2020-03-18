using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale
{
    /// <summary>
    /// Data container for Wale
    /// </summary>
    public static class AppDatas
    {
        /// <summary>
        /// Default width of main visual window
        /// </summary>
        public static double MainWindowWidth { get => 480; }
        /// <summary>
        /// Default height of main visual window
        /// </summary>
        public static double MainWindowHeightDefault { get => 285; }

        /// <summary>
        /// Base height of main window includes title bar and tab selector
        /// </summary>
        public static double MainWindowBaseHeight { get => 60; }
        /// <summary>
        /// Title bar height of Wale windows
        /// </summary>
        public static double TitleBarHeight { get => 35; }

        /// <summary>
        /// Final height of main window when selected config tab
        /// </summary>
        public static double MainWindowConfigHeight { get => 305; }
        /// <summary>
        /// Final long height of main window when selected config tab
        /// </summary>
        public static double MainWindowConfigLongHeight { get => 475; }

        /// <summary>
        /// Default height of windows except main window
        /// </summary>
        public static double SubWindowHeightDefault { get => 360; }

        /// <summary>
        /// Visual block of each session when normal view
        /// </summary>
        public static double SessionBlockHeightNormal { get => 27; }
        /// <summary>
        /// Visual block of each session when detailed view
        /// </summary>
        public static double SessionBlockHeightDetail { get => 42; }
    }
}
