using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Wale.Configuration
{
    public class Manager
    {
        public Manager() { Init(); }

        public General General = new General();
        

        #region Properties storing system Variables
        public string InstalledPath => System.AppDomain.CurrentDomain.BaseDirectory;
        public string AppDataPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaleAudioControl");
        public string WorkingPath { get; set; } = Directory.GetCurrentDirectory();
        private string StoringPath => System.IO.Path.Combine(WorkingPath, ConfFileName);
        private string ConfFileName => "WaleGeneral.conf";
        #endregion
        #region Methods
        private void Init()
        {
            PathInit();
            if (!IsSavedExists) { Reset(); }
            else { Read(); }
        }
        private void PathInit()
        {
            WorkingPath = InstalledPath;
            if (File.Exists(StoringPath)) { }
            else
            {
                Regex pf = new Regex(@"Program Files");
                WorkingPath = pf.IsMatch(InstalledPath) ? AppDataPath : InstalledPath;
            }
        }
        private bool IsSavedExists => File.Exists(StoringPath);

        private void Read()
        {
            JPack.JsonSaveManager.TryRead(out General c, WorkingPath, ConfFileName);
            General = c;
        }

        /// <summary>
        /// Stores current values of the application settings to a file.
        /// </summary>
        public void Save() => JPack.JsonSaveManager.Save(General, WorkingPath, ConfFileName);
        /// <summary>
        /// Upgrades application settings to reflect a more recent installation of the application.
        /// </summary>
        public void Upgrade()
        {
            throw new NotImplementedException("Upgrade");
        }

        /// <summary>
        /// Returns the value of the PreviousVersion property.
        /// This method is newly introduced for Wale and not same as original method of Properties.settings on WPF system.
        /// </summary>
        /// <returns></returns>
        public object GetPreviousVersion() => General.PreviousVersion;
        /// <summary>
        /// Returns the value of the named property for the previous version of the same application.
        /// This method is only for code continuity and always returns the result of GetPreviousVersion method w/o any params.
        /// </summary>
        /// <param name="propertyName">A string containing the name of the property whose the value is to be returned.</param>
        /// <returns></returns>
        public object GetPreviousVersion(string propertyName) => GetPreviousVersion();
        /// <summary>
        /// Restores the persisted application settings values to their corresponding default properties.
        /// </summary>
        public void Reset() => General.DefaultValues();
        #endregion

    }
}
