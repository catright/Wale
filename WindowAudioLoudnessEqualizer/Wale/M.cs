using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wale
{
    public static class M
    {
        /// <summary>
        /// Message(<paramref name="o"/>) will be suffixed with " *[<paramref name="caller"/>]" when <paramref name="caller"/> is not <c>null</c><br/>
        /// Message(<paramref name="o"/>) will be prefixed with "hh:mm> " when <paramref name="raw"/> is <c>false</c>
        /// </summary>
        /// <param name="o">message to log</param>
        /// <param name="caller"><see cref="CallerMemberNameAttribute"/></param>
        /// <param name="raw"></param>
        /// <returns></returns>
        public static string Message(object o, string caller = null, bool raw = false)
        {
            //if (o is System.Text.StringBuilder b) o = b.ToString();
            DateTime t = DateTime.Now.ToLocalTime();
            if (caller != null)
            {
                if (raw) return $"{o} *C[{caller}]";
                else return $"{t.Hour:d2}:{t.Minute:d2}> {o} *C[{caller}]";
            }
            else
            {
                if (raw) return $"{o}";
                else return $"{t.Hour:d2}:{t.Minute:d2}> {o}";
            }
        }

        /// <summary>
        /// <see cref="Debug.WriteLine(object)"/> with <see cref="Message(object, string, bool)"/>
        /// </summary>
        /// <param name="o">message to log</param>
        /// <param name="caller"><see cref="CallerMemberNameAttribute"/></param>
        public static void D(object o, [CallerMemberName] string caller = null) => Debug.WriteLine(Message(o, caller));

        /// <summary>
        /// <see cref="JPack.FileLog.Log(string, bool)"/>
        /// <para>Also <see cref="Debug.Write(object)"/> when <paramref name="show"/> is <c>true</c><br/>
        /// <see cref="Debug.WriteLine(object)"/> will be used instead of <see cref="Debug.Write(object)"/> when <paramref name="newLine"/> is <c>true</c></para>
        /// <para>Message(<paramref name="o"/>) will be dealt with <see cref="Message(object, string, bool)"/></para>
        /// </summary>
        /// <param name="o">message to log</param>
        /// <param name="show">Select whether write to debug console or not</param>
        /// <param name="newLine">flag for making newline after the end of the <paramref name="o"/></param>
        /// <param name="caller"><see cref="CallerMemberNameAttribute"/></param>
        public static void F(object o, bool show = false, bool newLine = true, bool verbose = true, [CallerMemberName] string caller = null)
        {
            if (!CanFlog) return;
            try
            {
                JPack.Log.Write(Message(o, verbose ? caller : null, true), newLine);
                //AppendText(LogScroll, content);
#if DEBUG
                if (show)
                {
                    if (newLine) Debug.WriteLine(Message(o, verbose ? caller : null));
                    else Debug.Write(Message(o, verbose ? caller : null));
                }
#endif
            }
            catch (Exception e)
            {
#if DEBUG
                Debug.WriteLine($"{e} {caller}");
#endif
            }
        }
        private static bool _CanFlog = false;
        public static bool CanFlog => _CanFlog;
        public static void InitF(string path, string file, int days)
        {
            JPack.Log.SetWorkDirectory(path);
            JPack.Log.Open(file);
            JPack.Log.Erase(days);
            _CanFlog = true;
        }
    }
}
