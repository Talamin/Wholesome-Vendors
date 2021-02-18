using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class RepairState : State
{
    public override string DisplayName => "Repair and Sell";

    private DatabaseNPC repairVendor;
    private Timer stateTimer = new Timer();

    public override bool NeedToRun
    {
        get
        {
            if (!stateTimer.IsReady)
                return false;

            stateTimer = new Timer(5000);

            if (PluginSettings.CurrentSetting.AutoRepair && ObjectManager.Me.GetDurabilityPercent < 35
                || PluginSettings.CurrentSetting.AllowAutoSell && Bag.GetContainerNumFreeSlotsByType(BagType.Unspecified) <= 3)
            {
                repairVendor = Database.GetRepairVendor();
                if (repairVendor == null)
                {
                    Main.Logger("Couldn't find repair vendor");
                    return false;
                }
                return true;
            }
            return false;
        }
    }

    public override void Run()
    {
        List<WoWItem> bagItems = Bag.GetBagItem();

        if (ObjectManager.Me.Position.DistanceTo(repairVendor.Position) >= 6)
        {
            Main.Logger("Nearest Repair from player:\n" + "Name: " + repairVendor.Name + "[" + repairVendor.Id + "]\nPosition: " + repairVendor.Position.ToStringXml() + "\nDistance: " + repairVendor.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
            GoToTask.ToPosition(repairVendor.Position);
        }

        if (ObjectManager.Me.Position.DistanceTo(repairVendor.Position) < 6)
        {
            if (Helpers.NpcIsAbsentOrDead(repairVendor))
                return;

            int nbItemsInBags = bagItems.Count;

            // Sell first
            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(repairVendor.Position, repairVendor.Id, i);
                List<string> listItemsToSell = new List<string>();
                foreach (WoWItem item in bagItems)
                {
                    if (item != null && !wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(item.Name))
                        listItemsToSell.Add(item.Name);
                }

                Vendor.SellItems(listItemsToSell, wManager.wManagerSetting.CurrentSetting.DoNotSellList, Helpers.GetListQualityToSell());
                if (Bag.GetBagItem().Count < nbItemsInBags)
                    break;
            }

            // Then repair
            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(repairVendor.Position, repairVendor.Id, i);
                Vendor.RepairAllItems();
                Lua.LuaDoString("MerchantRepairAllButton:Click();", false);
                Lua.LuaDoString("RepairAllItems();", false);
                Helpers.CloseWindow();
                Thread.Sleep(1000);

                if (ObjectManager.Me.GetDurabilityPercent >= 35)
                    break;
            }

            if (ObjectManager.Me.GetDurabilityPercent < 35)
            {
                Main.Logger($"Failed to repair, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(repairVendor.Id);
            }
        }
    }
}
