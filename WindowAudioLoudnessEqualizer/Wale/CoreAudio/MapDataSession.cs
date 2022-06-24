using CSCore.CoreAudioAPI;
using Wale.Extensions;

namespace Wale.CoreAudio
{
    /// <summary>
    /// Data container for audio session
    /// </summary>
    public class MapDataSession
    {
        public MapDataSession(AudioSessionControl2 asc2)
        {
            Name = Namer.Make(asc2);
            ProcessID = asc2.ProcessID;
            SessionIdentifier = asc2.SessionIdentifier;
            IsSystemSound = asc2.IsSystemSoundSession;
            State = asc2.SessionState.GetSessionState();
        }

        public string Name { get; private set; }
        public int ProcessID { get; private set; }
        public string SessionIdentifier { get; private set; }
        public bool IsSystemSound { get; private set; }
        public SessionState State { get; private set; }


    }
}