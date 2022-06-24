using System;

namespace Wale.WPF
{
    public static class AppVersion
    {
        private static readonly Version versionObject = typeof(App).Assembly.GetName().Version;
        /// <summary>
        /// App Release Number. Top level version
        /// </summary>
        public static int ARN = 0;
        public static int Major = versionObject.Major;
        public static int Minor = versionObject.Minor;
        /// <summary>
        /// Version for UI
        /// </summary>
        public static string Version = $"{ARN}.{Major}.{Minor}";



        //private static int SysBuild = versionObject.Build;
        //private static int SysRevision = versionObject.Revision;
        //private static DateTime buildDate = new DateTime(2000, 1, 1).AddDays(SysBuild).AddSeconds(SysRevision * 2);
        //private static TimeSpan critDate = buildDate.Subtract(new DateTime(2017, 8, 20));
        //public static int Build = (int)critDate.TotalDays;
        //public static int Revision = (int)critDate.Subtract(new TimeSpan(Build, 0, 0, 0)).TotalSeconds / 10;
        private static readonly int Build = versionObject.Build;
        private static readonly int Revision = versionObject.Revision;

        /// <summary>
        /// Build number
        /// </summary>
        public static string SubVersion = $"{Build}.{Revision}";
        /*public static string Option
        {
            get
            {
                // 3>=release, 2=beta, 1=alpha, 0=pre-alpha
                string opt = string.Empty;
                int rev = versionObject.Revision;
                if (rev == 0) opt = "pre-alpha";
                else if (rev == 1) opt = "alpha";
                else if (rev == 2) opt = "beta";
                else opt = "";
                return opt;
            }
        }*/



        public static string FullVersion = $"{Version}.{SubVersion}";
    }

    public static class AppUpdateCheck
    {
        public static Tuple<bool, string> Check()
        {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.Load("R:\\rss");

            System.Xml.XmlNode latest = null;
            DateTime latestDate = new DateTime(2017, 8, 20);
            foreach (System.Xml.XmlNode item in doc.SelectNodes("/rss/channel/item"))
            {
                if (item.SelectSingleNode("title").InnerText.EndsWith("msi"))
                {
                    // Console.WriteLine(item.SelectSingleNode("pubDate").InnerText.Replace(" UT", "Z"));
                    DateTime.TryParse(
                        item.SelectSingleNode("pubDate").InnerText.Replace(" UT", "Z"),
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                        out DateTime newDate);
                    //Console.WriteLine($"{latestDate} | {newDate} | {newDate.CompareTo(latestDate)}");
                    if (newDate.CompareTo(latestDate) > 0) { latestDate = newDate; latest = item; }
                }
            }

            string title = latest.SelectSingleNode("title").InnerText;
            string link = latest.SelectSingleNode("link").InnerText;
            //Console.WriteLine($"{title} {link}");
            string[] version = title.Substring(0, title.LastIndexOf('.')).Split('_')[1].Split('.');// Console.WriteLine($"{version[0]}.{version[1]}.{version[2]}");

            bool UpdateReq = Convert.ToInt32(version[0]) > AppVersion.ARN || Convert.ToInt32(version[1]) > AppVersion.Major || Convert.ToInt32(version[2]) > AppVersion.Minor;
            return new Tuple<bool, string>(UpdateReq, string.IsNullOrEmpty(link) ? null : link);
        }
    }
}
