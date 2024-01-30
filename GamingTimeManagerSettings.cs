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
        Daily = 1,
        Weekly = 7,
    }
    public class GamingTimeManagerSettings : ObservableObject
    {
        private Period gamingTimePeriod = Period.Daily;
        private double gamingTimePeriodGoal = 0;
        private bool notifyOnTimeGoalReached = false;

        public Period GamingTimePeriod { get => gamingTimePeriod; set => SetValue(ref gamingTimePeriod, value); }
        
        public string GamingTimePeriodGoal { 
            get => gamingTimePeriodGoal.ToString(); 
            
            set 
            {
                if (value == "" || value == null)
                {
                    SetValue(ref gamingTimePeriodGoal, 0);
                }
                else
                {
                    SetValue(ref gamingTimePeriodGoal, Convert.ToDouble(value));
                }
            } 
        }

        public bool NotifyOnTimeGoalReached { get => notifyOnTimeGoalReached; set => SetValue(ref notifyOnTimeGoalReached, value); }

        [DontSerialize]
        public Period[] AvailableGamingSessionPeriods
        {
            get { return (Period[])Enum.GetValues(typeof(Period)); }
        }
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

        public bool IsPluginEnabled()
        {
            return Convert.ToDouble(Settings.GamingTimePeriodGoal) > 0;
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