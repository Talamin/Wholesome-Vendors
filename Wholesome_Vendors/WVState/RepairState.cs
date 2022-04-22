using robotManager.FiniteStateMachine;
using System.Threading;
using WholesomeToolbox;
using WholesomeVendors.Blacklist;
using WholesomeVendors.Database;
using WholesomeVendors.Database.Models;
using WholesomeVendors.WVSettings;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

namespace WholesomeVendors.WVState
{
    public class RepairState : State
    {
        public override string DisplayName { get; set; } = "WV Repair";

        private ModelCreatureTemplate _vendorNpc;
        private Timer _stateTimer = new Timer();
        private readonly int MIN_DURABILITY = 35;
        private double _durabilityOnNeedToRun;

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
                    || !_stateTimer.IsReady
                    || ObjectManager.Me.IsOnTaxi)
                    return false;

                _stateTimer = new Timer(5000);

                _durabilityOnNeedToRun = ObjectManager.Me.GetDurabilityPercent;

                // Normal
                if (_durabilityOnNeedToRun < MIN_DURABILITY)
                {
                    _vendorNpc = MemoryDB.GetNearestRepairer();
                    if (_vendorNpc != null)
                    {
                        DisplayName = $"Repairing at {_vendorNpc.subname} {_vendorNpc.name}";
                        return true;
                    }
                }

                // Drive-by
                if (PluginSettings.CurrentSetting.AllowRepair && _durabilityOnNeedToRun < 70)
                {
                    _vendorNpc = MemoryDB.GetNearestRepairer();
                    if (_vendorNpc != null
                        && _vendorNpc.Creature.GetSpawnPosition.DistanceTo(ObjectManager.Me.Position) < PluginSettings.CurrentSetting.DriveByDistance)
                    {
                        DisplayName = $"Drive-by repair at {_vendorNpc.subname} {_vendorNpc.name}";
                        return true;
                    }
                }

                return false;
            }
        }

        public override void Run()
        {
            Main.Logger(DisplayName);

            Helpers.CheckMailboxNearby(_vendorNpc);

            if (ObjectManager.Me.Position.DistanceTo(_vendorNpc.Creature.GetSpawnPosition) >= 10)
                GoToTask.ToPosition(_vendorNpc.Creature.GetSpawnPosition);

            if (ObjectManager.Me.Position.DistanceTo(_vendorNpc.Creature.GetSpawnPosition) < 10)
            {
                if (Helpers.NpcIsAbsentOrDead(_vendorNpc))
                    return;

                for (int i = 0; i <= 5; i++)
                {
                    Main.Logger($"Attempt {i + 1}");
                    GoToTask.ToPositionAndIntecractWithNpc(_vendorNpc.Creature.GetSpawnPosition, _vendorNpc.entry, i);
                    Thread.Sleep(1000);
                    WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                    if (WTGossip.IsVendorGossipOpen)
                    {
                        Vendor.RepairAllItems();
                        Thread.Sleep(1000);
                        WTGossip.RepairAll();
                        Thread.Sleep(1000);
                        if (ObjectManager.Me.GetDurabilityPercent > _durabilityOnNeedToRun)
                        {
                            Helpers.CloseWindow();
                            return;
                        }
                    }
                    Helpers.CloseWindow();
                }

                Main.Logger($"Failed to repair, blacklisting {_vendorNpc.name}");
                NPCBlackList.AddNPCToBlacklist(_vendorNpc.entry);
            }
        }

    }
}