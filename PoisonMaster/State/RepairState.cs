using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class RepairState : State
{
    public override string DisplayName => "Repair Run";

    private DatabaseNPC repairVendor;
    private Timer stateTimer = new Timer();

    public override bool NeedToRun
    {
        get
        {
            if (!stateTimer.IsReady
                || !PluginSettings.CurrentSetting.AutoRepair
                || ObjectManager.Me.GetDurabilityPercent > 35)
                return false;

            stateTimer = new Timer(5000);

            // TODO case when the user doesn't have enough money to repair

            repairVendor = Database.GetRepairVendor();
            if (repairVendor == null)
            {
                Main.Logger("Couldn't find repair vendor");
                return false;
            }
            return true;
        }
    }

    public override void Run()
    {
        List<WoWItem> bagItems = Bag.GetBagItem();

        if (ObjectManager.Me.Position.DistanceTo(repairVendor.Position) >= 6)
        {
            Main.Logger("Running to Repair");
            Main.Logger("Nearest Repair from player:\n" + "Name: " + repairVendor.Name + "[" + repairVendor.Id + "]\nPosition: " + repairVendor.Position.ToStringXml() + "\nDistance: " + repairVendor.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
            GoToTask.ToPosition(repairVendor.Position);
        }
        else
        {
            if (Helpers.NpcIsAbsentOrDead(repairVendor))
                return;

            GoToTask.ToPositionAndIntecractWithNpc(repairVendor.Position, repairVendor.Id, 2);
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
