using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Threading;
using WholesomeToolbox;
using WholesomeVendors.Blacklist;
using WholesomeVendors.Database;
using WholesomeVendors.Database.Models;
using WholesomeVendors.WVSettings;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

namespace WholesomeVendors.WVState
{
    public class BuyDrinkState : State
    {
        public override string DisplayName { get; set; } = "WV Buying Drink";

        private WoWLocalPlayer _me = ObjectManager.Me;
        private Timer _stateTimer = new Timer();
        private ModelNpcVendor _drinkVendor;
        private ModelItemTemplate _drinkToBuy;
        private int _nbDrinksInBag;

        private int DrinkAmountSetting => PluginSettings.CurrentSetting.DrinkNbToBuy;
        private int AmountToBuy => DrinkAmountSetting - GetNbDrinksInBags();

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !Main.IsLaunched
                    || !MemoryDB.IsPopulated
                    || !PluginCache.Initialized
                    || !_stateTimer.IsReady
                    || _me.Level <= 3
                    || DrinkAmountSetting <= 0
                    || _me.IsOnTaxi)
                    return false;

                _stateTimer = new Timer(5000);
                _drinkVendor = null;
                _drinkToBuy = null;

                _nbDrinksInBag = GetNbDrinksInBags();

                if (PluginCache.IsInInstance)
                {
                    return false;
                }

                if (_nbDrinksInBag <= DrinkAmountSetting / 2)
                {
                    int amountToBuy = AmountToBuy;
                    foreach (ModelItemTemplate drink in MemoryDB.GetAllUsableDrinks())
                    {
                        if (Helpers.HaveEnoughMoneyFor(amountToBuy, drink))
                        {
                            ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(drink);
                            if (vendor != null)
                            {
                                _drinkToBuy = drink;
                                _drinkVendor = vendor;
                                // Normal
                                if (_nbDrinksInBag <= DrinkAmountSetting / 10)
                                {
                                    DisplayName = $"Buying {amountToBuy} x {_drinkToBuy.Name} at vendor {_drinkVendor.CreatureTemplate.name}";
                                    return true;
                                }
                                // Drive-by
                                if (_nbDrinksInBag <= DrinkAmountSetting / 2
                                    && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                                {
                                    DisplayName = $"Drive-by buying {amountToBuy} x {_drinkToBuy.Name} at vendor {_drinkVendor.CreatureTemplate.name}";
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
            Main.Logger(DisplayName);
            Vector3 vendorPos = _drinkVendor.CreatureTemplate.Creature.GetSpawnPosition;

            Helpers.CheckMailboxNearby(_drinkVendor.CreatureTemplate);

            if (_me.Position.DistanceTo(vendorPos) >= 10)
                GoToTask.ToPosition(vendorPos);

            if (_me.Position.DistanceTo(vendorPos) < 10)
            {
                if (Helpers.NpcIsAbsentOrDead(_drinkVendor.CreatureTemplate))
                    return;

                for (int i = 0; i <= 5; i++)
                {
                    Main.Logger($"Attempt {i + 1}");
                    GoToTask.ToPositionAndIntecractWithNpc(vendorPos, _drinkVendor.entry, i);
                    Thread.Sleep(1000);
                    WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                    if (WTGossip.IsVendorGossipOpen)
                    {
                        Helpers.SellItems();
                        Thread.Sleep(1000);
                        WTGossip.BuyItem(_drinkToBuy.Name, AmountToBuy, _drinkToBuy.BuyCount);
                        Thread.Sleep(1000);

                        if (GetNbDrinksInBags() >= DrinkAmountSetting)
                        {
                            Helpers.CloseWindow();
                            return;
                        }
                    }
                    Helpers.CloseWindow();
                }

                Main.Logger($"Failed to buy {_drinkToBuy.Name}, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(_drinkVendor.entry);
            }
        }

        private int GetNbDrinksInBags()
        {
            int nbDrinksInBags = 0;
            List<WoWItem> items = PluginCache.BagItems;
            List<ModelItemTemplate> allDrinks = MemoryDB.GetAllUsableDrinks();
            string drinkToSet = null;
            foreach (WoWItem item in items)
            {
                if (allDrinks.Exists(ua => ua.Entry == item.Entry))
                {
                    nbDrinksInBags += ItemsManager.GetItemCountById((uint)item.Entry);
                    drinkToSet = item.Name;
                }
            }

            if (drinkToSet != null && wManagerSetting.CurrentSetting.DrinkName != drinkToSet)
            {
                Main.Logger($"Setting drink to {drinkToSet}");
                wManagerSetting.CurrentSetting.DrinkName = drinkToSet;
                wManagerSetting.CurrentSetting.Save();
            }

            return nbDrinksInBags;
        }
    }
}