using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Localization
{
    [Serializable]
    public class WaleEnglish : WaleLocalization
    {
        public WaleEnglish()
        {
            Locale = "English";

            View = "View(F2)";
            Target = "Target";
            Device = "Device";
            Master = "Master";
            AlwaysTop = "AlwaysTop(F7)";
            StayOn = "StayOn(F8)";
            AdvancedView = "AdvancedView(F9)";

            Config = "Config(F3)";
            Configuration = "Configuration";
            Audio = "Audio";
            UIUpdate = "UI Update [ms]";
            AutoControl = "Auto Control [ms]";
            GCInterval = "GC Interval [ms]";
            TargetLevel = "Target Level";
            UpRate = "Up Rate";
            Kurtosis = "Kurtosis";
            Function = "Function";
            Original = "Original";
            Graph = "Graph";
            AverageTime = "Average Time [ms]";
            MinPeakLevel = "Min Peak Level";
            AverageEnabled = "Average Enabled";
            AutoControlEnabled = "Auto Control Enabled";
            Window = "Window";
            RunAtWindowsStartup = "Run at Windows Startup";
            AdvancedViewConf = "Advanced View(F9)";
            DetailedLog = "Detailed Log";
            Priority = "Priority";
            Normal = "Normal";
            AboveNormal = "Above Normal";
            High = "High";
            Save = "Save";
            SaveAndClose = "Save and Close";
            ReturnToDefault = "Return to Default";
            Cancel = "Cancel";

            Log = "Log(F4)";

            DeviceMap = "Device Map";
            Update = "Update";

            OpenLog = "Open Log";

            Help = "Help";
            HelpMsg = @"Latest usage and full description are always firstly updated on Github Wiki(https://github.com/catright/Wale/wiki)
Last Update 0.5.8


Usage

Wale tries to adjust all sound generating processes' peak level to Target-Yellow bar- level. You can change Target to your preferred level.

However, Wale uses windows volume system for now, which means the app only can control volume between 0.0~1.0. Hence, if you set Target to near 1.0, then Wale could not control volume, because all processes' peak levels are always less than or equal to Target. So, you should lower Target a little. Default Target level is 0.15(-16.5dB).

    Caution! Too low Target may cause sudden hugely loud sound. Low Target means quieter output sound from your windows system. Then, you may need to increase the volume of your hardware such as speaker to hear the sound. In this situation, uncontrolled process would make very loud sound caused by some trouble of sound system include unintentional stop of Wale.

    Click blue checkbox on the session to uncheck it if you want to exclude some session from auto control. This exclusion is not saved when Wale is closed.
    Click process name or white checkbox on the session to mute it. The mute state is saved in the process itself so kept when Wale is closed.
    You can use your mouse wheel to adjust Relative-White bar- volume for each session. Relative value is not saved when Wale is closed.";
            CopiedToClip = "Address copied to clipboard";
            LinkTooltip = "Ctrl+left click to open in browser\nRight click to copy to clipboard";

            License = "License";
            Localization = "Localization";
            LocalizationMsg = "";

            Restart = "Restart";
            Close = "Close";

            Exitxml = "E_xit";
            Exit = "Exit";
            AreYouSureToTerminateWaleCompletely = "Are you sure to terminate Wale completely?";
            AreYouSureToRestartWale = "Are you sure to restart Wale?";
        }
    }
}
