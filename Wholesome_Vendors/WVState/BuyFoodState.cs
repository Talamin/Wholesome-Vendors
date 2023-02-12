using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WholesomeToolbox;
using WholesomeVendors.Database.Models;
using WholesomeVendors.Managers;
using WholesomeVendors.Utils;
using WholesomeVendors.WVSettings;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.WVState
{
    public class BuyFoodState : State
    {
        public override string DisplayName { get; set; } = "WV Buy Food";

        private readonly IPluginCacheManager _pluginCacheManager;
        private readonly IMemoryDBManager _memoryDBManager;
        private readonly IVendorTimerManager _vendorTimerManager;
        private readonly IBlackListManager _blackListManager;

        private readonly WoWLocalPlayer _me = ObjectManager.Me;
        private ModelCreatureTemplate _foodVendor;
        private ModelItemTemplate _foodToBuy;
        private int _nbFoodsInBags;
        private bool _usingDungeonProduct;

        private int FoodAmountSetting => PluginSettings.CurrentSetting.FoodNbToBuy;
        private int AmountToBuy => FoodAmountSetting - _pluginCacheManager.NbFoodsInBags;

        public BuyFoodState(
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
                    || FoodAmountSetting <= 0
                    || _me.IsOnTaxi
                    || !Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                    return false;

                _foodToBuy = null;
                _foodVendor = null;

                _nbFoodsInBags = _pluginCacheManager.NbFoodsInBags;

                if (_pluginCacheManager.IsInInstance)
                {
                    return false;
                }

                if (_nbFoodsInBags <= FoodAmountSetting / 2 || _usingDungeonProduct)
                {
                    int amountToBuy = AmountToBuy;
                    Dictionary<ModelItemTemplate, ModelCreatureTemplate> potentialFoodVendors = new Dictionary<ModelItemTemplate, ModelCreatureTemplate>();
                    foreach (ModelItemTemplate food in _memoryDBManager.GetAllUsableFoods())
                    {
                        if (_pluginCacheManager.HaveEnoughMoneyFor(amountToBuy, food))
                        {
                            ModelNpcVendor vendor = _memoryDBManager.GetNearestItemVendor(food);
                            if (vendor != null)
                            {
                                potentialFoodVendors.Add(food, vendor.CreatureTemplate);
                            }
                        }
                    }

                    if (potentialFoodVendors.Count > 0)
                    {
                        Vector3 myPos = ObjectManager.Me.Position;
                        var sortedDic = potentialFoodVendors.OrderBy(kvp => myPos.DistanceTo(kvp.Value.Creature.GetSpawnPosition));
                        _foodToBuy = sortedDic.First().Key;
                        _foodVendor = sortedDic.First().Value;

                        if (_nbFoodsInBags <= FoodAmountSetting / 10
                            || _usingDungeonProduct && _nbFoodsInBags < FoodAmountSetting)
                        {
                            DisplayName = $"Buying {amountToBuy} x {_foodToBuy.Name} at vendor {_foodVendor.name}";
                            return true;
                        }

                        if (_nbFoodsInBags <= FoodAmountSetting / 2
                            && ObjectManager.Me.Position.DistanceTo(_foodVendor.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                        {
                            DisplayName = $"Drive-by buying {amountToBuy} x {_foodToBuy.Name} at vendor {_foodVendor.name}";
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public override void Run()
        {
            _pluginCacheManager.SanitizeDNSAndDNMLists();
            Vector3 vendorPosition = _foodVendor.Creature.GetSpawnPosition;

            if (!Helpers.TravelToVendorRange(_vendorTimerManager, _foodVendor, DisplayName)
                || Helpers.NpcIsAbsentOrDead(_blackListManager, _foodVendor))
            {
                return;
            }

            for (int i = 0; i <= 5; i++)
            {
                Logger.Log($"Attempt {i + 1}");
                GoToTask.ToPositionAndIntecractWithNpc(vendorPosition, _foodVendor.entry, i);
                Thread.Sleep(1000);
                WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                if (WTGossip.IsVendorGossipOpen)
                {
                    Helpers.SellItems(_pluginCacheManager);
                    Thread.Sleep(1000);
                    WTGossip.BuyItem(_foodToBuy.Name, AmountToBuy, _foodToBuy.BuyCount);
                    Thread.Sleep(1000);

                    if (_pluginCacheManager.NbFoodsInBags >= FoodAmountSetting)
                    {
                        Helpers.CloseWindow();
                        return;
                    }
                }
                Helpers.CloseWindow();
            }

            Logger.Log($"Failed to buy {_foodToBuy.Name}, blacklisting vendor");
            _blackListManager.AddNPCToBlacklist(_foodVendor.entry);
        }
    }
}