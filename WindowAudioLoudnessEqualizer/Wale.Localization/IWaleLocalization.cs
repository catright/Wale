using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Localization
{
    public interface IWaleLocalization
    {
        string Locale { get; }

        string View { get; }
        string Target { get; }
        string Device { get; }
        string Master { get; }
        string AlwaysTop { get; }
        string StayOn { get; }
        string AdvancedView { get; }

        string Config { get; }
        string Configuration { get; }
        string Audio { get; }
        string UIUpdate { get; }
        string AutoControl { get; }
        string GCInterval { get; }
        string TargetLevel { get; }
        string UpRate { get; }
        string Kurtosis { get; }
        string Function { get; }
        string Original { get; }
        string Graph { get; }
        string AverageTime { get; }
        string MinPeakLevel { get; }
        string AverageEnabled { get; }
        string AutoControlEnabled { get; }
        string Window { get; }
        string RunAtWindowsStartup { get; }
        string AdvancedViewConf { get; }
        string DetailedLog { get; }
        string Priority { get; }
        string Normal { get; }
        string AboveNormal { get; }
        string High { get; }
        string Save { get; }
        string SaveAndClose { get; }
        string ReturnToDefault { get; }
        string Cancel { get; }

        string Log { get; }

        string DeviceMap { get; }
        string Update { get; }

        string OpenLog { get; }

        string Help { get; }
        string HelpMsg { get; }
        string CopiedToClip { get; }
        string LinkTooltip { get; }

        string License { get; }
        string Localization { get; }
        string LocalizationMsg { get; }

        string Restart { get; }
        string Close { get; }

        string Exitxml { get; }
        string Exit { get; }
        string AreYouSureToTerminateWaleCompletely { get; }
        string AreYouSureToRestartWale { get; }
    }
}
