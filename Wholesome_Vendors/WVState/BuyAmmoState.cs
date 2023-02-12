using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
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
    public class BuyAmmoState : State
    {
        public override string DisplayName { get; set; } = "WV Buy Ammunition";

        private readonly IPluginCacheManager _pluginCacheManager;
        private readonly IMemoryDBManager _memoryDBManager;
        private readonly IVendorTimerManager _vendorTimerManager;
        private readonly IBlackListManager _blackListManager;

        private WoWLocalPlayer _me = ObjectManager.Me;
        private ModelCreatureTemplate _ammoVendor;
        private ModelItemTemplate _ammoToBuy;
        private int _nbAmmoInBags;
        private bool _usingDungeonProduct;

        private int AmmoAmountSetting => PluginSettings.CurrentSetting.AmmoAmount;
        private int AmountToBuy => AmmoAmountSetting - _pluginCacheManager.NbAmmosInBags;

        public BuyAmmoState(
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
                    || _pluginCacheManager.InLoadingScreen
                    || Fight.InFight
                    || _pluginCacheManager.RangedWeaponType == null
                    || PluginSettings.CurrentSetting.AmmoAmount <= 0
                    || _me.IsOnTaxi
                    || !Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                {
                    return false;
                }

                _ammoVendor = null;
                _ammoToBuy = null;

                _nbAmmoInBags = _pluginCacheManager.NbAmmosInBags;

                if (_pluginCacheManager.IsInInstance)
                {
                    return false;
                }

                if (_nbAmmoInBags <= AmmoAmountSetting / 2 || _usingDungeonProduct)
                {
                    int amountToBuy = AmountToBuy;
                    foreach (ModelItemTemplate ammo in _pluginCacheManager.UsableAmmos)
                    {
                        if (_pluginCacheManager.HaveEnoughMoneyFor(amountToBuy, ammo))
                        {
                            ModelNpcVendor vendor = _memoryDBManager.GetNearestItemVendor(ammo);
                            if (vendor != null)
                            {
                                _ammoToBuy = ammo;
                                _ammoVendor = vendor.CreatureTemplate;
                                // Normal
                                if (_nbAmmoInBags <= AmmoAmountSetting / 10
                                    || _usingDungeonProduct && _nbAmmoInBags < AmmoAmountSetting)
                                {
                                    DisplayName = $"Buying {amountToBuy} x {_ammoToBuy.Name} at vendor {_ammoVendor.name}";
                                    return true;
                                }
                                // Drive-by
                                if (_nbAmmoInBags <= AmmoAmountSetting / 2
                                    && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                                {
                                    DisplayName = $"Drive-by buying {amountToBuy} x {_ammoToBuy.Name} at vendor {_ammoVendor.name}";
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
        }

        public override void Run()
        {
            _pluginCacheManager.SanitizeDNSAndDNMLists();
            Vector3 vendorPosition = _ammoVendor.Creature.GetSpawnPosition;

            if (!Helpers.TravelToVendorRange(_vendorTimerManager, _ammoVendor, DisplayName) 
                || Helpers.NpcIsAbsentOrDead(_blackListManager, _ammoVendor))
            {
                return;
            }

            for (int i = 0; i <= 5; i++)
            {
                Logger.Log($"Attempt {i + 1}");
                GoToTask.ToPositionAndIntecractWithNpc(vendorPosition, _ammoVendor.entry, i);
                Thread.Sleep(1000);
                WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                if (WTGossip.IsVendorGossipOpen)
                {
                    Helpers.SellItems(_pluginCacheManager);
                    Thread.Sleep(1000);
                    WTGossip.BuyItem(_ammoToBuy.Name, AmountToBuy, _ammoToBuy.BuyCount);
                    Thread.Sleep(1000);

                    if (_pluginCacheManager.NbAmmosInBags >= AmmoAmountSetting)
                    {
                        Helpers.CloseWindow();
                        return;
                    }
                }
                Helpers.CloseWindow();
            }

            Logger.Log($"Failed to buy {_ammoToBuy.Name}, blacklisting vendor");
            _blackListManager.AddNPCToBlacklist(_ammoVendor.entry);
        }
    }
}
