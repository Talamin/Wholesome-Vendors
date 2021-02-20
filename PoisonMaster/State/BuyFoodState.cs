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

public class BuyFoodState : State
{
    public override string DisplayName => "WV Buying Food";

    private static readonly Dictionary<int, HashSet<int>> FoodDictionary = new Dictionary<int, HashSet<int>>
        {
            { 85, new HashSet<int>{ 35953 } },
            { 75, new HashSet<int>{ 35953 } }, // Mead Basted Caribouhl au
            { 65, new HashSet<int>{ 29451, 29449, 29450, 29448, 29452, 29453 } }, // Clefthoof Ribs
            { 61, new HashSet<int>{ 27854, 27855, 27856, 27857, 27858, 27859 } }, // Smoked Talbuk Venison -- make sure this is only used in TBC
            { 45, new HashSet<int>{ 8952, 8950, 8932, 8948, 8957} }, // Roasted Quail
            { 35, new HashSet<int>{ 4599, 4601, 3927, 4608, 6887 } }, // Cured Ham Steak
            { 25, new HashSet<int>{ 3771, 4544, 1707, 4607, 4594, 4539 } }, // Wild Hog Shank
            { 20, new HashSet<int>{ 3770, 4542, 422, 4606, 4593, 4538 } }, // Mutton Chop
            { 10, new HashSet<int>{ 2287, 4541, 414, 4605, 4592, 4538} }, // Haunch of Meat
            { 5, new HashSet<int>{ 117, 4540, 2070, 4604, 787 , 4537} }, // Haunch of Meat
            { 0, new HashSet<int>{ 117, 4540, 2070, 4604, 787 , 4536} }, // Haunch of Meat
        };

    private WoWLocalPlayer Me = ObjectManager.Me;
    public static Timer stateTimer = new Timer();
    private DatabaseNPC foodVendor;
    private int foodToBuy;

    public override bool NeedToRun
    {
        get
        {
            if (!stateTimer.IsReady 
                || Me.Level <= 3 
                || !PluginSettings.CurrentSetting.AutobuyFood 
                || Helpers.GetMoney < 1000
                || wManagerSetting.CurrentSetting.FoodAmount <= 0)
                return false;

            stateTimer = new Timer(5000);

            if (Me.Level > 10) // to be moved
                NPCBlackList.AddNPCListToBlacklist(new[] { 5871, 8307, 3489 });

            if (Helpers.OutOfFood())
            {
                foodVendor = SelectBestFoodVendor();
                if (foodVendor == null)
                {
                    Main.Logger("Couldn't find food vendor");
                    return false;
                }
                return true;
            }
            return false;
        }
    }

    public override void Run()
    {
        Main.Logger("Nearest Vendor from player:\n" + "Name: " + foodVendor.Name + "[" + foodVendor.Id + "]\nPosition: " + foodVendor.Position.ToStringXml() + "\nDistance: " + foodVendor.Position.DistanceTo(Me.Position) + " yrds");

        if (Me.Position.DistanceTo(foodVendor.Position) >= 10)
            GoToTask.ToPosition(foodVendor.Position);

        if (Me.Position.DistanceTo(foodVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(foodVendor))
                return;

            // Sell first
            Helpers.SellItems(foodVendor);

            string foodNameToBuy = ItemsManager.GetNameById(foodToBuy);
            wManagerSetting.CurrentSetting.FoodName = foodNameToBuy;
            wManagerSetting.CurrentSetting.Save();

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(foodVendor.Position, foodVendor.Id, i);
                Helpers.BuyItem(foodNameToBuy, wManagerSetting.CurrentSetting.FoodAmount, 5);
                ClearDoNotSellListFromFoods();
                Helpers.AddItemToDoNotSellList(foodNameToBuy);
                Helpers.CloseWindow();
                Thread.Sleep(1000);
                if (ItemsManager.GetItemCountById((uint)foodToBuy) >= wManagerSetting.CurrentSetting.FoodAmount)
                    return;
            }
            Main.Logger($"Failed to buy {foodNameToBuy}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(foodVendor.Id);
        }
    }

    private void ClearDoNotSellListFromFoods()
    {
        foreach (KeyValuePair<int, HashSet<int>> foodList in FoodDictionary)
            foreach(int food in foodList.Value)
                Helpers.RemoveItemFromDoNotSellList(ItemsManager.GetNameById(food));
    }

    private DatabaseNPC SelectBestFoodVendor()
    {
        foodToBuy = 0;
        foodVendor = null;

        foreach (int food in GetListUsableFood().First())
        {
            DatabaseNPC vendorWithThisFood = Database.GetFoodVendor(new HashSet<int>(){ food });
            if (vendorWithThisFood != null)
            {
                if (foodVendor == null || foodVendor.Position.DistanceTo2D(Me.Position) > vendorWithThisFood.Position.DistanceTo2D(Me.Position))
                {
                    Main.Logger($"{vendorWithThisFood.Name} is {vendorWithThisFood.Position.DistanceTo2D(Me.Position)} yards away and sells {ItemsManager.GetNameById(food)}");
                    foodToBuy = food;
                    foodVendor = vendorWithThisFood;
                }
            }
        }
        Main.Logger($"Food chosen: {ItemsManager.GetNameById(foodToBuy)} at vendor {foodVendor?.Name}");
        return foodVendor;
    }

    private List<HashSet<int>> GetListUsableFood()
    {
        List<HashSet<int>> listFood = new List<HashSet<int>>();
        foreach (KeyValuePair<int, HashSet<int>> food in FoodDictionary)
        {
            if (food.Key <= Me.Level)
            {
                listFood.Add(food.Value);
                //Main.Logger("Adding Food: " + food.Value);
            }

        }
        return listFood;
    }
}

