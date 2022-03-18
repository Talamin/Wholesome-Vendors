using PoisonMaster;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Vendors.Database;
using Wholesome_Vendors.Database.Models;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class BuyFoodState : State
{
    public override string DisplayName { get; set; } = "WV Buying Food";

    private readonly WoWLocalPlayer Me = ObjectManager.Me;
    private Timer StateTimer = new Timer();
    private ModelNpcVendor FoodVendor;
    private ModelItemTemplate FoodToBuy;
    private int FoodAmountSetting => PluginSettings.CurrentSetting.FoodNbToBuy;
    private int NbFoodsInBags;
    private int AmountToBuy;

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !StateTimer.IsReady
                || Me.Level <= 3
                || FoodAmountSetting <= 0
                || Me.IsOnTaxi)
                return false;

            StateTimer = new Timer(5000);
            FoodToBuy = null;
            FoodVendor = null;

            NbFoodsInBags = GetNbOfFoodInBags();
            AmountToBuy = FoodAmountSetting - NbFoodsInBags;

            if (NbFoodsInBags <= FoodAmountSetting / 2)
            {
                List<ModelItemTemplate> foods = MemoryDB.GetAllUsableFoods();
                Dictionary<ModelItemTemplate, ModelNpcVendor> potentialFoodVendors = new Dictionary<ModelItemTemplate, ModelNpcVendor>();
                foreach (ModelItemTemplate food in foods)
                {
                    if (Helpers.HaveEnoughMoneyFor(AmountToBuy, food))
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
                    FoodToBuy = sortedDic.First().Key;
                    FoodVendor = sortedDic.First().Value;
                    DisplayName = $"Buying {AmountToBuy} x {FoodToBuy.Name} at vendor {FoodVendor.CreatureTemplate.name}";
                    return true;
                }
            }

            return false;
        }
    }

    public override void Run()
    {
        Main.Logger(DisplayName);
        Vector3 vendorPos = FoodVendor.CreatureTemplate.Creature.GetSpawnPosition;

        Helpers.CheckMailboxNearby(FoodVendor.CreatureTemplate);

        if (Me.Position.DistanceTo(vendorPos) >= 10)
            GoToTask.ToPosition(vendorPos);

        if (Me.Position.DistanceTo(vendorPos) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(FoodVendor.CreatureTemplate))
                return;

            ClearObsoleteFoods();
            Helpers.AddItemToDoNotSellList(FoodToBuy.Name);
            Helpers.AddItemToDoNotMailList(FoodToBuy.Name);

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(vendorPos, FoodVendor.entry, i);
                Thread.Sleep(500);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.IsVendorGossipOpen())
                {
                    // Sell first
                    Helpers.SellItems(FoodVendor.CreatureTemplate);

                    Helpers.BuyItem(FoodToBuy.Name, AmountToBuy, FoodToBuy.BuyCount);
                    Helpers.CloseWindow();
                    Thread.Sleep(1000);

                    if (GetNbOfFoodInBags() >= FoodAmountSetting)
                        return;
                }
            }
            Main.Logger($"Failed to buy {FoodToBuy.Name}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(FoodVendor.entry);
        }
    }

    private void ClearObsoleteFoods()
    {
        foreach (ModelItemTemplate food in MemoryDB.GetAllFoods)
        {
            Helpers.RemoveItemFromDoNotSellList(food.Name);
        }
    }

    private int GetNbOfFoodInBags()
    {
        int nbFoodsInBags = 0;
        List<WoWItem> items = Bag.GetBagItem();
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

