using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.CoreAudio
{
    /// <summary>
    /// Data class of audio device
    /// </summary>
    public class DeviceData
    {
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public DeviceState State { get; set; }
        public List<SessionData> Sessions { get; set; }
    }//End class DeviceData
}//End namespace Wale.CoreAudio