// Fsm state to manage target at attack
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Class;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using DatabaseManager.Enums;
using DatabaseManager.Filter;
using DatabaseManager.Types;
using DatabaseManager.WoW;

public class BuyPoison : State
{
    public override string DisplayName
    {
        get { return "Buying Poison"; }
    }

    public override int Priority
    {
        get { return _priority; }
        set { _priority = value; }
    }

    private int _priority;

    public override List<State> NextStates
    {
        get { return new List<State>(); }
    }

    public override List<State> BeforeStates
    {
        get { return new List<State>(); }
    }

    public static int continentId = Usefuls.ContinentId; //0=Azeroth, 1=Kalimdor, 571=Northrend, 
    public static WoWLocalPlayer Me = ObjectManager.Me;
    public static uint InstantPoison;
    public static uint DeadlyPoison;
 

    private static Dictionary<int, uint> InstantPoisonDictionary = new Dictionary<int, uint>
    {
        { 79, 43231 },
        { 73, 43230 },
        { 68, 21927 },
        { 60, 8928 },
        { 52, 8927 },
        { 44, 8926 },
        { 36, 6950 },
        { 28, 6949 },
        { 20, 6947 }
    };

    private static Dictionary<int, uint> DeadlyPoisonDictionary = new Dictionary<int, uint>
    {
        { 80, 43233 },
        { 76, 43232 },
        { 70, 22054 },
        { 62, 22053 },
        { 60, 20844 },
        { 54, 8985 },
        { 46, 8984 },
        { 38, 2893 },
        { 30, 2892 }
    };

    // If this method return true, wrobot launch method Run(), if return false wrobot go to next state in FSM
    public override bool NeedToRun
    {
        get
        {
            if (ObjectManager.Me.InCombat || ObjectManager.Me.InCombatFlagOnly || ObjectManager.Me.IsDead)
            {
                return false;
            }
            if (!PluginSettings.CurrentSetting.AllowAutobuyPoison)
            {
                return false;
            }
            if (ObjectManager.Me.WowClass == WoWClass.Rogue)
            {
                //Logging.Write("Poison set: " + InstantPoison);
                if (ObjectManager.Me.WowClass == WoWClass.Rogue && ObjectManager.Me.Level >= 20 && ItemsManager.GetItemCountById(InstantPoison) == 0)
                {
                    Logging.Write("We have to Buy InstantPoison");
                    DisplayName = "Buying Poison";
                    return true;
                }
                if (ObjectManager.Me.WowClass == WoWClass.Rogue && ObjectManager.Me.Level >= 30 && ItemsManager.GetItemCountById(DeadlyPoison) == 0)
                {
                    Logging.Write("We have to Buy DeadlyPoison");
                    DisplayName = "Buying Poison";
                    return true;
                }
                return false;
            }
            return false;
        }
    }

    // If NeedToRun() == true
    public override void Run()
    {
        SetBuy();
        Database.ChooseDatabaseBuyVendorPoisonNPC();
        Logging.Write("Running to buy Poisons");
        Logging.Write("Nearest Vendor from player:\n" + "Name: " + Database.BuyVendorsPoison?.Name + "[" + Database.BuyVendorsPoison?.id + "]\nPosition: " + Database.BuyVendorsPoison?.Position.ToStringXml() + "\nDistance: " + Database.BuyVendorsPoison?.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
        if (ObjectManager.Me.WowClass == WoWClass.Rogue && ItemsManager.GetItemCountById(InstantPoison) <= 20)
        {
            int instpoison = 10 - ItemsManager.GetItemCountById(InstantPoison);
            Logging.Write("No Poison found, time to buy some InstantPoison! " + instpoison + " " + InstantPoison);
            GoToTask.ToPositionAndIntecractWithNpc(Database.BuyVendorsPoison.Position, Database.BuyVendorsPoison.id);
            while (ItemsManager.GetItemCountById(InstantPoison) < 20)
            {
                if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Name == Database.BuyVendorsPoison.Name) <= 0)
                {
                    Logging.Write("Looks like " + Database.BuyVendorsPoison + " is not here, we choose another one");
                    if (!Blacklist.myBlacklist.Contains(Database.BuyVendorsPoison.id))
                    {
                        Blacklist.myBlacklist.Add(Database.BuyVendorsPoison.id);
                        Thread.Sleep(50);
                        return;
                    }
                }
                Vendor.BuyItem(ItemsManager.GetNameById(InstantPoison), instpoison);
                Thread.Sleep(10);
                if (!wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(ItemsManager.GetNameById(InstantPoison)))
                {
                    wManager.wManagerSetting.CurrentSetting.DoNotSellList.Add(ItemsManager.GetNameById(InstantPoison));
                }
            }
            Thread.Sleep(Usefuls.LatencyReal * Usefuls.Latency);
        }
        if (ObjectManager.Me.WowClass == WoWClass.Rogue && ItemsManager.GetItemCountById(DeadlyPoison) <= 10 && ObjectManager.Me.Level >= 30)
        {
            int deadpoison = 10 - ItemsManager.GetItemCountById(DeadlyPoison);
            Logging.Write("No Poison found, time to buy some DeadlyPoison! " + deadpoison + " " + DeadlyPoison);
            GoToTask.ToPositionAndIntecractWithNpc(Database.BuyVendorsPoison.Position, Database.BuyVendorsPoison.id);
            while (ItemsManager.GetItemCountById(DeadlyPoison) < 20)
            {
                if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Name == Database.BuyVendorsPoison.Name) <= 0)
                {
                    Logging.Write("Looks like " + Database.BuyVendorsPoison + " is not here, we choose another one");
                    if (!Blacklist.myBlacklist.Contains(Database.BuyVendorsPoison.id))
                    {
                        Blacklist.myBlacklist.Add(Database.BuyVendorsPoison.id);
                        Thread.Sleep(50);
                        return;
                    }
                }
                Vendor.BuyItem(ItemsManager.GetNameById(DeadlyPoison), deadpoison);
                Thread.Sleep(10);
                if (!wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(ItemsManager.GetNameById(DeadlyPoison)))
                {
                    wManager.wManagerSetting.CurrentSetting.DoNotSellList.Add(ItemsManager.GetNameById(DeadlyPoison));
                }
            }
            Thread.Sleep(Usefuls.LatencyReal * Usefuls.Latency);
        }
    }

    public static void SetBuy()
    {
        foreach (int level in InstantPoisonDictionary.Keys)
        {
            if (level <= Me.Level)
            {
                InstantPoison = InstantPoisonDictionary[level];
                string IPName = ItemsManager.GetNameById(InstantPoisonDictionary[level]);
                if(!wManagerSetting.CurrentSetting.DoNotSellList.Contains(IPName))
                {
                    wManagerSetting.CurrentSetting.DoNotSellList.Add(IPName);
                }
                break;
            }
        }

        foreach (int level in DeadlyPoisonDictionary.Keys)
        {
            if (level <= Me.Level)
            {
                //Logging.WriteDebug($"Selected Deadly Poison Level {level}");
                DeadlyPoison = DeadlyPoisonDictionary[level];
                string DPName = ItemsManager.GetNameById(DeadlyPoisonDictionary[level]);
                if (!wManagerSetting.CurrentSetting.DoNotSellList.Contains(DPName))
                {
                    wManagerSetting.CurrentSetting.DoNotSellList.Add(DPName);
                }
                break;
            }
        }

    }

}