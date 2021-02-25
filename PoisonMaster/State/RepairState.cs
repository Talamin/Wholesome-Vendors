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
    public override string DisplayName => "WV Repair and Sell";

    private DatabaseNPC repairVendor;
    private Timer stateTimer = new Timer();
    private int minDurability = 35;
    private int maxFreeSlots = 3;

    public override bool NeedToRun
    {
        get
        {
            if (!stateTimer.IsReady
                || ObjectManager.Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);

            if (PluginSettings.CurrentSetting.AutoRepair && ObjectManager.Me.GetDurabilityPercent < minDurability)
            {
                repairVendor = Database.GetRepairVendor();
                return repairVendor != null;
            }
            else if (PluginSettings.CurrentSetting.AllowAutoSell && Bag.GetContainerNumFreeSlotsByType(BagType.Unspecified) <= maxFreeSlots)
            {
                repairVendor = Database.GetSellVendor();
                return repairVendor != null;
            }

            return false;
        }
    }

    public override void Run()
    {
        List<WoWItem> bagItems = Bag.GetBagItem();

        if (ObjectManager.Me.Position.DistanceTo(repairVendor.Position) >= 10)
        {
            Main.Logger("Nearest Repair from player:\n" + "Name: " + repairVendor.Name + "[" + repairVendor.Id + "]\nPosition: " + repairVendor.Position.ToStringXml() + "\nDistance: " + repairVendor.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
            GoToTask.ToPosition(repairVendor.Position);
        }

        if (ObjectManager.Me.Position.DistanceTo(repairVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(repairVendor))
                return;

            // Sell first
            Helpers.SellItems(repairVendor);

            // Then repair
            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(repairVendor.Position, repairVendor.Id, i);
                Vendor.RepairAllItems();
                Lua.LuaDoString("MerchantRepairAllButton:Click();", false);
                Lua.LuaDoString("RepairAllItems();", false);
                Helpers.CloseWindow();
                Thread.Sleep(1000);

                if (ObjectManager.Me.GetDurabilityPercent >= minDurability)
                    break;
            }

            if (ObjectManager.Me.GetDurabilityPercent < minDurability || Bag.GetContainerNumFreeSlotsByType(BagType.Unspecified) <= maxFreeSlots)
            {
                Main.Logger($"Failed to sell/repair, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(repairVendor.Id);
            }
        }
    }
}
