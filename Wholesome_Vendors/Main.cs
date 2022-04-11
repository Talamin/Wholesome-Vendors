using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using WholesomeToolbox;
using WholesomeVendors;
using WholesomeVendors.Blacklist;
using WholesomeVendors.Database;
using WholesomeVendors.WVSettings;
using WholesomeVendors.WVState;
using wManager;
using wManager.Plugin;
using Timer = robotManager.Helpful.Timer;

public class Main : IPlugin
{
    private readonly BackgroundWorker _pulseThread = new BackgroundWorker();
    private static string Name = "Wholesome Vendors";
    public static bool IsLaunched;

    private Timer stateAddTimer;

    public static string version = "1.3.01"; // Must match version in Version.txt

    public void Initialize()
    {
        try
        {
            PluginSettings.Load();
            Helpers.OverrideWRobotUserSettings();
            WTSettings.AddRecommendedBlacklistZones();
            WTSettings.AddRecommendedOffmeshConnections();
            WTTransport.AddRecommendedTransportsOffmeshes();
            NPCBlackList.AddNPCListToBlacklist();

            if (AutoUpdater.CheckUpdate(version))
            {
                Logger("New version downloaded, restarting, please wait");
                Helpers.Restart();
                return;
            }

            Logger($"Launching version {version} on client {WTLua.GetWoWVersion}");

            Logger($"Checking for actual Database, maybe download is needed");
            if (File.Exists("Data/WoWDB335"))
            {
                var databaseUpdater = new DBUpdater();
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

            FiniteStateMachineEvents.OnRunState += StateAddEventHandler;
            _pulseThread.RunWorkerAsync();

            if (PluginSettings.CurrentSetting.DrinkNbToBuy > 0 || PluginSettings.CurrentSetting.FoodNbToBuy > 0)
            {
                wManagerSetting.CurrentSetting.TryToUseBestBagFoodDrink = false;
                wManagerSetting.CurrentSetting.Save();
            }

            IsLaunched = true;
            PluginCache.Initialize();
            MemoryDB.Initialize();
        }
        catch (Exception ex)
        {
            LoggerError("Something gone wrong!" + ex);
        }
    }

    public void Dispose()
    {
        PluginCache.Dispose();
        MemoryDB.Dispose();
        IsLaunched = false;
        Helpers.RestoreWRobotUserSettings();
        FiniteStateMachineEvents.OnRunState -= StateAddEventHandler;
        _pulseThread.Dispose();
        Logger("Disposed");
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

            WTState.AddState(engine, new SellState(), "To Town");
            WTState.AddState(engine, new TrainingState(), "To Town");
            WTState.AddState(engine, new BuyFoodState(), "To Town");
            WTState.AddState(engine, new BuyDrinkState(), "To Town");
            WTState.AddState(engine, new BuyAmmoState(), "To Town");
            WTState.AddState(engine, new RepairState(), "To Town");
            WTState.AddState(engine, new BuyMountState(), "To Town");
            WTState.AddState(engine, new BuyBagsState(), "To Town");
            WTState.AddState(engine, new BuyPoisonState(), "To Town");
            engine.RemoveStateByName("Trainers");
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