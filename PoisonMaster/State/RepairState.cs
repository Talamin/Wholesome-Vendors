using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Vendors.Database;
using Wholesome_Vendors.Database.Models;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class RepairState : State
{
    public override string DisplayName { get; set; } = "WV Repair";

    private ModelCreatureTemplate VendorNpc;
    private Timer stateTimer = new Timer();
    private int MinDurability = 35;
    private double durabilityOnNeedToRun;

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !MemoryDB.IsPopulated
                || !PluginCache.Initialized
                || !PluginSettings.CurrentSetting.AllowRepair
                || PluginCache.IsInInstance
                || !stateTimer.IsReady
                || ObjectManager.Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);

            durabilityOnNeedToRun = ObjectManager.Me.GetDurabilityPercent;

            // Normal
            if (durabilityOnNeedToRun < MinDurability)
            {
                VendorNpc = MemoryDB.GetNearestRepairer();
                if (VendorNpc != null)
                {
                    DisplayName = $"Repairing at {VendorNpc.subname} {VendorNpc.name}";
                    return true;
                }
            }

            // Drive-by
            if (PluginSettings.CurrentSetting.AllowRepair && durabilityOnNeedToRun < 70)
            {
                VendorNpc = MemoryDB.GetNearestRepairer();
                if (VendorNpc != null
                    && VendorNpc.Creature.GetSpawnPosition.DistanceTo(ObjectManager.Me.Position) < PluginSettings.CurrentSetting.DriveByDistance)
                {
                    DisplayName = $"Drive-by repair at {VendorNpc.subname} {VendorNpc.name}";
                    return true;
                }
            }

            return false;
        }
    }

    public override void Run()
    {
        Main.Logger(DisplayName);

        Helpers.CheckMailboxNearby(VendorNpc);

        if (ObjectManager.Me.Position.DistanceTo(VendorNpc.Creature.GetSpawnPosition) >= 10)
            GoToTask.ToPosition(VendorNpc.Creature.GetSpawnPosition);

        if (ObjectManager.Me.Position.DistanceTo(VendorNpc.Creature.GetSpawnPosition) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(VendorNpc))
                return;

            for (int i = 0; i <= 5; i++)
            {
                Main.Logger($"Attempt {i + 1}");
                GoToTask.ToPositionAndIntecractWithNpc(VendorNpc.Creature.GetSpawnPosition, VendorNpc.entry, i);
                Thread.Sleep(1000);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.IsVendorGossipOpen())
                {
                    Vendor.RepairAllItems();
                    Thread.Sleep(1000);
                    Lua.LuaDoString("MerchantRepairAllButton:Click();", false);
                    Lua.LuaDoString("RepairAllItems();", false);
                    Thread.Sleep(1000);
                    if (ObjectManager.Me.GetDurabilityPercent > durabilityOnNeedToRun)
                    {
                        Helpers.CloseWindow();
                        return;
                    }
                }
                Helpers.CloseWindow();
            }

            if (ObjectManager.Me.GetDurabilityPercent < MinDurability)
            {
                Main.Logger($"Failed to repair, blacklisting {VendorNpc.name}");
                NPCBlackList.AddNPCToBlacklist(VendorNpc.entry);
            }
        }
    }

}
