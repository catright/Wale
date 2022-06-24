using System;

namespace Wale.Localization
{
    [Serializable]
    public abstract class WaleLocalization : IWaleLocalization
    {
        public virtual string Locale { get; protected set; }

        public virtual string View { get; protected set; }
        public virtual string Target { get; protected set; }
        public virtual string Device { get; protected set; }
        public virtual string Master { get; protected set; }
        public virtual string AlwaysTop { get; protected set; }
        public virtual string StayOn { get; protected set; }
        public virtual string AdvancedView { get; protected set; }

        public virtual string Config { get; protected set; }
        public virtual string Configuration { get; protected set; }
        public virtual string Audio { get; protected set; }
        public virtual string UIUpdate { get; protected set; }
        public virtual string AutoControl { get; protected set; }
        public virtual string GCInterval { get; protected set; }
        public virtual string TargetLevel { get; protected set; }
        public virtual string UpRate { get; protected set; }
        public virtual string Kurtosis { get; protected set; }
        public virtual string Function { get; protected set; }
        public virtual string Original { get; protected set; }
        public virtual string Graph { get; protected set; }
        public virtual string AverageTime { get; protected set; }
        public virtual string MinPeakLevel { get; protected set; }
        public virtual string AverageEnabled { get; protected set; }
        public virtual string AutoControlEnabled { get; protected set; }
        public virtual string Window { get; protected set; }
        public virtual string RunAtWindowsStartup { get; protected set; }
        public virtual string AdvancedViewConf { get; protected set; }
        public virtual string DetailedLog { get; protected set; }
        public virtual string Priority { get; protected set; }
        public virtual string Normal { get; protected set; }
        public virtual string AboveNormal { get; protected set; }
        public virtual string High { get; protected set; }
        public virtual string Save { get; protected set; }
        public virtual string SaveAndClose { get; protected set; }
        public virtual string ReturnToDefault { get; protected set; }
        public virtual string Cancel { get; protected set; }

        public virtual string Log { get; protected set; }

        public virtual string DeviceMap { get; protected set; }
        public virtual string Update { get; protected set; }

        public virtual string AboutWale { get; protected set; }
        public virtual string OpenLog { get; protected set; }
        public virtual string WindowsSoundSetting { get; protected set; }
        public virtual string WindowsVolumeMixer { get; protected set; }

        public virtual string Help { get; protected set; }
        public virtual string HelpMsg { get; protected set; }
        public virtual string CopiedToClip { get; protected set; }
        public virtual string LinkTooltip { get; protected set; }

        public virtual string License { get; protected set; }
        public virtual string Localization { get; protected set; }
        public virtual string LocalizationMsg { get; protected set; }

        public virtual string Restart { get; protected set; }
        public virtual string Close { get; protected set; }

        public virtual string Exitxml { get; protected set; }
        public virtual string Exit { get; protected set; }
        public virtual string AreYouSureToTerminateWaleCompletely { get; protected set; }
        public virtual string AreYouSureToRestartWale { get; protected set; }
    }

}
