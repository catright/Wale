using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.CoreAudio
{
    /// <summary>
    /// List object of SessionData.
    /// </summary>
    public class SessionDataList : List<SessionData>
    {
        public SessionDataList() { this.Clear(); }
        /// <summary>
        /// Find session by its process id.
        /// <para>Log($"Error(GetSession): ArgumentNullException") when ArgumentNullException. 
        /// Log($"Error(GetSession): NullReferenceException") when NullReferenceException. 
        /// Log($"Error(GetSession): {(Exception)e.ToString()}") when Exception</para>
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns>SessionData when find SessionData successfully or null.</returns>
        public SessionData GetSession(uint pid)
        {
            try { return this.Find(sc => sc.PID == pid); }
            catch (ArgumentNullException)
            {
                JPack.FileLog.Log($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JPack.FileLog.Log($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.FileLog.Log($"Error(GetSession): {e.ToString()}");
            }
            return null;
        }
        /// <summary>
        /// Get Relative value with <code>GetSession</code> by its process id.
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns>Relative value when find Relative value successfully or 0.0.</returns>
        public double GetRelative(uint pid)
        {
            try { return GetSession(pid).Relative; }
            catch (NullReferenceException)
            {
                JPack.FileLog.Log($"Error(GetRelative): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.FileLog.Log($"Error(GetRelative): {e.ToString()}");
            }
            return 0.0;
        }

    }
}
