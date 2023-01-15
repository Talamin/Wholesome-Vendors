using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
    private static string Name = "Wholesome Vendors";
    public static bool IsLaunched;
    private Timer stateAddTimer;
    public static string version = FileVersionInfo.GetVersionInfo(Others.GetCurrentDirectory + @"\Plugins\Wholesome_Vendors.dll").FileVersion;
    private bool _statesAdded;

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

            FiniteStateMachineEvents.OnRunState += StateAddEventHandler;

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
            LoggerError("Something gone wrong!\n" + ex.Message + "\n" + ex.StackTrace);
        }
    }

    public void Dispose()
    {
        PluginCache.Dispose();
        MemoryDB.Dispose();
        IsLaunched = false;
        Helpers.RestoreWRobotUserSettings();
        FiniteStateMachineEvents.OnRunState -= StateAddEventHandler;
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
        if (_statesAdded)
        {
            Logger($"States added");
            FiniteStateMachineEvents.OnRunState -= StateAddEventHandler;
            return;
        }

        if (engine.States.Count <= 5)
        {
            if (stateAddTimer == null)
            {
                Helpers.SoftRestart(); // hack to wait for correct engine to trigger
            }
            return;
        }

        if (!engine.States.Exists(eng => eng.DisplayName == "To Town"))
        {
            LoggerError("The product you're currently using doesn't have a To Town state. Can't start.");
            Dispose();
            return;
        }

        if (stateAddTimer == null)
            stateAddTimer = new Timer();

        if (stateAddTimer.IsReady && engine != null)
        {
            stateAddTimer = new Timer(3000);

            WTState.AddState(engine, new BuyPoisonState(), "To Town");
            WTState.AddState(engine, new BuyBagsState(), "To Town");
            WTState.AddState(engine, new BuyMountState(), "To Town");
            WTState.AddState(engine, new TrainingState(), "To Town");
            WTState.AddState(engine, new BuyFoodState(), "To Town");
            WTState.AddState(engine, new BuyDrinkState(), "To Town");
            WTState.AddState(engine, new BuyAmmoState(), "To Town");
            WTState.AddState(engine, new RepairState(), "To Town");
            WTState.AddState(engine, new SellState(), "To Town");

            engine.RemoveStateByName("Trainers");
            _statesAdded = true;
        }
    }

    public static void Logger(string message)
    {
        Logging.Write($"[{Name}]: {message}", Logging.LogType.Normal, Color.ForestGreen);
    }
    public static void LoggerError(string message)
    {
        Logging.Write($"[{Name}]: {message}", Logging.LogType.Normal, Color.Red);
    }
}