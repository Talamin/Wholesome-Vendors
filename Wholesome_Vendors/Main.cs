using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using WholesomeToolbox;
using WholesomeVendors;
using WholesomeVendors.Managers;
using WholesomeVendors.Utils;
using WholesomeVendors.WVSettings;
using WholesomeVendors.WVState;
using wManager;
using wManager.Plugin;
using wManager.Wow.Enums;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class Main : IPlugin
{
    public static bool IsLaunched;
    private Timer stateAddTimer;
    public static string version = FileVersionInfo.GetVersionInfo(Others.GetCurrentDirectory + @"\Plugins\Wholesome_Vendors.dll").FileVersion;
    private bool _statesAdded;
    private IVendorTimerManager _vendorTimerManager;
    private IBlackListManager _blackListManager;
    private IPluginCacheManager _pluginCacheManager;
    private IMemoryDBManager _memoryDBManager;

    public void Initialize()
    {
        try
        {
            PluginSettings.Load();
            Helpers.OverrideWRobotUserSettings();
            WTSettings.AddRecommendedBlacklistZones();
            WTSettings.AddRecommendedOffmeshConnections();
            WTTransport.AddRecommendedTransportsOffmeshes();
            WTSettings.AddItemToDoNotSellAndMailList(new List<string>()
            {
                "Hearthstone",
                "Skinning Knife",
                "Mining Pick"
            });

            _vendorTimerManager = new VendorTimerManager();
            _vendorTimerManager.Initialize();
            _blackListManager = new BlackListManager(_vendorTimerManager);
            _blackListManager.Initialize();
            _memoryDBManager = new MemoryDBManager(_blackListManager);
            _memoryDBManager.Initialize();
            _pluginCacheManager = new PluginCacheManager(_memoryDBManager);
            _pluginCacheManager.Initialize();

            if (AutoUpdater.CheckUpdate(version))
            {
                Logger.Log("New version downloaded, restarting, please wait");
                Helpers.Restart();
                return;
            }

            Logger.Log($"Launching version {version} on client {WTLua.GetWoWVersion}");

            FiniteStateMachineEvents.OnRunState += StateAddEventHandler;

            if (PluginSettings.CurrentSetting.DrinkNbToBuy > 0 || PluginSettings.CurrentSetting.FoodNbToBuy > 0)
            {
                wManagerSetting.CurrentSetting.TryToUseBestBagFoodDrink = false;
                wManagerSetting.CurrentSetting.Save();
            }

            if (PluginSettings.CurrentSetting.FirstLaunch)
            {
                if (ObjectManager.Me.WowClass == WoWClass.Rogue)
                {
                    PluginSettings.CurrentSetting.BuyPoison = true;
                }
                if (ObjectManager.Me.WowClass == WoWClass.Hunter)
                {
                    PluginSettings.CurrentSetting.AmmoAmount = 2000;
                }
                PluginSettings.CurrentSetting.LastLevelTrained = (int)ObjectManager.Me.Level;

                PluginSettings.CurrentSetting.FirstLaunch = false;
                PluginSettings.CurrentSetting.Save();
            }

            IsLaunched = true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Something gone wrong!\n" + ex.Message + "\n" + ex.StackTrace);
        }
    }

    public void Dispose()
    {
        _blackListManager?.Dispose();
        _vendorTimerManager?.Dispose();
        _memoryDBManager?.Dispose();
        _pluginCacheManager?.Dispose();
        IsLaunched = false;
        Helpers.RestoreWRobotUserSettings();
        FiniteStateMachineEvents.OnRunState -= StateAddEventHandler;
        Logger.Log("Disposed");
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
            Logger.Log($"States added");
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
            Logger.LogError("The product you're currently using doesn't have a To Town state. Can't start.");
            Dispose();
            return;
        }

        if (stateAddTimer == null)
        {
            stateAddTimer = new Timer();
        }

        if (stateAddTimer.IsReady && engine != null)
        {
            stateAddTimer = new Timer(3000);

            // From bottom to top priority
            WTState.AddState(engine, new TrainWeaponsState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");
            WTState.AddState(engine, new BuyPoisonState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");
            WTState.AddState(engine, new BuyBagsState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");
            WTState.AddState(engine, new BuyMountState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");
            WTState.AddState(engine, new TrainingState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");
            WTState.AddState(engine, new BuyFoodState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");
            WTState.AddState(engine, new BuyDrinkState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");
            WTState.AddState(engine, new BuyAmmoState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");
            WTState.AddState(engine, new RepairState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");
            WTState.AddState(engine, new SellState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");
            WTState.AddState(engine, new SendMailState(_memoryDBManager, _pluginCacheManager, _vendorTimerManager, _blackListManager), "To Town");

            //engine.States.ForEach(s => Logger.Log($"state {s.DisplayName} with prio {s.Priority}"));

            engine.RemoveStateByName("Trainers");
            _statesAdded = true;
        }
    }
}