using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;

namespace GamingTimeManager
{
    public class GamingTimeSessionData
    {
        public DateTime SessionDateStart { get; set; }

        public DateTime SessionDateEnd { get; set; }

        public TimeSpan SessionTimePlayed { get; set; }
    }
    public class GamingTimeManager : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GamingTimeManagerSettingsViewModel Settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("8cb3c9f6-fe18-4f9f-ac4c-dfba4ef46a72");

        private readonly string DataSaveFileName = "session.json";

        private ulong CurrentSessionTime { get; set; }

        public GamingTimeManager(IPlayniteAPI api) : base(api)
        {
            Settings = new GamingTimeManagerSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }


        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            //Task.Delay(10000).Wait();
            //this.PlayniteApi.Dialogs.ShowMessage("1,2,Testing");
        }
        
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            string dataPath = GetPluginUserDataPath();

            bool isDataDeserialized = Serialization.TryFromJsonFile(Path.Combine(dataPath, DataSaveFileName), out GamingTimeSessionData gamingSessionData, out Exception deserializeJSONException);

            if (!isDataDeserialized)
            {
                if (deserializeJSONException is System.IO.FileNotFoundException)
                {
                    DateTime SessionDateStart = DateTime.UtcNow;
                    DateTime SessionDateEnd;

                    switch (this.Settings.Settings.GamingSessionTimePeriod)
                    {
                        case Period.Daily:
                            SessionDateEnd = SessionDateStart.AddDays(1);
                            break;

                        case Period.Weekly:
                            SessionDateEnd = SessionDateStart.AddDays(7);
                            break;

                        default:
                            //Not sure if the default behavior should be this
                            SessionDateEnd = SessionDateStart.AddDays(1);
                            break;
                    }

                    GamingTimeSessionData defaultGamingSessionData = new GamingTimeSessionData
                    {
                        SessionDateStart = SessionDateStart,
                        SessionDateEnd = SessionDateEnd,
                        SessionTimePlayed = TimeSpan.Zero,
                    };

                    logger.Info("Creating data file " + DataSaveFileName + " in folder " + dataPath);

                    File.WriteAllText(Path.Combine(dataPath, DataSaveFileName), Serialization.ToJson(defaultGamingSessionData));
                }
                else
                {
                    logger.Error(deserializeJSONException.Message);
                }
            }
        }

    public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            var sessionTime = args.ElapsedSeconds;
            var totalGamingTime = this.CurrentSessionTime + sessionTime;

            this.PlayniteApi.Notifications.Add("1", $"Current gaming session time: {sessionTime} s. Total game time played: {totalGamingTime} s", NotificationType.Info);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GamingTimeManagerSettingsView();
        }
    }
}