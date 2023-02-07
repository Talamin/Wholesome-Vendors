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
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.WVState
{
    public class BuyPoisonState : State
    {
        public override string DisplayName { get; set; } = "WV Buy Poison";

        private readonly IPluginCacheManager _pluginCacheManager;
        private readonly IMemoryDBManager _memoryDBManager;
        private readonly IVendorTimerManager _vendorTimerManager;
        private readonly IBlackListManager _blackListManager;

        private WoWLocalPlayer _me = ObjectManager.Me;

        private ModelItemTemplate _poisonToBuy;
        private ModelCreatureTemplate _poisonVendor;
        private int _amountToBuy;
        private bool _usingDungeonProduct;

        public BuyPoisonState(
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
                    || !PluginSettings.CurrentSetting.BuyPoison
                    || ObjectManager.Me.WowClass != WoWClass.Rogue
                    || ObjectManager.Me.Level < 20
                    || _me.IsOnTaxi
                    || !Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                {
                    return false;
                }

                _poisonToBuy = null;
                _poisonVendor = null;
                _amountToBuy = 0;

                if (_pluginCacheManager.IsInInstance)
                {
                    return false;
                }

                // Deadly Poison
                if (_pluginCacheManager.NbDeadlyPoisonsInBags <= 15)
                {
                    _amountToBuy = 20 - _pluginCacheManager.NbDeadlyPoisonsInBags;
                    ModelItemTemplate deadlyP = _memoryDBManager.GetDeadlyPoisons.Find(p => p.RequiredLevel <= ObjectManager.Me.Level);
                    if (deadlyP != null && _pluginCacheManager.HaveEnoughMoneyFor(_amountToBuy, deadlyP))
                    {
                        ModelNpcVendor vendor = _memoryDBManager.GetNearestItemVendor(deadlyP);
                        if (vendor != null)
                        {
                            _poisonToBuy = deadlyP;
                            _poisonVendor = vendor.CreatureTemplate;
                            // Normal
                            if (_pluginCacheManager.NbDeadlyPoisonsInBags <= 1
                                || _usingDungeonProduct && _pluginCacheManager.NbDeadlyPoisonsInBags <= 15)
                            {
                                DisplayName = $"Buying {_amountToBuy} x {_poisonToBuy.Name} at vendor {_poisonVendor.name}";
                                return true;
                            }
                            // Drive-by
                            if (_pluginCacheManager.NbDeadlyPoisonsInBags <= 15
                                && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                            {
                                DisplayName = $"Drive-by buying {_amountToBuy} x {_poisonToBuy.Name} at vendor {_poisonVendor.name}";
                                return true;
                            }
                        }
                    }
                }

                // Instant Poison
                if (_pluginCacheManager.NbInstantPoisonsInBags <= 10)
                {
                    _amountToBuy = 20 - _pluginCacheManager.NbInstantPoisonsInBags;
                    ModelItemTemplate instantP = _memoryDBManager.GetInstantPoisons.Find(p => p.RequiredLevel <= ObjectManager.Me.Level);
                    if (instantP != null && _pluginCacheManager.HaveEnoughMoneyFor(_amountToBuy, instantP))
                    {
                        ModelNpcVendor vendor = _memoryDBManager.GetNearestItemVendor(instantP);
                        if (vendor != null)
                        {
                            // Normal
                            if (_pluginCacheManager.NbInstantPoisonsInBags <= 1
                                || _usingDungeonProduct && _pluginCacheManager.NbInstantPoisonsInBags <= 15)
                            {
                                _poisonToBuy = instantP;
                                _poisonVendor = vendor.CreatureTemplate;
                                DisplayName = $"Buying {_amountToBuy} x {_poisonToBuy.Name} at vendor {_poisonVendor.name}";
                                return true;
                            }
                            // Drive-by
                            if (_pluginCacheManager.NbInstantPoisonsInBags <= 15
                                && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                            {
                                _poisonToBuy = instantP;
                                _poisonVendor = vendor.CreatureTemplate;
                                DisplayName = $"Drive-by buying {_amountToBuy} x {_poisonToBuy.Name} at vendor {_poisonVendor.name}";
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }

        public override void Run()
        {
            Vector3 vendorPosition = _poisonVendor.Creature.GetSpawnPosition;

            if (!Helpers.TravelToVendorRange(_vendorTimerManager, _poisonVendor, DisplayName) 
                || Helpers.NpcIsAbsentOrDead(_blackListManager, _poisonVendor))
            {
                return;
            }

            for (int i = 0; i <= 5; i++)
            {
                Logger.Log($"Attempt {i + 1}");
                GoToTask.ToPositionAndIntecractWithNpc(vendorPosition, _poisonVendor.entry, i);
                Thread.Sleep(1000);
                WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                if (WTGossip.IsVendorGossipOpen)
                {
                    Helpers.SellItems(_pluginCacheManager);
                    Thread.Sleep(1000);
                    WTGossip.BuyItem(_poisonToBuy.Name, _amountToBuy, _poisonToBuy.BuyCount);
                    Thread.Sleep(1000);

                    if (_poisonToBuy.displayid == 13710 && _pluginCacheManager.NbInstantPoisonsInBags >= 20) // Instant
                    {
                        Helpers.CloseWindow();
                        return;
                    }
                    if (_poisonToBuy.displayid == 13707 && _pluginCacheManager.NbDeadlyPoisonsInBags >= 20) // Deadly
                    {
                        Helpers.CloseWindow();
                        return;
                    }
                }
                Helpers.CloseWindow();
            }

            Logger.Log($"Failed to buy poisons, blacklisting vendor");
            _blackListManager.AddNPCToBlacklist(_poisonVendor.entry);
        }
    }
}