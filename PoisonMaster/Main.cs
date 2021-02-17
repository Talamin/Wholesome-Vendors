﻿using System;
using System.ComponentModel;
using System.Diagnostics.Eventing;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using robotManager.Products;
using wManager;
using wManager.Plugin;
using wManager.Wow.Bot.States;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using MarsSettingsGUI;
using PoisonMaster;
using System.Drawing;

public class Main : IPlugin
{
    //private static bool IsLaunched = false;
	private bool _stateAdded;
    private readonly BackgroundWorker _pulseThread = new BackgroundWorker();
    private static string Name = "Wholesome  Manager";
    public void Initialize()
    {
        try
        {
            PluginSettings.Load();
            EventsLua.AttachEventLua("PLAYER_EQUIPMENT_CHANGED", m => Helpers.CheckEquippedItems());
            //FiniteStateMachineEvents.OnStartEngine += StateAddEventHandler;
            //FiniteStateMachineEvents.OnAfterRunState += AfterStateAddEventHandler;
            FiniteStateMachineEvents.OnRunState += StateAddEventHandler;
            //IsLaunched = true;
			_stateAdded = false;
            //_pulseThread.DoWork += DoBackgroundPulse;
            _pulseThread.RunWorkerAsync();
            if(Helpers.HaveRanged())
            {
                Helpers.CheckEquippedItems();
            }
        }
        catch (Exception ex)
        {
            Main.LoggerError("Something gone wrong!" + ex);
        }
    }

    public void Dispose()
    {
        //FiniteStateMachineEvents.OnStartEngine -= StateAddEventHandler;
        //FiniteStateMachineEvents.OnAfterRunState -= AfterStateAddEventHandler;
        //IsLaunched = false;
        //_pulseThread.DoWork -= DoBackgroundPulse;
        _pulseThread.Dispose();
        Main.Logger("Plugin was terminated!");
    }

    public void Settings()
    {
        PluginSettings.Load();
        PluginSettings.CurrentSetting.ShowConfiguration();
        PluginSettings.CurrentSetting.Save();
    }
    //private void AfterStateAddEventHandler(Engine engine, State state)
    //{
    //    AddStates(engine);
    //}
    //private void StateAddEventHandler(Engine engine)
    //{
    //    AddStates(engine);
    //}
    private void StateAddEventHandler(Engine engine, State state, CancelEventArgs canc)
    {
        AddStates(engine);
    }

    private void AddStates(Engine engine)
	{
		if (!_stateAdded && engine != null)
		{
			try
			{
                Helpers.AddState(engine, new BuyPoison(), "To Town");
                Helpers.AddState(engine, new BuyArrows(), "Buying Poison");
                Helpers.AddState(engine, new BuyFood(), "Buying Arrows and Bullets");
                Helpers.AddState(engine, new BuyDrink(), "Buying Food");
                Helpers.AddState(engine, new Repair(), "Buying Drink");
                Helpers.AddState(engine, new SellItems(), "Repair Run");
                engine.States.Sort();
				_stateAdded = true;
			}
			catch (Exception e)
			{
				Main.LoggerError("" + e);
			}
		}
	}
	//private void DoBackgroundPulse(object sender, DoWorkEventArgs args)
	//{
	//	while (IsLaunched)
	//	{
	//		try
	//		{
	//			if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
	//			{
	//				//BuyPoison.SetBuy();
	//				//BuyArrows.SetBuy();
 //                   //Helpers.OutOfFoodVar = Helpers.OutOfFood();
 //                   //Helpers.OutOfDrinkVar = Helpers.OutOfDrink();
 //                   //Thread.Sleep(500);
 //               }
	//		}
	//		catch (Exception e)
	//		{
	//			Main.LoggerError("" + e);
	//		}

	//		Thread.Sleep(50);
	//	}
	//}
    public static void Logger(string message)
    {
        Logging.Write($"[{Name}]: { message}", Logging.LogType.Normal, Color.ForestGreen);
    }
    public static void LoggerError(string message)
    {
        Logging.Write($"[{Name}]: { message}", Logging.LogType.Normal, Color.Red);
    }
}