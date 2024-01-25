using Microsoft.Toolkit.Uwp.Notifications;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;

namespace GamingTimeManager
{
    public class GamingTimePeriodData
    {
        public DateTime PeriodDateStart { get; set; }

        public DateTime PeriodDateEnd { get; set; }

        public TimeSpan PeriodTimePlayed { get; set; }

        [DontSerialize]
        public DateTime CurrentGameStartDate { get; set; }

    }
    public class GamingTimeManager : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GamingTimeManagerSettingsViewModel Settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("8cb3c9f6-fe18-4f9f-ac4c-dfba4ef46a72");

        private readonly string DataSaveFileName = "session.json";

        private GamingTimePeriodData gamingSessionData;

        private CancellationTokenSource dataSavingTokenSource;

        private TimeSpan initialSessionTimePlayed;

        private readonly int DataSavingTaskTick = 10000;

        private bool timeOverNotificationFlag = false;

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
            string dataPath = GetPluginUserDataPath();

            bool isDataDeserialized = Serialization.TryFromJsonFile(Path.Combine(dataPath, DataSaveFileName), out gamingSessionData, out Exception deserializeJSONException);

            if (!isDataDeserialized)
            {
                if (deserializeJSONException is System.IO.FileNotFoundException)
                {

                    gamingSessionData = CreateDefaultGamingTimePeriodData();

                    logger.Info("Creating data file " + DataSaveFileName + " in folder " + dataPath);

                    WriteGamingSessionTimeData(dataPath);
                }
                else
                {
                    logger.Error(deserializeJSONException.Message);
                    return;
                }
            }

            dataSavingTokenSource = new CancellationTokenSource();
            dataSavingTokenSource.Token.Register(() => 
            { 
                gamingSessionData.PeriodTimePlayed = CalculateGamingSessionTimespan(DateTime.UtcNow, gamingSessionData.CurrentGameStartDate) + initialSessionTimePlayed;
                WriteGamingSessionTimeData(dataPath);
            });

            Task.Run(async () =>
            {
                gamingSessionData.CurrentGameStartDate = DateTime.UtcNow;
                initialSessionTimePlayed = gamingSessionData.PeriodTimePlayed;

                while (!dataSavingTokenSource.IsCancellationRequested)
                {
                    if (IsPassedDate(gamingSessionData.PeriodDateEnd)) IncrementGamingTimePeriod();

                    gamingSessionData.PeriodTimePlayed = CalculateGamingSessionTimespan(DateTime.UtcNow, gamingSessionData.CurrentGameStartDate) + initialSessionTimePlayed;
                    WriteGamingSessionTimeData(dataPath);

                    if (IsMaxGamingTimeReached() && Convert.ToDouble(Settings.Settings.GamingTimePeriodGoal) > 0) DisplayTimeOverToasterNotification();

                    await Task.Delay(DataSavingTaskTick);
                }
            }, dataSavingTokenSource.Token);
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            try
            {
                dataSavingTokenSource.Cancel();
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
            }

            NotifyGamingTimeSummary();
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GamingTimeManagerSettingsView();
        }

        private void WriteGamingSessionTimeData(string dataPath)
        {
            try
            {
                File.WriteAllText(Path.Combine(dataPath, DataSaveFileName), Serialization.ToJson(gamingSessionData));
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
            }
        }

        private TimeSpan CalculateGamingSessionTimespan(DateTime start, DateTime end)
        {
            return start - end;
        }

        private bool IsMaxGamingTimeReached() {

            TimeSpan maxGoal = TimeSpan.FromMinutes(Convert.ToDouble(Settings.Settings.GamingTimePeriodGoal));

            return gamingSessionData.PeriodTimePlayed >= maxGoal;
        }

        private void DisplayTimeOverToasterNotification()
        {
            if (timeOverNotificationFlag == false && Settings.Settings.NotifyOnTimeGoalReached == true)
            {
                new ToastContentBuilder()
                    .AddText("Time's over")
                    .AddText("You've reached your maximun gaming time goal set")
                    .SetToastScenario(ToastScenario.Alarm)
                    .AddAudio(new Uri("ms-appx:///Audio/NotificationSound.mp3"))
                    .Show();

                timeOverNotificationFlag = true;
            }
        }

        private bool IsPassedDate(DateTime start)
        {
            return start > DateTime.UtcNow;
        }

        private void IncrementGamingTimePeriod()
        {
            gamingSessionData.PeriodDateStart = DateTime.UtcNow;
            gamingSessionData.PeriodDateEnd = gamingSessionData.PeriodDateStart.Add(GetTimespanFromPeriod(Settings.Settings.GamingTimePeriod));
            gamingSessionData.PeriodTimePlayed = TimeSpan.Zero;
            gamingSessionData.CurrentGameStartDate = gamingSessionData.PeriodDateStart;
        }

        private TimeSpan GetTimespanFromPeriod(Period period)
        {
            TimeSpan span;
            
            switch (period)
            {
                case Period.Daily:
                    span = TimeSpan.FromDays(1);
                    break;

                case Period.Weekly:
                    span = TimeSpan.FromDays(7);
                    break;

                default:
                    span = TimeSpan.Zero;
                    break;
            }

            return span;
        }

        private GamingTimePeriodData CreateDefaultGamingTimePeriodData() {

            DateTime SessionDateStart = DateTime.UtcNow;
            DateTime SessionDateEnd = SessionDateStart.Add(GetTimespanFromPeriod(Settings.Settings.GamingTimePeriod));

            return new GamingTimePeriodData
            {
                PeriodDateStart = SessionDateStart,
                PeriodDateEnd = SessionDateEnd,
                PeriodTimePlayed = TimeSpan.Zero,
                CurrentGameStartDate = SessionDateStart,
            };

        }

        private void NotifyGamingTimeSummary()
        {
            string timePlayed = gamingSessionData.PeriodTimePlayed.ToString(@"dd\.hh\:mm\:ss");

            string goal = TimeSpan.FromMinutes(Convert.ToDouble(Settings.Settings.GamingTimePeriodGoal)).ToString(@"dd\.hh\:mm\:ss");

            string summaryText = $"You've played {timePlayed} out of your {goal} limit.";
            PlayniteApi.Notifications.Add("1", summaryText, NotificationType.Info);
        }

    }
}