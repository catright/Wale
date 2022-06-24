namespace Wale.CoreAudio
{
    /// <summary>
    /// Data class of audio device
    /// </summary>
    public class MapDataDevice
    {
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public DeviceState State { get; set; }
        public MapDataSession[] Sessions { get; set; }
    }//End class DeviceData
}//End namespace Wale.CoreAudio