using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Configs
{
    public static class Visual
    {
        /// <summary>
        /// Default width of main visual window
        /// </summary>
        public static double MainWindowWidth => 480;
        /// <summary>
        /// Default height of main visual window
        /// </summary>
        public static double MainWindowHeightDefault => 285;

        /// <summary>
        /// Base height of main window includes title bar and tab selector
        /// </summary>
        public static double MainWindowBaseHeight => 60;
        /// <summary>
        /// Title bar height of Wale windows
        /// </summary>
        public static double TitleBarHeight => 35;

        // add 22px to conf heights when another lineitem is added
        /// <summary>
        /// Height of config set
        /// </summary>
        public static double ConfigSetHeight => 311;
        /// <summary>
        /// Long height of config set when advanced view is selected
        /// </summary>
        public static double ConfigSetLongHeight => 439;

        /// <summary>
        /// Default height of windows except main window
        /// </summary>
        public static double SubWindowHeightDefault => 360;

        /// <summary>
        /// Visual block of each session when normal view
        /// </summary>
        public static double SessionBlockHeightNormal => 27;
        /// <summary>
        /// Visual block of each session when detailed view
        /// </summary>
        public static double SessionBlockHeightDetail => 42;
    }
}
