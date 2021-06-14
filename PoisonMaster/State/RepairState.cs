using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class RepairState : State
{
    public override string DisplayName => "WV Repair and Sell";

    private DatabaseNPC RepairVendor;
    private Timer stateTimer = new Timer();
    private int MinDurability = 35;
    private int MinFreeSlots => wManagerSetting.CurrentSetting.MinFreeBagSlotsToGoToTown;

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !stateTimer.IsReady
                || ObjectManager.Me.IsOnTaxi)
                return false;

            if (Usefuls.ContinentId != 0 || Usefuls.ContinentId != 1 || Usefuls.ContinentId != 530 || Usefuls.ContinentId != 571)
                return false;

            stateTimer = new Timer(5000);

            if (PluginSettings.CurrentSetting.AutoRepair && ObjectManager.Me.GetDurabilityPercent < MinDurability)
            {
                RepairVendor = Database.GetRepairVendor();
                return RepairVendor != null;
            }
            else if (PluginSettings.CurrentSetting.AllowAutoSell && Bag.GetContainerNumFreeSlotsByType(BagType.Unspecified) <= MinFreeSlots)
            {
                RepairVendor = Database.GetSellVendor();
                return RepairVendor != null;
            }

            return false;
        }
    }

    public override void Run()
    {
        Main.Logger($"Going to sell/repair vendor {RepairVendor.Name}");

        Helpers.CheckMailboxNearby(RepairVendor);

        List<WoWItem> bagItems = Bag.GetBagItem();

        if (ObjectManager.Me.Position.DistanceTo(RepairVendor.Position) >= 10)
            GoToTask.ToPosition(RepairVendor.Position);

        if (ObjectManager.Me.Position.DistanceTo(RepairVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(RepairVendor))
                return;

            // Sell first
            Helpers.SellItems(RepairVendor);

            // Then repair
            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(RepairVendor.Position, RepairVendor.Id, i);
                Vendor.RepairAllItems();
                Lua.LuaDoString("MerchantRepairAllButton:Click();", false);
                Lua.LuaDoString("RepairAllItems();", false);
                Helpers.CloseWindow();
                Thread.Sleep(1000);

                if (ObjectManager.Me.GetDurabilityPercent >= MinDurability)
                    break;
            }

            if (ObjectManager.Me.GetDurabilityPercent < MinDurability || Bag.GetContainerNumFreeSlotsByType(BagType.Unspecified) <= MinFreeSlots)
            {
                Main.Logger($"Failed to sell/repair, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(RepairVendor.Id);
            }
        }
    }
}
