using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using Wholesome_Vendors.Database;
using Wholesome_Vendors.Database.Models;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class SellState : State
{
    public override string DisplayName { get; set; } = "WV Repair and Sell";

    private ModelCreatureTemplate VendorNpc;
    private Timer stateTimer = new Timer();
    private int MinFreeSlots => PluginSettings.CurrentSetting.MinFreeSlots;

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !MemoryDB.IsPopulated
                || !PluginCache.Initialized
                || !PluginSettings.CurrentSetting.AllowSell
                || PluginCache.IsInInstance
                || !stateTimer.IsReady
                || ObjectManager.Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);

            // Normal
            if (PluginSettings.CurrentSetting.AllowSell && PluginCache.NbFreeSlots <= MinFreeSlots)
            {
                VendorNpc = MemoryDB.GetNearestSeller();
                if (VendorNpc != null)
                {
                    DisplayName = $"Selling at {VendorNpc.subname} {VendorNpc.name}";
                    return true;
                }
            }

            // Drive-by
            if (PluginSettings.CurrentSetting.AllowSell && PluginCache.ItemsToSell.Count > 5)
            {
                VendorNpc = MemoryDB.GetNearestSeller();
                if (VendorNpc != null
                    && VendorNpc.Creature.GetSpawnPosition.DistanceTo(ObjectManager.Me.Position) < PluginSettings.CurrentSetting.DriveByDistance)
                {
                    DisplayName = $"Drive-by sell at {VendorNpc.subname} {VendorNpc.name}";
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

        List<WoWItem> bagItems = PluginCache.BagItems;

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
                    Helpers.SellItems(VendorNpc);
                    Thread.Sleep(1000);
                    if (PluginCache.NbFreeSlots > MinFreeSlots)
                    {
                        Helpers.CloseWindow();
                        return;
                    }
                }
                Helpers.CloseWindow();
            }

            if (PluginCache.NbFreeSlots <= MinFreeSlots)
            {
                Main.Logger($"Failed to sell, blacklisting {VendorNpc.name}");
                NPCBlackList.AddNPCToBlacklist(VendorNpc.entry);
            }
        }
    }
}
