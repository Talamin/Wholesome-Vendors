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
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.WVState
{
    public class BuyBagsState : State
    {
        public override string DisplayName { get; set; } = "WV Buy Bags";

        private readonly IPluginCacheManager _pluginCacheManager;
        private readonly IMemoryDBManager _memoryDBManager;
        private readonly IVendorTimerManager _vendorTimerManager;
        private readonly IBlackListManager _blackListManager;

        private WoWLocalPlayer _me = ObjectManager.Me;
        private ModelCreatureTemplate _bagVendor;
        private ModelItemTemplate _bagToBuy;

        public BuyBagsState(
            IMemoryDBManager memoryDBManager,
            IPluginCacheManager pluginCacheManager,
            IVendorTimerManager vendorTimerManager,
            IBlackListManager blackListManager)
        {
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
                    || !PluginSettings.CurrentSetting.BuyBags
                    || Fight.InFight
                    || _me.IsOnTaxi
                    || !Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                    return false;

                if (_pluginCacheManager.EmptyContainerSlots <= 0)
                {
                    WTSettings.RemoveItemFromDoNotSellAndMailList(_memoryDBManager.GetBags.Select(bag => bag.Name).ToList());
                    return false;
                }

                if (BagInBags() != null)
                {
                    Logger.Log($"Equipping {BagInBags().Name}");
                    WTItem.EquipBag(BagInBags().Name);
                    Thread.Sleep(500);
                    return false;
                }

                _bagVendor = null;
                _bagToBuy = null;

                if (_pluginCacheManager.IsInInstance)
                {
                    return false;
                }

                foreach (ModelItemTemplate bag in _memoryDBManager.GetBags)
                {
                    if (_pluginCacheManager.HaveEnoughMoneyFor(1, bag))
                    {
                        ModelNpcVendor vendor = _memoryDBManager.GetNearestItemVendor(bag);
                        if (vendor != null)
                        {
                            _bagToBuy = bag;
                            _bagVendor = vendor.CreatureTemplate;
                            DisplayName = $"Buying {_bagToBuy.Name} at vendor {_bagVendor.name}";
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public override void Run()
        {
            Vector3 vendorPosition = _bagVendor.Creature.GetSpawnPosition;

            if (!Helpers.TravelToVendorRange(_vendorTimerManager, _bagVendor, DisplayName) 
                || Helpers.NpcIsAbsentOrDead(_blackListManager, _bagVendor))
            {
                return;
            }

            WTSettings.AddItemToDoNotSellAndMailList(new List<string>() { _bagToBuy.Name });

            for (int i = 0; i <= 5; i++)
            {
                Logger.Log($"Attempt {i + 1}");
                GoToTask.ToPositionAndIntecractWithNpc(vendorPosition, _bagVendor.entry, i);
                Thread.Sleep(1000);
                WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                if (WTGossip.IsVendorGossipOpen)
                {
                    Helpers.SellItems(_pluginCacheManager);
                    Thread.Sleep(1000);
                    int nbEmptyContainerSlotsBeforeBuying = _pluginCacheManager.EmptyContainerSlots;
                    WTGossip.BuyItem(_bagToBuy.Name, 1, _bagToBuy.BuyCount);
                    Thread.Sleep(1000);

                    // Check if already equipped by inventory plugin
                    if (nbEmptyContainerSlotsBeforeBuying > _pluginCacheManager.EmptyContainerSlots)
                    {
                        Helpers.CloseWindow();
                        return;
                    }

                    if (ItemsManager.GetItemCountByNameLUA(_bagToBuy.Name) > 0)
                    {
                        Logger.Log($"Equipping {BagInBags().Name}");
                        WTItem.EquipBag(BagInBags().Name);
                        Thread.Sleep(1000);
                        Helpers.CloseWindow();
                        return;
                    }
                }
                Helpers.CloseWindow();
            }

            Logger.Log($"Failed to buy {_bagToBuy.Name}, blacklisting vendor");
            _blackListManager.AddNPCToBlacklist(_bagVendor.entry);
        }

        private ModelItemTemplate BagInBags()
        {
            List<WVItem> items = _pluginCacheManager.BagItems;
            List<ModelItemTemplate> allBags = _memoryDBManager.GetBags;
            foreach (WVItem item in items)
            {
                if (allBags.Exists(ua => ua.Entry == item.Entry))
                {
                    return allBags.Find(ua => ua.Entry == item.Entry);
                }
            }
            return null;
        }
    }
}