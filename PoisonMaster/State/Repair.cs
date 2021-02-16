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
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

    public class Repair : State
    {
        public override string DisplayName
        {
            get { return "Repair Run"; }
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
        public static int continentid = Usefuls.ContinentId;

        private CreatureFilter repairVendorFilter = new CreatureFilter
        {
            ContinentId = ContinentId.Kalimdor,

            ExcludeIds = Blacklist.myBlacklist,

            Faction = new Faction(ObjectManager.Me.Faction,
                ReactionType.Friendly),

            NpcFlags = new NpcFlag(Operator.Or,
                new List<UnitNPCFlags>
                {
                UnitNPCFlags.CanRepair
                }),
        };

        //Sell while Repair
        private List<WoWItem> bagItems;
        private List<string> Sellitems = new List<string> { };
        private List<WoWItemQuality> Quality = new List<WoWItemQuality>
        {
            //WoWItemQuality.Common,
            //WoWItemQuality.Poor,
            //WoWItemQuality.Rare,
            //WoWItemQuality.Epic,
            //WoWItemQuality.Uncommon
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
            if(!PluginSettings.CurrentSetting.AllowAutoRepair)
            {
                return false;
            }
            if (ObjectManager.Me.GetDurabilityPercent <= 35)
                {
                    DisplayName = "Repair Run";
                    return true;
                }
                return false;

            }
        }

        // If NeedToRun() == true
        public override void Run()
        {
        getBagItems();
        Database.ChooseDatabaseVendorRepairNPC();
            if (!ObjectManager.Me.InCombat && !ObjectManager.Me.InCombatFlagOnly && !ObjectManager.Me.IsDead)
            {
                if (Database.VendorsRepair != null)
                {
                    if (ObjectManager.Me.Position.DistanceTo(Database.VendorsRepair.Position) >= 6)
                    {
                        Logging.Write("Running to Repair");
                        Logging.Write("Nearest Repair from player:\n" + "Name: " + Database.VendorsRepair?.Name + "[" + Database.VendorsRepair?.id + "]\nPosition: " + Database.VendorsRepair?.Position.ToStringXml() + "\nDistance: " + Database.VendorsRepair?.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
                        GoToTask.ToPosition(Database.VendorsRepair.Position);
                    }
                    if (ObjectManager.Me.Position.DistanceTo(Database.VendorsRepair.Position) <= 5)
                    {
                        if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Name == Database.VendorsRepair.Name) <= 0)
                        {
                            Logging.Write("Looks like " + Database.VendorsRepair + " is not here, we choose another one");
                            if (!Blacklist.myBlacklist.Contains(Database.VendorsRepair.id))
                            {
                                Blacklist.myBlacklist.Add(Database.VendorsRepair.id);
                                Thread.Sleep(50);
                                return;
                            }
                        }
                        GoToTask.ToPositionAndIntecractWithNpc(Database.VendorsRepair.Position, Database.VendorsRepair.id, 2);
                        Thread.Sleep(800 + Usefuls.Latency);
                        Usefuls.SelectGossipOption(1);
                        Thread.Sleep(800 + Usefuls.Latency);
                        Vendor.RepairAllItems();
                        Lua.LuaDoString("MerchantRepairAllButton:Click();", false);
                        Lua.LuaDoString("RepairAllItems();", false);
                        //Sell while  Repairrun
                        foreach (WoWItem item in bagItems)
                        {
                            if (item != null && !wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(item.Name) && !Sellitems.Contains(item.Name))
                            {
                                Sellitems.Add(item.Name);
                            }
                        }
                        if(wManager.wManagerSetting.CurrentSetting.SellGray && !Quality.Contains(WoWItemQuality.Poor))
                        {
                            Quality.Add(WoWItemQuality.Poor);
                        }
                        if (wManager.wManagerSetting.CurrentSetting.SellWhite && !Quality.Contains(WoWItemQuality.Common))
                        {
                            Quality.Add(WoWItemQuality.Common);
                        }
                        if (wManager.wManagerSetting.CurrentSetting.SellGreen && !Quality.Contains(WoWItemQuality.Uncommon))
                        {
                            Quality.Add(WoWItemQuality.Uncommon);
                        }
                        if (wManager.wManagerSetting.CurrentSetting.SellBlue && !Quality.Contains(WoWItemQuality.Rare))
                        {
                            Quality.Add(WoWItemQuality.Rare);
                        }
                        if (wManager.wManagerSetting.CurrentSetting.SellPurple && !Quality.Contains(WoWItemQuality.Epic))
                        {
                            Quality.Add(WoWItemQuality.Epic);
                        }
                    Vendor.SellItems(Sellitems, wManager.wManagerSetting.CurrentSetting.DoNotSellList, Quality);
                        Vendor.RepairAllItems();
                        Thread.Sleep(2000);
                    }
                }
                return;
            }
        }
        private void getBagItems()
        {
            bagItems = Bag.GetBagItem();
        }
    }
