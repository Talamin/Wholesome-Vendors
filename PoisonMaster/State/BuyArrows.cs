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

    public class BuyArrows : State
    {
        public override string DisplayName
        {
            get { return "Buying Arrows"; }
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



        private CreatureFilter AmmoVendor = new CreatureFilter
        {
            ContinentId = ContinentId.Kalimdor,

            ExcludeIds = Blacklist.myBlacklist,

            Faction = new Faction(ObjectManager.Me.Faction,
                ReactionType.Friendly),

            NpcFlags = new NpcFlag(Operator.Or,
                new List<UnitNPCFlags>
                {
                UnitNPCFlags.SellsAmmo
                }),

            Range = new Range(ObjectManager.Me.Position)          //is needed for auto-order function by .Get() method
        };

        Npc npcHordeArrows = new Npc
        {
            //Kalimdor - Ogrimmar - Arrow Dealer
            Entry = 3313,
            Position = new Vector3(1523.102, -4355.529, 19.09188),
            Type = Npc.NpcType.Vendor
        };



        // If this method return true, wrobot launch method Run(), if return false wrobot go to next state in FSM
        public override bool NeedToRun
        {
            get
            {
                if (Me.IsDead)
                {
                    return false;
                }
                if (!Me.IsAlive)
                {
                    return false;
                }
                if (Me.InCombatFlagOnly)
                {
                    return false;
                }
                if (ObjectManager.Me.WowClass == WoWClass.Hunter)
                {
                    SetBuy();

                    if (ItemsManager.GetItemCountById(Arrow) == 0 && ObjectManager.Me.WowClass == WoWClass.Hunter && ItemsManager.GetItemCountById(Arrow) <= 200)
                    {
                        Logging.Write("We have to Buy Arrows");
                        DisplayName = "Buying Arrows";
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
            var ammoVendor = DbCreature.GetNearest(AmmoVendor, ObjectManager.Me.Position, 2500);
            while (!ObjectManager.Me.InCombat && !ObjectManager.Me.InCombatFlagOnly && !ObjectManager.Me.IsDead)
            {
                if (ammoVendor != null)
                {
                    while (ObjectManager.Me.Position.DistanceTo(ammoVendor.Position) >= 6)
                    {
                        if (MovementManager.InMovement)
                        {
                            break;
                        }
                        if (ObjectManager.Me.InCombatFlagOnly)
                        {
                            Logging.Write("Being  Attacked");
                            break;
                        }
                        Logging.Write("Running to Buy Arrows");
                        Logging.Write("Nearest AmmunitionVendor from player:\n" + "Name: " + ammoVendor?.Name + "[" + ammoVendor?.id + "]\nPosition: " + ammoVendor?.Position.ToStringXml() + "\nDistance: " + ammoVendor?.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
                        MovementManager.Go(PathFinder.FindPath(ammoVendor.Position));
                        break;
                    }
                    if (ObjectManager.Me.Position.DistanceTo(ammoVendor.Position) <= 5 && ItemsManager.GetItemCountById(Arrow) < 2000)
                    {
                        if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Name == ammoVendor.Name) <= 0)
                        {
                            Logging.Write("Looks like " + ammoVendor + " is not here, we choose another one");
                            if (!Blacklist.myBlacklist.Contains(ammoVendor.id))
                            {
                                Blacklist.myBlacklist.Add(ammoVendor.id);
                                Thread.Sleep(50);
                                //if (!Blacklist.ReadSpecificTxt(ammoVendor.id.ToString()))
                                //{
                                //    Blacklist.WriteTxt(ammoVendor.id.ToString());
                                //}
                                return;
                            }
                        }
                        Logging.Write("No Arrows found, time to buy some! " + Arrow);
                        GoToTask.ToPositionAndIntecractWithNpc(ammoVendor.Position, ammoVendor.id);
                        Vendor.BuyItem(ItemsManager.GetNameById(Arrow), 2000 / 200);
                        Thread.Sleep(Usefuls.LatencyReal * Usefuls.Latency);
                    }
                }
                return;
            }
        }

        public static void SetBuy()
        {
            foreach (int level in ArrowDictionary.Keys)
            {
                if (level <= Me.Level)
                {
                    //Logging.WriteDebug($"Selected Arrow Level {level}");
                    Arrow = ArrowDictionary[level];
                    string AName = ItemsManager.GetNameById(ArrowDictionary[level]);
                    wManager.wManagerSetting.CurrentSetting.DoNotSellList.Add(AName);
                    break;
                }
            }
        }

    }
