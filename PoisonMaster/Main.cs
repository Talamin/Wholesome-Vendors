using System;
using System.ComponentModel;
using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using wManager.Plugin;
using wManager.Wow.Helpers;
using PoisonMaster;
using System.Drawing;
using Timer = robotManager.Helpful.Timer;
using wManager;
using WoWDBUpdater;
using System.Threading.Tasks;
using System.Net;
using System.IO;

public class Main : IPlugin
{
    private readonly BackgroundWorker _pulseThread = new BackgroundWorker();
    private static string Name = "Wholesome Vendors";
    public static bool IsLaunched;

    private Timer stateAddTimer;

    public static string version = "1.1.02"; // Must match version in Version.txt

    private DB _database;

    public void Initialize()
    {
        try
        {
            PluginSettings.Load();
            Helpers.OverrideWRobotUserSettings();
            NPCBlackList.AddNPCListToBlacklist();

            if (AutoUpdater.CheckUpdate(version))
            {
                Logger("New version downloaded, restarting, please wait");
                Helpers.Restart();
                return;
            }

            Logger($"Launching version {version} on client {Helpers.GetWoWVersion()}");

            Logger($"Checking for actual Database, maybe download is needed");
            if (File.Exists("Data/WoWDB335"))
            {
                _database = new DB();
                var databaseUpdater = new DBUpdater(_database);
                if (databaseUpdater.CheckUpdate())
                {
                    databaseUpdater.Update();
                }
            }
            else
            {
                Logger($"Downloading Wholesome DB");
                Task.Factory.StartNew(() =>
                {
                    using (var client = new WebClient())
                    {
                        try
                        {
                            client.DownloadFile("https://s3-eu-west-1.amazonaws.com/wholesome.team/WoWDb335.zip",
                            "Data/wholesome_db_temp.zip");
                        }
                        catch (WebException e)
                        {
                            LoggerError($"Failed to download/write Wholesome Database!\n" + e.Message);
                            return false;
                        }
                    }

                    Logger($"Extracting Wholesome Database.");

                        System.IO.Compression.ZipFile.ExtractToDirectory("Data/wholesome_db_temp.zip", "Data");
                        File.Delete("Data/wholesome_db_temp.zip");

                    Logger($"Successfully downloaded Wholesome Database");
                    return true;
                });
            }

            EventsLua.AttachEventLua("PLAYER_EQUIPMENT_CHANGED", m => Helpers.GetRangedWeaponType());
            FiniteStateMachineEvents.OnRunState += StateAddEventHandler;
            IsLaunched = true;
            _pulseThread.RunWorkerAsync();

            if (PluginSettings.CurrentSetting.DrinkNbToBuy > 0 || PluginSettings.CurrentSetting.FoodNbToBuy > 0)
            {
                wManagerSetting.CurrentSetting.TryToUseBestBagFoodDrink = false;
                wManagerSetting.CurrentSetting.Save();
            }
        }
        catch (Exception ex)
        {
            LoggerError("Something gone wrong!" + ex);
        }
    }

    public void Dispose()
    {
        IsLaunched = false;
        _database?.Dispose();
        Helpers.RestoreWRobotUserSettings();
        FiniteStateMachineEvents.OnRunState -= StateAddEventHandler;
        _pulseThread.Dispose();
        Logger("Plugin was terminated!");
    }

    public void Settings()
    {
        PluginSettings.Load();
        PluginSettings.CurrentSetting.ShowConfiguration();
        PluginSettings.CurrentSetting.Save();
    }

    private void StateAddEventHandler(Engine engine, State state, CancelEventArgs canc)
    {
        if (engine.States.Count <= 5)
        {
            if (stateAddTimer == null)
                Helpers.SoftRestart(); // hack to wait for correct engine to trigger
            return;
        }

        if (stateAddTimer == null)
            stateAddTimer = new Timer();

        if (stateAddTimer.IsReady && engine != null)
        {
            stateAddTimer = new Timer(3000);

            Helpers.AddState(engine, new BuyPoisonState(), "To Town");
            Helpers.AddState(engine, new BuyAmmoState(), "To Town");
            Helpers.AddState(engine, new BuyFoodState(), "To Town");
            Helpers.AddState(engine, new BuyDrinkState(), "To Town");
            Helpers.AddState(engine, new RepairState(), "To Town");
            Helpers.AddState(engine, new TrainingState(), "Trainers");
            //engine.RemoveStateByName("To Town");
        }
    }

    public static void Logger(string message)
    {
        Logging.Write($"[{Name}]: { message}", Logging.LogType.Normal, Color.ForestGreen);
    }
    public static void LoggerError(string message)
    {
        Logging.Write($"[{Name}]: { message}", Logging.LogType.Normal, Color.Red);
    }
}