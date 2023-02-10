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
using Timer = robotManager.Helpful.Timer;

namespace WholesomeVendors.WVState
{
    public class SellState : State
    {
        private ModelCreatureTemplate _vendorNpc;
        private int _nbFreeSlotsOnNeedToRun;
        private bool _usingDungeonProduct;
        private Timer _stateTimer = new Timer(); // avoid triggering too often

        public override string DisplayName { get; set; } = "WV Sell";

        private readonly IPluginCacheManager _pluginCacheManager;
        private readonly IMemoryDBManager _memoryDBManager;
        private readonly IVendorTimerManager _vendorTimerManager;
        private readonly IBlackListManager _blackListManager;
        private int MinFreeSlots => PluginSettings.CurrentSetting.MinFreeSlots;

        public SellState(
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
                if (!PluginSettings.CurrentSetting.AllowSell
                    || !_stateTimer.IsReady
                    || _pluginCacheManager.ItemsToSell.Count <= 0
                    || !Main.IsLaunched
                    || _pluginCacheManager.InLoadingScreen
                    || Fight.InFight
                    || _pluginCacheManager.IsInInstance
                    || ObjectManager.Me.IsOnTaxi
                    || !Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                {
                    return false;
                }

                _nbFreeSlotsOnNeedToRun = _pluginCacheManager.NbFreeSlots;

                // Normal
                if (_nbFreeSlotsOnNeedToRun <= MinFreeSlots
                    || _usingDungeonProduct && _pluginCacheManager.ItemsToSell.Count > 5)
                {
                    _vendorNpc = _memoryDBManager.GetNearestSeller();
                    if (_vendorNpc != null)
                    {
                        DisplayName = $"Selling at {_vendorNpc.subname} {_vendorNpc.name}";
                        return true;
                    }
                }

                // Drive-by
                if (_pluginCacheManager.ItemsToSell.Count > 5)
                {
                    _vendorNpc = _memoryDBManager.GetNearestSeller();
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
                    if (_pluginCacheManager.NbFreeSlots > _nbFreeSlotsOnNeedToRun)
                    {
                        Helpers.CloseWindow();
                        _stateTimer = new Timer(1000 * 60 * 5);
                        return;
                    }
                }
                Helpers.CloseWindow();
            }

            Logger.Log($"Failed to sell, blacklisting {_vendorNpc.name}");
            _blackListManager.AddNPCToBlacklist(_vendorNpc.entry);
        }
    }
}