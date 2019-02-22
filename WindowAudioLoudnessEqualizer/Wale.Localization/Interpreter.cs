using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Localization
{
    public class Interpreter
    {
        
    }
    [Serializable]
    public class WaleLocalization : IWaleLocalization
    {
        public string View { get; set; } = "View(F2)";
        public string Target { get; set; } = "Target";
        public string Device { get; set; } = "Device";
        public string Master { get; set; } = "Master";
        public string AlwaysTop { get; set; } = "AlwaysTop(F7)";
        public string StayOn { get; set; } = "StayOn(F8)";
        public string DetailView { get; set; } = "DetailView(F9)";

        public string Config { get; set; } = "Config(F3)";
        public string Configuration { get; set; } = "Configuration";
        public string Audio { get; set; } = "Audio";
        public string UIUpdate { get; set; } = "UI Update [ms]";
        public string AutoControl { get; set; } = "Auto Control [ms]";
        public string GCInterval { get; set; } = "GC Interval [ms]";
        public string TargetLevel { get; set; } = "Target Level";
        public string UpRate { get; set; } = "Up Rate";
        public string Kurtosis { get; set; } = "Kurtosis";
        public string Function { get; set; } = "Function";
        public string Original { get; set; } = "Original";
        public string Graph { get; set; } = "Graph";
        public string AverageTime { get; set; } = "Average Time [ms]";
        public string MinPeakLevel { get; set; } = "Min Peak Level";
        public string AverageEnabled { get; set; } = "Average Enabled";
        public string AutoControlEnabled { get; set; } = "Auto Control Enabled";
        public string Window { get; set; } = "Window";
        public string RunAtWindowsStartup { get; set; } = "Run at Windows Startup";
        public string DetailedView { get; set; } = "Detailed View";
        public string DetailedLog { get; set; } = "Detailed Log";
        public string Priority { get; set; } = "Priority";
        public string Normal { get; set; } = "Normal";
        public string AboveNormal { get; set; } = "Above Normal";
        public string High { get; set; } = "High";
        public string Save { get; set; } = "Save";
        public string SaveAndClose { get; set; } = "Save and Close";
        public string ReturnToDefault { get; set; } = "Return to Default";
        public string Cancel { get; set; } = "Cancel";

        public string Log { get; set; } = "Log(F4)";

        public string DeviceMap { get; set; } = "Device Map";
        public string Update { get; set; } = "Update";
        public string Close { get; set; } = "Close";
        public string OpenLog { get; set; } = "Open Log";

        public string Help { get; set; } = "Help";
        public string HelpMsg { get; set; } = @"Latest usage and full description are always firstly updated on Github Wiki(https://github.com/catright/Wale/wiki)
Last Update 0.5.7


Usage

Wale tries to adjust all sound generating processes' peak level to Target-Yellow bar- level. You can change Target to your preferred level.

However, Wale uses windows volume system for now, which means the app only can control volume between 0.0~1.0. Hence, if you set Target to near 1.0, then Wale could not control volume, because all processes' peak levels are always less than or equal to Target. So, you should lower Target a little. Suggested Target level is about 0.25(-6dB). Default is 0.15(-8dB).

    Caution! Too low Target may cause sudden hugely loud sound. Low Target means quieter output sound from your windows system. Then, you may need to increase the volume of your hardware such as speaker to hear the sound. In this situation, uncontrolled process would make very loud sound caused by some trouble of sound system include unintentional stop of Wale.

    Click blue checkbox on the session to uncheck it if you want to exclude some session from auto control. This exclusion is not saved when Wale is closed.
    Click process name or white checkbox on the session to mute it. The mute state is saved in the process itself so kept when Wale is closed.
    You can use your mouse wheel to adjust Relative-White bar- volume for each session. Relative value is not saved when Wale is closed.";

        public string License { get; set; } = "License";
        public string Localization { get; set; } = "Localization";
        public string LocalizationMsg { get; set; } = "";

        public string Exit { get; set; } = "Exit";
        public string AreYouSureToTerminateWaleCompletely { get; set; } = "Are you sure to terminate Wale completely?";
    }
    public interface IWaleLocalization
    {
        string View { get; set; }
        string Target { get; set; }
        string Device { get; set; }
        string Master { get; set; }
        string AlwaysTop { get; set; }
        string StayOn { get; set; }
        string DetailView { get; set; }

        string Config { get; set; }
        string Configuration { get; set; }
        string Audio { get; set; }
        string UIUpdate { get; set; }
        string AutoControl { get; set; }
        string GCInterval { get; set; }
        string TargetLevel { get; set; }
        string UpRate { get; set; }
        string Kurtosis { get; set; }
        string Function { get; set; }
        string Original { get; set; }
        string Graph { get; set; }
        string AverageTime { get; set; }
        string MinPeakLevel { get; set; }
        string AverageEnabled { get; set; }
        string AutoControlEnabled { get; set; }
        string Window { get; set; }
        string RunAtWindowsStartup { get; set; }
        string DetailedView { get; set; }
        string DetailedLog { get; set; }
        string Priority { get; set; }
        string Normal { get; set; }
        string AboveNormal { get; set; }
        string High { get; set; }
        string Save { get; set; }
        string SaveAndClose { get; set; }
        string ReturnToDefault { get; set; }
        string Cancel { get; set; }

        string Log { get; set; }

        string DeviceMap { get; set; }
        string Update { get; set; }
        string Close { get; set; }
        string OpenLog { get; set; }

        string Help { get; set; }
        string HelpMsg { get; set; }

        string License { get; set; }
        string Localization { get; set; }
        string LocalizationMsg { get; set; }

        string Exit { get; set; }
        string AreYouSureToTerminateWaleCompletely { get; set; }
    }
}
