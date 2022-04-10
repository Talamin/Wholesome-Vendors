using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
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
    public class BuyFoodState : State
    {
        public override string DisplayName { get; set; } = "WV Buying Food";

        private readonly WoWLocalPlayer _me = ObjectManager.Me;
        private Timer _stateTimer = new Timer();
        private ModelNpcVendor _foodVendor;
        private ModelItemTemplate _foodToBuy;
        private int _nbFoodsInBags;

        private int FoodAmountSetting => PluginSettings.CurrentSetting.FoodNbToBuy;
        private int AmountToBuy => FoodAmountSetting - GetNbOfFoodInBags();

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !Main.IsLaunched
                    || !_stateTimer.IsReady
                    || !MemoryDB.IsPopulated
                    || PluginCache.IsInInstance
                    || !PluginCache.Initialized
                    || _me.Level <= 3
                    || FoodAmountSetting <= 0
                    || _me.IsOnTaxi)
                    return false;

                _stateTimer = new Timer(5000);
                _foodToBuy = null;
                _foodVendor = null;

                _nbFoodsInBags = GetNbOfFoodInBags();

                if (_nbFoodsInBags <= FoodAmountSetting / 2)
                {
                    int amountToBuy = AmountToBuy;
                    Dictionary<ModelItemTemplate, ModelNpcVendor> potentialFoodVendors = new Dictionary<ModelItemTemplate, ModelNpcVendor>();
                    foreach (ModelItemTemplate food in MemoryDB.GetAllUsableFoods())
                    {
                        if (Helpers.HaveEnoughMoneyFor(amountToBuy, food))
                        {
                            ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(food);
                            if (vendor != null)
                            {
                                potentialFoodVendors.Add(food, vendor);
                            }
                        }
                    }

                    if (potentialFoodVendors.Count > 0)
                    {
                        Vector3 myPos = ObjectManager.Me.Position;
                        var sortedDic = potentialFoodVendors.OrderBy(kvp => myPos.DistanceTo(kvp.Value.CreatureTemplate.Creature.GetSpawnPosition));
                        _foodToBuy = sortedDic.First().Key;
                        _foodVendor = sortedDic.First().Value;

                        if (_nbFoodsInBags <= FoodAmountSetting / 10)
                        {
                            DisplayName = $"Buying {amountToBuy} x {_foodToBuy.Name} at vendor {_foodVendor.CreatureTemplate.name}";
                            return true;
                        }

                        if (_nbFoodsInBags <= FoodAmountSetting / 2
                            && ObjectManager.Me.Position.DistanceTo(_foodVendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                        {
                            DisplayName = $"Drive-by buying {amountToBuy} x {_foodToBuy.Name} at vendor {_foodVendor.CreatureTemplate.name}";
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public override void Run()
        {
            Main.Logger(DisplayName);
            Vector3 vendorPos = _foodVendor.CreatureTemplate.Creature.GetSpawnPosition;

            Helpers.CheckMailboxNearby(_foodVendor.CreatureTemplate);

            if (_me.Position.DistanceTo(vendorPos) >= 10)
                GoToTask.ToPosition(vendorPos);

            if (_me.Position.DistanceTo(vendorPos) < 10)
            {
                if (Helpers.NpcIsAbsentOrDead(_foodVendor.CreatureTemplate))
                    return;

                for (int i = 0; i <= 5; i++)
                {
                    Main.Logger($"Attempt {i + 1}");
                    GoToTask.ToPositionAndIntecractWithNpc(vendorPos, _foodVendor.entry, i);
                    Thread.Sleep(1000);
                    WTLua.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                    if (WTGossip.IsVendorGossipOpen)
                    {
                        Helpers.SellItems();
                        Thread.Sleep(1000);
                        WTGossip.BuyItem(_foodToBuy.Name, AmountToBuy, _foodToBuy.BuyCount);
                        Thread.Sleep(1000);

                        if (GetNbOfFoodInBags() >= FoodAmountSetting)
                        {
                            Helpers.CloseWindow();
                            return;
                        }
                    }
                    Helpers.CloseWindow();
                }
                Main.Logger($"Failed to buy {_foodToBuy.Name}, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(_foodVendor.entry);
            }
        }

        private int GetNbOfFoodInBags()
        {
            int nbFoodsInBags = 0;
            List<WoWItem> items = PluginCache.BagItems;
            List<ModelItemTemplate> allFoods = MemoryDB.GetAllUsableFoods();
            string foodToSet = null;
            foreach (WoWItem item in items)
            {
                if (allFoods.Exists(ua => ua.Entry == item.Entry))
                {
                    nbFoodsInBags += ItemsManager.GetItemCountById((uint)item.Entry);
                    foodToSet = item.Name;
                }
            }

            if (foodToSet != null && wManagerSetting.CurrentSetting.FoodName != foodToSet)
            {
                Main.Logger($"Setting food to {foodToSet}");
                wManagerSetting.CurrentSetting.FoodName = foodToSet;
                wManagerSetting.CurrentSetting.Save();
            }

            return nbFoodsInBags;
        }
    }
}