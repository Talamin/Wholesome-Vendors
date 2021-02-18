using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class SellItemsState : State
{
    public override string DisplayName => "Selling Run";

    public static int continentid = Usefuls.ContinentId;

    private DatabaseNPC sellVendor;
    private Timer stateTimer = new Timer();

    public override bool NeedToRun
    {
        get
        {
            if (!stateTimer.IsReady
                || !PluginSettings.CurrentSetting.AllowAutoSell
                || Bag.GetContainerNumFreeSlots > 3)
                return false;

            stateTimer = new Timer(5000);

            sellVendor = Database.GetSellVendor();
            if (sellVendor == null)
            {
                Main.Logger("Couldn't find sell vendor");
                return false;
            }
            return true;
        }
    }

    public override void Run()
    {
        List<WoWItem> bagItems = Bag.GetBagItem();

        if (ObjectManager.Me.Position.DistanceTo(sellVendor.Position) >= 6)
        {
            Main.Logger("Running to Sell");
            Main.Logger("Nearest Repair from player:\n" + "Name: " + sellVendor.Name + "[" + sellVendor.Id + "]\nPosition: " + sellVendor.Position.ToStringXml() + "\nDistance: " + sellVendor.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
            GoToTask.ToPosition(sellVendor.Position);
        }
        else
        {
            if (Helpers.NpcIsAbsentOrDead(sellVendor))
                return;

            GoToTask.ToPositionAndIntecractWithNpc(sellVendor.Position, sellVendor.Id, 2);
            Thread.Sleep(800 + Usefuls.Latency);
            Usefuls.SelectGossipOption(1);
            Thread.Sleep(800 + Usefuls.Latency);
            Vendor.RepairAllItems();
            Lua.LuaDoString("MerchantRepairAllButton:Click();", false);
            Lua.LuaDoString("RepairAllItems();", false);

            // Sell while  Repairrun
            List<string> listItemsToSell = new List<string>();
            foreach (WoWItem item in bagItems)
            {
                if (item != null && !wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(item.Name))
                {
                    listItemsToSell.Add(item.Name);
                }
            }

            Vendor.SellItems(listItemsToSell, wManager.wManagerSetting.CurrentSetting.DoNotSellList, Helpers.GetListQualityToSell());
            Vendor.RepairAllItems();
            Thread.Sleep(2000);
        }
    }
}
