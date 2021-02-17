// Fsm state to manage target at attack
using DatabaseManager.Enums;
using DatabaseManager.Filter;
using DatabaseManager.Types;
using DatabaseManager.WoW;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Class;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using PoisonMaster;
using DatabaseManager.Tables;

public class BuyArrows : State
    {
        public override string DisplayName
        {
            get { return "Buying Arrows and Bullets"; }
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
        public static uint Arrow = 0;
        public static uint Bullet = 0;


        private static Dictionary<int, uint> ArrowDictionary = new Dictionary<int, uint>
    {
        { 75, 41586 },
        { 65, 28056 },
        { 55, 28053 },
        { 40, 11285 },
        { 25, 3030 },
        { 10, 2515 },
        { 1, 2512 },
    };
    private static Dictionary<int, uint> BulletsDictionary = new Dictionary<int, uint>
    {
        {75 ,41584},
        {65 ,28061},
        {55, 28060},
        {40, 11284},
        {25, 3033},
        {10, 2519 },
        {1, 2516 },
    };


        // If this method return true, wrobot launch method Run(), if return false wrobot go to next state in FSM
        public override bool NeedToRun
        {
            get
            {
                if (Me.IsDead || !Me.IsAlive || Me.InCombatFlagOnly)
                {
                    return false;
                }
                if(!PluginSettings.CurrentSetting.AllowAutobuyAmmunition)
                {
                    return false;
                }
                if (ObjectManager.Me.WowClass == WoWClass.Hunter && Helpers.HaveRanged())
                {
                    SetBuy();
                    if (ItemsManager.GetItemCountById(Arrow) == 0 && 
                    ObjectManager.Me.WowClass == WoWClass.Hunter && 
                    ItemsManager.GetItemCountById(Arrow) <= 50 &&
                    Helpers.RangedWeaponType == "Bows") 
                    {
                        Main.Logger("We have to Buy Arrows");
                        DisplayName = "Buying Arrows and Bullets";
                        return true;
                    }
                    if (ItemsManager.GetItemCountById(Bullet) == 0 &&
                    ObjectManager.Me.WowClass == WoWClass.Hunter &&
                    ItemsManager.GetItemCountById(Bullet) <= 50 &&
                    Helpers.RangedWeaponType == "Guns")
                    {
                        Main.Logger("We have to Buy Bullets");
                        DisplayName = "Buying Arrows and Bullets";
                        return true;
                    }
                }
                return false;
            }
        }

        // If NeedToRun() == true
        public override void Run()
        {
        SetBuy();
        Database.ChooseDatabaseAmmoNPC();
        //var ammoVendor = DbCreature.Get(AmmoVendor).OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).First();      
        if (!ObjectManager.Me.InCombat && !ObjectManager.Me.InCombatFlagOnly && !ObjectManager.Me.IsDead)
            {
                if (Database.AmmoVendors != null)
                {
                    if (ObjectManager.Me.Position.DistanceTo(Database.AmmoVendors.Position) >= 6)
                    {
                        Main.Logger("Running to Buy Arrows");
                        Main.Logger("Nearest AmmunitionVendor from player:\n" + "Name: " + Database.AmmoVendors?.Name + "[" + Database.AmmoVendors?.id + "]\nPosition: " + Database.AmmoVendors?.Position.ToStringXml() + "\nDistance: " + Database.AmmoVendors?.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
                        GoToTask.ToPositionAndIntecractWithNpc(Database.AmmoVendors.Position, Database.AmmoVendors.id);                
                    }
                    if (ObjectManager.Me.Position.DistanceTo(Database.AmmoVendors.Position) <= 5 && ItemsManager.GetItemCountById(Arrow) < 2000)
                    {
                        if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Name == Database.AmmoVendors.Name) <= 0)
                        {
                            Main.Logger("Looks like " + Database.AmmoVendors + " is not here, we choose another one");
                            if (!Blacklist.myBlacklist.Contains(Database.AmmoVendors.id))
                            {
                                Blacklist.myBlacklist.Add(Database.AmmoVendors.id);
                                Thread.Sleep(50);
                                return;
                            }
                        }
                        Main.Logger("No Arrows found, time to buy some! " + Arrow);
                        GoToTask.ToPositionAndIntecractWithNpc(Database.AmmoVendors.Position, Database.AmmoVendors.id);
                        Vendor.BuyItem(ItemsManager.GetNameById(Arrow), 2000 / 200);
                        Main.Logger("We bought " + 2000 + " of  Arrows with id " + Arrow);
                        Thread.Sleep(Usefuls.LatencyReal * Usefuls.Latency);
                    }
                }
                return;
            }
        }

        public static void SetBuy()
        {
            if(Helpers.RangedWeaponType == "Bows")
            {
                foreach (int level in ArrowDictionary.Keys)
                {
                    if (level <= Me.Level)
                    {
                        //Main.Logger($"Selected Arrow Level {level}");
                        Arrow = ArrowDictionary[level];
                        string AName = ItemsManager.GetNameById(ArrowDictionary[level]);
                        if(!wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(AName))
                            {
                                wManager.wManagerSetting.CurrentSetting.DoNotSellList.Add(AName);
                            }
                        break;
                    }
                }
            }
        if (Helpers.RangedWeaponType == "Guns")
        {
            foreach (int level in BulletsDictionary.Keys)
            {
                if (level <= Me.Level)
                {
                    //Main.Logger($"Selected Arrow Level {level}");
                    Bullet = BulletsDictionary[level];
                    string BName = ItemsManager.GetNameById(BulletsDictionary[level]);
                    if (!wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(BName))
                    {
                        wManager.wManagerSetting.CurrentSetting.DoNotSellList.Add(BName);
                    }
                    break;
                }
            }
        }

    }
    }
