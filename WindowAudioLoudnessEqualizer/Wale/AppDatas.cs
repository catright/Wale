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
        /// Height of config set
        /// </summary>
        public static double ConfigSetHeight { get => 265; }
        /// <summary>
        /// Long height of config set when advanced view is selected
        /// </summary>
        public static double ConfigSetLongHeight { get => 415; }

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
