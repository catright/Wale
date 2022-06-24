using System.Collections.Generic;

namespace Wale
{
    public static class E
    {
        public static readonly Dictionary<uint, string> _E = new Dictionary<uint, string>
        {
            { 0x88890004, "C:can not access coreaudioapi" },
            { 0x80070490, "C:IAudioSessionControl::UnregisterAudioSessionNotification> Element not found" },
            { 0x80004002, "N:IMMDevice: E_NOINTERFACE" }
        };
        public static bool IsKnown(this int HResult) => _E.ContainsKey((uint)HResult);
        public static bool IsUnknown(this int HResult) => !IsKnown(HResult);
        public static bool Get(this int HResult, out string Message)
        {
            bool res = IsUnknown(HResult);
            Message = res ? string.Empty : _E[(uint)HResult];
            return res;
        }
    }
}
