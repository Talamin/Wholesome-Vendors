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
    public class BuyDrinkState : State
    {
        public override string DisplayName { get; set; } = "WV Buy Drink";

        private readonly IPluginCacheManager _pluginCacheManager;
        private readonly IMemoryDBManager _memoryDBManager;
        private readonly IVendorTimerManager _vendorTimerManager;
        private readonly IBlackListManager _blackListManager;

        private WoWLocalPlayer _me = ObjectManager.Me;
        private ModelCreatureTemplate _drinkVendor;
        private ModelItemTemplate _drinkToBuy;
        private int _nbDrinksInBag;
        private bool _usingDungeonProduct;

        private int DrinkAmountSetting => PluginSettings.CurrentSetting.DrinkNbToBuy;
        private int AmountToBuy => DrinkAmountSetting - _pluginCacheManager.NbDrinksInBags;

        public BuyDrinkState(
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
                    || _me.Level <= 3
                    || DrinkAmountSetting <= 0
                    || _me.IsOnTaxi
                    || !Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                    return false;

                _drinkVendor = null;
                _drinkToBuy = null;

                _nbDrinksInBag = _pluginCacheManager.NbDrinksInBags;

                if (_pluginCacheManager.IsInInstance)
                {
                    return false;
                }

                if (_nbDrinksInBag <= DrinkAmountSetting / 2 || _usingDungeonProduct)
                {
                    int amountToBuy = AmountToBuy;
                    foreach (ModelItemTemplate drink in _memoryDBManager.GetAllUsableDrinks())
                    {
                        if (_pluginCacheManager.HaveEnoughMoneyFor(amountToBuy, drink))
                        {
                            ModelNpcVendor vendor = _memoryDBManager.GetNearestItemVendor(drink);
                            if (vendor != null)
                            {
                                _drinkToBuy = drink;
                                _drinkVendor = vendor.CreatureTemplate;
                                // Normal
                                if (_nbDrinksInBag <= DrinkAmountSetting / 10
                                    || _usingDungeonProduct && _nbDrinksInBag < DrinkAmountSetting)
                                {
                                    DisplayName = $"Buying {amountToBuy} x {_drinkToBuy.Name} at vendor {_drinkVendor.name}";
                                    return true;
                                }
                                // Drive-by
                                if (_nbDrinksInBag <= DrinkAmountSetting / 2
                                    && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                                {
                                    DisplayName = $"Drive-by buying {amountToBuy} x {_drinkToBuy.Name} at vendor {_drinkVendor.name}";
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
            Vector3 vendorPosition = _drinkVendor.Creature.GetSpawnPosition;

            if (!Helpers.TravelToVendorRange(_vendorTimerManager, _drinkVendor, DisplayName) 
                || Helpers.NpcIsAbsentOrDead(_blackListManager, _drinkVendor))
            {
                return;
            }

            for (int i = 0; i <= 5; i++)
            {
                Logger.Log($"Attempt {i + 1}");
                GoToTask.ToPositionAndIntecractWithNpc(vendorPosition, _drinkVendor.entry, i);
                Thread.Sleep(1000);
                WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                if (WTGossip.IsVendorGossipOpen)
                {
                    Helpers.SellItems(_pluginCacheManager);
                    Thread.Sleep(1000);
                    WTGossip.BuyItem(_drinkToBuy.Name, AmountToBuy, _drinkToBuy.BuyCount);
                    Thread.Sleep(1000);

                    if (_pluginCacheManager.NbDrinksInBags >= DrinkAmountSetting)
                    {
                        Helpers.CloseWindow();
                        return;
                    }
                }
                Helpers.CloseWindow();
            }

            Logger.Log($"Failed to buy {_drinkToBuy.Name}, blacklisting vendor");
            _blackListManager.AddNPCToBlacklist(_drinkVendor.entry);
        }
    }
}