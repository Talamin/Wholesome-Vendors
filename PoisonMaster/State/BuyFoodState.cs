using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;
using PoisonMaster;
using static PluginSettings;

public class BuyFoodState : State
{
    public override string DisplayName => "WV Buying Food";

    private static readonly Dictionary<int, HashSet<int>> FoodDictionary = new Dictionary<int, HashSet<int>>
    {
        { 75, new HashSet<int>{ 35953 } }, // Mead Basted Caribouhl au
        { 65, new HashSet<int>{ 29451, 29449, 29450, 29448, 29452, 29453, 33454, 33443 } }, // Clefthoof Ribs
        { 55, new HashSet<int>{ 27854, 27855, 27856, 27857, 27858, 27859 } }, // Smoked Talbuk Venison 
        { 45, new HashSet<int>{ 8952, 8950, 8932, 8948, 8957} }, // Roasted Quail
        { 35, new HashSet<int>{ 4599, 4601, 3927, 4608, 6887 } }, // Cured Ham Steak
        { 25, new HashSet<int>{ 3771, 4544, 1707, 4607, 4594, 4539 } }, // Wild Hog Shank
        { 15, new HashSet<int>{ 3770, 4542, 422, 4606, 4593, 4538 } }, // Mutton Chop
        { 10, new HashSet<int>{ 2287, 4541, 414, 4605, 4592, 4538} }, // Haunch of Meat
        { 0, new HashSet<int>{ 117, 4540, 2070, 4604, 787 , 4536} }, // Haunch of Meat
    };

    private readonly WoWLocalPlayer Me = ObjectManager.Me;
    private Timer StateTimer = new Timer();
    private DatabaseNPC FoodVendor;
    private int FoodIdToBuy;
    private string FoodNameToBuy;
    private int FoodAmountToBuy => CurrentSetting.FoodAmount;

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !StateTimer.IsReady
                || Me.Level <= 3
                || FoodAmountToBuy <= 0
                || Me.IsOnTaxi)
                return false;

            StateTimer = new Timer(5000);

            SetFoodAndVendor();

            if (FoodIdToBuy > 0
                && GetNbOfFoodInBags() <= 3)
                return FoodVendor != null;

            return false;
        }
    }

    public override void Run()
    {
        Main.Logger($"Buying {FoodAmountToBuy} x {FoodNameToBuy} [{FoodIdToBuy}] at vendor {FoodVendor.Name}");

        Helpers.CheckMailboxNearby(FoodVendor);

        if (Me.Position.DistanceTo(FoodVendor.Position) >= 10)
            GoToTask.ToPosition(FoodVendor.Position);

        if (Me.Position.DistanceTo(FoodVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(FoodVendor))
                return;

            ClearDoNotSellListFromFoods();
            Helpers.AddItemToDoNotSellList(FoodNameToBuy);
            Helpers.AddItemToDoNotMailList(FoodNameToBuy);

            List<string> allFoodNames = GetPotentialFoodNames();

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(FoodVendor.Position, FoodVendor.Id, i);
                Thread.Sleep(500);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.OpenRecordVendorItems(allFoodNames)) // also checks if vendor window is open
                {
                    // Sell first
                    Helpers.SellItems(FoodVendor);
                    if (!Helpers.HaveEnoughMoneyFor(FoodAmountToBuy, FoodNameToBuy))
                    {
                        Main.Logger("Not enough money. Item prices sold by this vendor are now recorded.");
                        Helpers.CloseWindow();
                        return;
                    }
                    Helpers.BuyItem(FoodNameToBuy, FoodAmountToBuy, 5);
                    Helpers.CloseWindow();
                    Thread.Sleep(1000);
                    if (ItemsManager.GetItemCountById((uint)FoodIdToBuy) >= FoodAmountToBuy)
                        return;
                }
            }
            Main.Logger($"Failed to buy {FoodNameToBuy}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(FoodVendor.Id);
        }
    }

    private List<string> GetPotentialFoodNames()
    {
        List<string> allFoods = new List<string>();
        foreach (KeyValuePair<int, HashSet<int>> foods in FoodDictionary)
            foreach (int foodToAdd in foods.Value)
                allFoods.Add(Database.GetItemName(foodToAdd));
        return allFoods;
    }

    private void ClearDoNotSellListFromFoods()
    {
        foreach (KeyValuePair<int, HashSet<int>> foodList in FoodDictionary)
            foreach (int food in foodList.Value)
                Helpers.RemoveItemFromDoNotSellList(Database.GetItemName(food));
    }

    private void SetFoodAndVendor()
    {
        FoodIdToBuy = 0;
        FoodVendor = null;

        foreach (KeyValuePair<int, HashSet<int>> foodEntry in FoodDictionary.Where(f => f.Key <= Me.Level))
        {
            foreach (int foodId in foodEntry.Value)
            {
                DatabaseNPC vendorWithThisFood = Database.GetFoodVendor(new HashSet<int>() { foodId });

                // Skip to lower tier food if we don't have enough money for this tier
                if (!Helpers.HaveEnoughMoneyFor(FoodAmountToBuy, Database.GetItemName(foodId)))
                    break;

                if (vendorWithThisFood != null)
                {
                    if (FoodVendor == null || vendorWithThisFood.Position.DistanceTo2D(Me.Position) < FoodVendor.Position.DistanceTo2D(Me.Position))
                    {
                        FoodIdToBuy = foodId;
                        FoodNameToBuy = Database.GetItemName(foodId);
                        FoodVendor = vendorWithThisFood;
                    }
                }
            }
            if (FoodVendor != null)
                break;
        }

        if (FoodVendor == null)
        {
            Main.Logger($"Couldn't find any food vendor");
            return;
        }

        List<int> listFoodInBags = GetListFoodFromBags();
        if (listFoodInBags.Count > 0)
        {
            string foodNameToSet = Database.GetItemName(listFoodInBags.Last());
            if (foodNameToSet != wManagerSetting.CurrentSetting.FoodName)
            {
                Main.Logger($"Setting food to {foodNameToSet}");
                wManagerSetting.CurrentSetting.FoodName = foodNameToSet;
                wManagerSetting.CurrentSetting.Save();
            }
        }
    }

    private List<int> GetListUsableFood() // From best to worst
    {
        List<int> listFood = new List<int>();
        foreach (KeyValuePair<int, HashSet<int>> foodSet in FoodDictionary)
        {
            if (foodSet.Key <= Me.Level)
                foreach (int food in foodSet.Value)
                    listFood.Add(food);
        }
        return listFood;
    }

    private int GetNbOfFoodInBags()
    {
        int nbFoodsInBags = 0;
        GetListFoodFromBags().ForEach(f => nbFoodsInBags += ItemsManager.GetItemCountById((uint)f));
        return nbFoodsInBags;
    }

    private List<int> GetListFoodFromBags() // From best to worst
    {
        List<int> foodInBags = new List<int>();
        foreach (int food in GetListUsableFood())
            if (ItemsManager.GetItemCountById((uint)food) > 0)
                foodInBags.Add(food);

        return foodInBags;
    }
}

