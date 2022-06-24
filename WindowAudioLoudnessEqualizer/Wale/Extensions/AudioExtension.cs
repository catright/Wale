using CSCore.CoreAudioAPI;
using System;

namespace Wale.Extensions
{
    public static class AudioExtension
    {
        // CoreAudioAPI
        /// <summary>
        /// <c>state</c> = AudioSessionStateActive
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsActive(this AudioSessionState state) => state == AudioSessionState.AudioSessionStateActive;
        /// <inheritdoc cref= "IsActive(AudioSessionState)" />
        public static bool IsActive(this CoreAudio.SessionState state) => state == CoreAudio.SessionState.AudioSessionStateActive;
        //public static bool IsActive(this NAudio.CoreAudioApi.Interfaces.AudioSessionState state) => state == NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateActive;
        /// <summary>
        /// <c>state</c> != AudioSessionStateActive
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsNotActive(this AudioSessionState state) => state != AudioSessionState.AudioSessionStateActive;
        /// <inheritdoc cref= "IsNotActive(AudioSessionState)" />
        public static bool IsNotActive(this CoreAudio.SessionState state) => state != CoreAudio.SessionState.AudioSessionStateActive;
        //public static bool IsNotActive(this NAudio.CoreAudioApi.Interfaces.AudioSessionState state) => state != NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateActive;
        /// <summary>
        /// <c>state</c> = AudioSessionStateInactive
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsInactive(this AudioSessionState state) => state == AudioSessionState.AudioSessionStateInactive;
        /// <inheritdoc cref= "IsInactive(AudioSessionState)" />
        public static bool IsInactive(this CoreAudio.SessionState state) => state == CoreAudio.SessionState.AudioSessionStateInactive;
        //public static bool IsInactive(this NAudio.CoreAudioApi.Interfaces.AudioSessionState state) => state == NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateInactive;
        /// <summary>
        /// <c>state</c> = AudioSessionStateExpired
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsExpired(this AudioSessionState state) => state == AudioSessionState.AudioSessionStateExpired;
        /// <inheritdoc cref= "IsExpired(AudioSessionState)" />
        public static bool IsExpired(this CoreAudio.SessionState state) => state == CoreAudio.SessionState.AudioSessionStateExpired;
        //public static bool IsExpired(this NAudio.CoreAudioApi.Interfaces.AudioSessionState state) => state == NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateExpired;

        public static CoreAudio.DeviceState GetDeviceState(this DeviceState state)
        {
            switch (state)
            {
                case DeviceState.Active: return CoreAudio.DeviceState.Active;
                case DeviceState.Disabled: return CoreAudio.DeviceState.Disabled;
                case DeviceState.NotPresent: return CoreAudio.DeviceState.NotPresent;
                case DeviceState.UnPlugged: return CoreAudio.DeviceState.UnPlugged;
                case DeviceState.All: return CoreAudio.DeviceState.All;
            }
            throw new ArgumentException("AudioExtension: GetDeviceState: invalid state");
        }
        public static CoreAudio.SessionState GetSessionState(this AudioSessionState state)
        {
            switch (state)
            {
                case AudioSessionState.AudioSessionStateActive: return CoreAudio.SessionState.AudioSessionStateActive;
                case AudioSessionState.AudioSessionStateInactive: return CoreAudio.SessionState.AudioSessionStateInactive;
                case AudioSessionState.AudioSessionStateExpired: return CoreAudio.SessionState.AudioSessionStateExpired;
            }
            throw new ArgumentException("AudioExtension: GetSessionState: invalid state");
        }

        // ETC
        /// <summary>
        /// Unit conversion. Linear->deciBel
        /// </summary>
        /// <param name="value">audio level, such as volume or peak</param>
        /// <returns></returns>
        public static double TodB(this double value) => 20 * Math.Log10(value);
        /// <summary>
        /// Unit conversion. deciBel->Linear
        /// </summary>
        /// <param name="value">audio level, such as volume or peak</param>
        /// <returns></returns>
        public static double ToLinear(this double value) => Math.Pow(10, value / 20);

        /// <summary>
        /// Unit conversion.
        /// Get output level for UI. 0=Linear, 1=dB.
        /// Output would be rounded to 3digit when 0, 1digit when 1.
        /// Output would be -∞ when it's below than -99.9 and unit is 1.
        /// </summary>
        /// <param name="input">audio level, such as volume or peak</param>
        /// <param name="unit">0=Linear(Windows default), 1=dB, else=Linear</param>
        /// <returns></returns>
        public static double Level(this double input, int unit = 0)
        {
            double output;
            switch (unit)
            {
                case 1:
                    output = Math.Round(input.TodB(), 1);
                    if (output < -99.9) { output = double.NegativeInfinity; }
                    else if (output > 99.9) { output = double.PositiveInfinity; }
                    break;
                case 0:
                default:
                    output = Math.Round(input, 3);
                    break;
            }
            return output;
        }
        /// <summary>
        /// Unit conversion.
        /// Get output level difference (subtraction) for UI. 0=Linear, 1=dB.
        /// Output would be rounded to 3digit when 0, 1digit when 1.
        /// Output would be -∞ when it's below than -99.9 and unit is 1.
        /// </summary>
        /// <param name="input1">audio level, such as volume or peak</param>
        /// <param name="input2">audio level, such as volume or peak</param>
        /// <param name="unit">0=Linear(Windows default), 1=dB, else=Linear</param>
        /// <returns></returns>
        public static double LevelDiff(this double input1, double input2, int unit = 0)
        {
            double output;
            switch (unit)
            {
                case 1:
                    output = Math.Round(20 * (Math.Log10(input1) - Math.Log10(input2)), 1);
                    if (output < -99.9) { output = double.NegativeInfinity; }
                    else if (output > 99.9) { output = double.PositiveInfinity; }
                    break;
                case 0:
                default:
                    output = Math.Round(input1 - input2, 3);
                    break;
            }
            return output;
        }
        /// <summary>
        /// Unit conversion.
        /// Get output level difference (multiplication) for UI. 0=Linear, 1=dB.
        /// Output would be rounded to 3digit when 0, 1digit when 1.
        /// Output would be -∞ when it's below than -99.9 and unit is 1.
        /// </summary>
        /// <param name="input">audio level, such as volume or peak</param>
        /// <param name="unit">0=Linear(Windows default), 1=dB, else=Linear</param>
        /// <returns></returns>
        public static double LevelMult(this double input, int unit = 0)
        {
            double output;
            switch (unit)
            {
                case 1:
                    output = Math.Round((1 + input).TodB(), 1);
                    if (output < -99.9) { output = double.NegativeInfinity; }
                    else if (output > 99.9) { output = double.PositiveInfinity; }
                    break;
                case 0:
                default:
                    output = Math.Round(1 + input, 3);
                    break;
            }
            return output;
        }
        /// <summary>
        /// Unit conversion.
        /// Get output level of Relative factor for UI. 0=Linear, 1=dB.
        /// Output would be rounded to 3digit when 0, 1digit when 1.
        /// Output would be ∞ when absolute value of output is above than 99.9 and unit is 1.
        /// </summary>
        /// <param name="input">audio level, such as volume or peak</param>
        /// <param name="unit">0=Linear(Windows default), 1=dB, else=Linear</param>
        /// <returns></returns>
        public static double RelLv(this double input, int unit = 0)
        {
            double output;
            switch (unit)
            {
                case 1:
                    output = Math.Round(Math.Pow(Configs.Audio.RelativeBase, input).TodB(), 1);
                    if (output < -99.9) { output = double.NegativeInfinity; }
                    else if (output > 99.9) { output = double.PositiveInfinity; }
                    break;
                case 0:
                default:
                    output = Math.Round(Math.Pow(Configs.Audio.RelativeBase, input), 3);
                    break;
            }
            return output;
        }

    }
}
