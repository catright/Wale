using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.CoreAudio
{
    public class DeviceData
    {
        public string DeviceId;
        public string Name;
        public DeviceState State;
        public List<SessionData> Sessions;
    }//End class DeviceData
}//End namespace Wale.CoreAudio