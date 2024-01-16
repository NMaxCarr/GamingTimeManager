using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GamingTimeManager
{
    public enum Period
    {
        Daily,
        Weekly
    }
    public class GamingTimeManagerSettings : ObservableObject
    {
        private Period gamingSessionTimePeriod = Period.Daily;
        private uint gamingSessionTimeGoal = 3210;
        private bool sessionTimeOverNotification = false;
        private bool optionThatWontBeSaved = false;

        public Period GamingSessionTimePeriod { get => gamingSessionTimePeriod; set => SetValue(ref gamingSessionTimePeriod, value); }

        [DontSerialize]
        public Period[] GamingSessionPeriods 
        {
            get { return (Period[])Enum.GetValues(typeof(Period)); }
        }
        
        public uint GamingSessionTimeGoal { get => gamingSessionTimeGoal; set => SetValue(ref gamingSessionTimeGoal, value); }

        public bool SessionTimeOverNotification { get => sessionTimeOverNotification; set => SetValue(ref sessionTimeOverNotification, value); }

        [DontSerialize]
        public bool OptionThatWontBeSaved { get => optionThatWontBeSaved; set => SetValue(ref optionThatWontBeSaved, value); }
    }

    public class GamingTimeManagerSettingsViewModel : ObservableObject, ISettings
    {
        private readonly GamingTimeManager plugin;
        private GamingTimeManagerSettings editingClone { get; set; }

        private GamingTimeManagerSettings settings;
        public GamingTimeManagerSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public GamingTimeManagerSettingsViewModel(GamingTimeManager plugin)
        {
            this.plugin = plugin;

            var savedSettings = plugin.LoadPluginSettings<GamingTimeManagerSettings>();

            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new GamingTimeManagerSettings();
            }
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}