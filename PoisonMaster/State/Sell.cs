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

public class SellItems : State
{
    public override string DisplayName
    {
        get { return "Selling Run"; }
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


    //Sell while Repair
    private List<WoWItem> bagItems;
    private List<string> Sellitems = new List<string> { };
    private List<WoWItemQuality> Quality = new List<WoWItemQuality>
    {
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
            if (!PluginSettings.CurrentSetting.AllowAutoSell)
            {
                return false;
            }
            if (Bag.GetContainerNumFreeSlots <=3)
            {
                DisplayName = "Selling Run";
                return true;
            }
            return false;

        }
    }

    // If NeedToRun() == true
    public override void Run()
    {
        getBagItems();
        Database.ChooseDatabaseSellVendorNPC();
        if (!ObjectManager.Me.InCombat && !ObjectManager.Me.InCombatFlagOnly && !ObjectManager.Me.IsDead)
        {
            if (Database.VendorsSell != null)
            {
                if (ObjectManager.Me.Position.DistanceTo(Database.VendorsSell.Position) >= 6)
                {
                    Logging.Write("Running to Sell");
                    Logging.Write("Nearest Repair from player:\n" + "Name: " + Database.VendorsSell?.Name + "[" + Database.VendorsSell?.id + "]\nPosition: " + Database.VendorsSell?.Position.ToStringXml() + "\nDistance: " + Database.VendorsSell?.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
                    GoToTask.ToPosition(Database.VendorsSell.Position);
                }
                if (ObjectManager.Me.Position.DistanceTo(Database.VendorsSell.Position) <= 5)
                {
                    if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Name == Database.VendorsSell.Name) <= 0)
                    {
                        Logging.Write("Looks like " + Database.VendorsSell + " is not here, we choose another one");
                        if (!Blacklist.myBlacklist.Contains(Database.VendorsSell.id))
                        {
                            Blacklist.myBlacklist.Add(Database.VendorsSell.id);
                            Thread.Sleep(50);
                            return;
                        }
                    }
                    GoToTask.ToPositionAndIntecractWithNpc(Database.VendorsSell.Position, Database.VendorsSell.id, 2);
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
                    if (wManager.wManagerSetting.CurrentSetting.SellGray && !Quality.Contains(WoWItemQuality.Poor))
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
