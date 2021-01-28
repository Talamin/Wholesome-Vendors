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

    private static CreatureFilter PoisonVendor = new CreatureFilter
    {

        ExcludeIds = Blacklist.myBlacklist,

        ContinentId = (ContinentId)Usefuls.ContinentId,

        Faction = new Faction(ObjectManager.Me.Faction,
            ReactionType.Friendly),

        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.VENDOR_POISON
            }),

        Range = new Range(ObjectManager.Me.Position)          //is needed for auto-order function by .Get() method
    };

    // If this method return true, wrobot launch method Run(), if return false wrobot go to next state in FSM
    public override bool NeedToRun
    {
        get
        {
            if (ObjectManager.Me.WowClass == WoWClass.Rogue &&  Me.IsAlive && !Me.InCombat)
            {
                //Logging.Write("Poison set: " + InstantPoison);
                if (ObjectManager.Me.WowClass == WoWClass.Rogue && ObjectManager.Me.Level >= 20 && ItemsManager.GetItemCountById(InstantPoison) == 0)
                {
                    Logging.Write("We have to Buy Poison");
                    DisplayName = "Buying Poison";
                    return true;
                }
                if (ObjectManager.Me.WowClass == WoWClass.Rogue && ObjectManager.Me.Level >= 30 && ItemsManager.GetItemCountById(DeadlyPoison) == 0)
                {
                    Logging.Write("We have to Buy Poison");
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
        var poisonVendor = DbCreature.GetNearest(PoisonVendor, ObjectManager.Me.Position, 5000);
        Logging.Write("Running to buy Poisons");
        Logging.Write("Nearest Vendor from player:\n" + "Name: " + poisonVendor?.Name + "[" + poisonVendor?.id + "]\nPosition: " + poisonVendor?.Position.ToStringXml() + "\nDistance: " + poisonVendor?.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
        if (ObjectManager.Me.WowClass == WoWClass.Rogue && ItemsManager.GetItemCountById(InstantPoison) <= 20)
        {
            int instpoison = 10 - ItemsManager.GetItemCountById(InstantPoison);
            Logging.Write("No Poison found, time to buy some InstantPoison! " + instpoison + " " + InstantPoison);
            GoToTask.ToPositionAndIntecractWithNpc(poisonVendor.Position, poisonVendor.id);
            while (ItemsManager.GetItemCountById(InstantPoison) < 20)
            {
                Vendor.BuyItem(ItemsManager.GetNameById(InstantPoison), instpoison);
                Thread.Sleep(10);
            }
            Thread.Sleep(Usefuls.LatencyReal * Usefuls.Latency);
        }
        if (ObjectManager.Me.WowClass == WoWClass.Rogue && ItemsManager.GetItemCountById(DeadlyPoison) <= 10 && ObjectManager.Me.Level >= 30)
        {
            int deadpoison = 10 - ItemsManager.GetItemCountById(DeadlyPoison);
            Logging.Write("No Poison found, time to buy some DeadlyPoison! " + deadpoison + " " + DeadlyPoison);
            GoToTask.ToPositionAndIntecractWithNpc(poisonVendor.Position, poisonVendor.id);
            while (ItemsManager.GetItemCountById(DeadlyPoison) < 20)
            {
                Vendor.BuyItem(ItemsManager.GetNameById(DeadlyPoison), deadpoison);
                Thread.Sleep(10);
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