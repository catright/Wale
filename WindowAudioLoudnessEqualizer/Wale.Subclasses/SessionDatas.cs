using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.CoreAudio
{
    public class SessionDatas : List<SessionData>
    {
        public SessionDatas() { this.Clear(); }
        public SessionData GetSession(uint id)
        {
            try { return this.Find(sc => sc.PID == id); }
            catch (ArgumentNullException)
            {
                JDPack.FileLog.Log($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JDPack.FileLog.Log($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JDPack.FileLog.Log($"Error(GetSession): {e.ToString()}");
            }
            return null;
        }
        public double GetRelative(uint id)
        {
            try { return GetSession(id).Relative; }
            catch (NullReferenceException)
            {
                JDPack.FileLog.Log($"Error(GetRelative): NullReferenceException");
            }
            catch (Exception e)
            {
                JDPack.FileLog.Log($"Error(GetRelative): {e.ToString()}");
            }
            return 0.0;
        }

    }
}
