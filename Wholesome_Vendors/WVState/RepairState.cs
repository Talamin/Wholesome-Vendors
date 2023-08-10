using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Threading;
using WholesomeToolbox;
using WholesomeVendors.Database.Models;
using WholesomeVendors.Managers;
using WholesomeVendors.Utils;
using WholesomeVendors.WVSettings;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.WVState
{
    public class RepairState : State
    {
        public override string DisplayName { get; set; } = "WV Repair";

        private readonly IPluginCacheManager _pluginCacheManager;
        private readonly IMemoryDBManager _memoryDBManager;
        private readonly IVendorTimerManager _vendorTimerManager;
        private readonly IBlackListManager _blackListManager;

        private ModelCreatureTemplate _vendorNpc;
        private double _durabilityOnNeedToRun;
        private bool _usingDungeonProduct;

        public RepairState(
            IMemoryDBManager memoryDBManager,
            IPluginCacheManager pluginCacheManager,
            IVendorTimerManager vendorTimerManager,
            IBlackListManager blackListManager)
        {
            _usingDungeonProduct = Helpers.UsingDungeonProduct();
            _memoryDBManager = memoryDBManager;
            _pluginCacheManager = pluginCacheManager;
            _vendorTimerManager = vendorTimerManager;
            _blackListManager = blackListManager;
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Main.IsLaunched
                    || !PluginSettings.CurrentSetting.AllowRepair
                    || _pluginCacheManager.InLoadingScreen
                    || !_pluginCacheManager.BagsRecorded
                    || Fight.InFight
                    || _pluginCacheManager.IsInInstance
                    || ObjectManager.Me.IsOnTaxi
                    || !Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                    return false;

                _durabilityOnNeedToRun = ObjectManager.Me.GetDurabilityPercent;

                // Normal
                if (_durabilityOnNeedToRun < 35
                    || _usingDungeonProduct && _durabilityOnNeedToRun < 90)
                {
                    _vendorNpc = _memoryDBManager.GetNearestRepairer();
                    if (_vendorNpc != null)
                    {
                        DisplayName = $"Repairing at {_vendorNpc.subname} {_vendorNpc.name}";
                        return true;
                    }
                }

                // Drive-by
                if (_durabilityOnNeedToRun < 70)
                {
                    _vendorNpc = _memoryDBManager.GetNearestRepairer();
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
            _pluginCacheManager.SanitizeDNSAndDNMLists();
            Vector3 vendorPosition = _vendorNpc.Creature.GetSpawnPosition;

            if (!Helpers.TravelToVendorRange(_vendorTimerManager, _vendorNpc, DisplayName) 
                || Helpers.NpcIsAbsentOrDead(_blackListManager, _vendorNpc))
            {
                return;
            }

            for (int i = 0; i <= 5; i++)
            {
                Logger.Log($"Attempt {i + 1}");
                GoToTask.ToPositionAndIntecractWithNpc(vendorPosition, _vendorNpc.entry, i);
                Thread.Sleep(1000);
                WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                if (WTGossip.IsVendorGossipOpen)
                {
                    Helpers.SellItems(_pluginCacheManager);
                    Thread.Sleep(1000);
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

            Logger.Log($"Failed to repair, blacklisting {_vendorNpc.name}");
            _blackListManager.AddNPCToBlacklist(_vendorNpc.entry);
        }
    }
}