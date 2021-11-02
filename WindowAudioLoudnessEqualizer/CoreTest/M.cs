namespace CoreTest
{
    /// <summary>
    /// Console Message
    /// </summary>
    public static class M
    {
        /// <summary>
        /// N:normal, ER:error
        /// </summary>
        public enum Kind { N = 0, ER = 1 };
        private static string Skind(Kind k)
        {
            string s = "";
            switch (k)
            {
                case Kind.ER: s = "ER"; break;
                case Kind.N:
                default: s = "N"; break;
            }
            return s;
        }
        public static void D(object o) => System.Diagnostics.Debug.WriteLine(o);
        public static void D(int id) => D($"{id}(N)");
        public static void D(int id, object o) => D($"{id}(N): {o}");
        public static void D(int id, Kind kind) => D($"{id}({Skind(kind)})");
        public static void D(int id, Kind kind, object o) => D($"{id}({Skind(kind)}): {o}");
    }
}
