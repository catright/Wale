using System;
using System.Collections.Generic;

namespace Wale.CoreAudio
{
    /// <summary>
    /// List object of Sessions. List&lt;Session&gt;
    /// </summary>
    public class SessionList : List<Session>, IDisposable
    {
        public object Locker { get; } = new object();
        public SessionList() { }
        public void DisposedCheck() => _ = RemoveAll(s => s == null || s.State == SessionState.AudioSessionStateExpired);
        public void DisposeAll()
        {
            ForEach(s => s.Dispose());
            base.Clear();
        }
        public void Dispose() => DisposeAll();
        public new void Clear() => DisposeAll();

        public bool Exists(int pid) => base.Exists(s => s.ProcessID == pid);
        public bool Exists(Guid id, bool grp = false) => grp ? base.Exists(s => s.GroupParam == id) : base.Exists(s => s.ID == id);


        /// <summary>
        /// Find session with process id.
        /// <list type="bullet">
        /// <item>Log($"Error(GetSession): ArgumentNullException") when ArgumentNullException. </item>
        /// <item>Log($"Error(GetSession): NullReferenceException") when NullReferenceException. </item>
        /// <item>Log($"Error(GetSession): {(Exception)e.ToString()}") when Exception</item>
        /// </list>
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns><see cref="Session"/> when found or null.</returns>
        public Session GetSession(int pid)
        {
            try { return Find(sc => sc.ProcessID == pid); }
            catch (ArgumentNullException)
            {
                JPack.Log.Write($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JPack.Log.Write($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.Log.Write($"Error(GetSession): {e}");
            }
            return null;
        }
        /// <summary>
        /// Find session with grouping param.
        /// <list type="bullet">
        /// <item>Log($"Error(GetSession): ArgumentNullException") when ArgumentNullException. </item>
        /// <item>Log($"Error(GetSession): NullReferenceException") when NullReferenceException. </item>
        /// <item>Log($"Error(GetSession): {(Exception)e.ToString()}") when Exception</item>
        /// </list>
        /// </summary>
        /// <param name="grparam">Grouping Param</param>
        /// <returns><see cref="Session"/> when found or null.</returns>
        public Session GetSession(Guid id, bool grp = false)
        {
            try { return grp ? Find(s => s.GroupParam == id) : Find(s => s.ID == id); }
            catch (ArgumentNullException)
            {
                JPack.Log.Write($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JPack.Log.Write($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.Log.Write($"Error(GetSession): {e}");
            }
            return null;
        }

        /// <summary>
        /// Get Relative value via <see cref="GetSession(int)"/> with process id.
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns>Relative value if session is found or 0.0</returns>
        public double GetRelative(int pid)
        {
            try { return GetSession(pid).Relative; }
            catch (NullReferenceException)
            {
                JPack.Log.Write($"Error(GetRelative): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.Log.Write($"Error(GetRelative): {e}");
            }
            return 0.0;
        }
        /// <summary>
        /// Get Relative value vis <see cref="GetSession(Guid)"/> with grouping param.
        /// </summary>
        /// <param name="grparam">Grouping Param</param>
        /// <returns>Relative value if session is found or 0.0</returns>
        public double GetRelative(Guid id, bool grp = false)
        {
            try { return GetSession(id, grp).Relative; }
            catch (NullReferenceException)
            {
                JPack.Log.Write($"Error(GetRelative): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.Log.Write($"Error(GetRelative): {e}");
            }
            return 0.0;
        }


        /// <summary>
        /// Find sessions with SessionIdentifier.
        /// <list type="bullet">
        /// <item>Log($"Error(GetSession): ArgumentNullException") when ArgumentNullException. </item>
        /// <item>Log($"Error(GetSession): NullReferenceException") when NullReferenceException. </item>
        /// <item>Log($"Error(GetSession): {(Exception)e.ToString()}") when Exception</item>
        /// </list>
        /// </summary>
        /// <param name="sid">SessionIdentifier</param>
        /// <returns><see cref="List{T}"/> of <see cref="Session"/> when found or null.</returns>
        public List<Session> GetSessionList(string sid)
        {
            try { return FindAll(sc => sc.SessionIdentifier == sid); }
            catch (ArgumentNullException)
            {
                JPack.Log.Write($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JPack.Log.Write($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JPack.Log.Write($"Error(GetSession): {e}");
            }
            return null;
        }

    }
}
