using robotManager.FiniteStateMachine;
using System.Collections.Generic;
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
    public class SellState : State
    {
        public override string DisplayName { get; set; } = "WV Repair and Sell";

        private ModelCreatureTemplate _vendorNpc;
        private Timer _stateTimer = new Timer();
        private int _nbFreeSlotsOnNeedToRun;

        private int MinFreeSlots => PluginSettings.CurrentSetting.MinFreeSlots;

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !Main.IsLaunched
                    || !MemoryDB.IsPopulated
                    || !PluginCache.Initialized
                    || Fight.InFight
                    || !PluginSettings.CurrentSetting.AllowSell
                    || PluginCache.IsInInstance
                    || !_stateTimer.IsReady
                    || ObjectManager.Me.IsOnTaxi)
                    return false;

                _stateTimer = new Timer(5000);
                _nbFreeSlotsOnNeedToRun = PluginCache.NbFreeSlots;

                // Normal
                if (PluginSettings.CurrentSetting.AllowSell && _nbFreeSlotsOnNeedToRun <= MinFreeSlots)
                {
                    _vendorNpc = MemoryDB.GetNearestSeller();
                    if (_vendorNpc != null)
                    {
                        DisplayName = $"Selling at {_vendorNpc.subname} {_vendorNpc.name}";
                        return true;
                    }
                }

                // Drive-by
                if (PluginSettings.CurrentSetting.AllowSell && PluginCache.ItemsToSell.Count > 5)
                {
                    _vendorNpc = MemoryDB.GetNearestSeller();
                    if (_vendorNpc != null
                        && _vendorNpc.Creature.GetSpawnPosition.DistanceTo(ObjectManager.Me.Position) < PluginSettings.CurrentSetting.DriveByDistance)
                    {
                        DisplayName = $"Drive-by sell at {_vendorNpc.subname} {_vendorNpc.name}";
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

            List<WoWItem> bagItems = PluginCache.BagItems;

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
                        Helpers.SellItems();
                        Thread.Sleep(1000);
                        if (PluginCache.NbFreeSlots > _nbFreeSlotsOnNeedToRun)
                        {
                            Helpers.CloseWindow();
                            return;
                        }
                    }
                    Helpers.CloseWindow();
                }

                Main.Logger($"Failed to sell, blacklisting {_vendorNpc.name}");
                NPCBlackList.AddNPCToBlacklist(_vendorNpc.entry);
            }
        }
    }
}