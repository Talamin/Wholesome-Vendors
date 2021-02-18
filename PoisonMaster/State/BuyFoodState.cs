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
using static PoisonMaster.PMEnums;

public class BuyFoodState : State
{
    public override string DisplayName => "Buying Food";

    private static readonly Dictionary<int, List<uint>> FoodDictionary = new Dictionary<int, List<uint>>
        {
            { 0, new List<uint>{ 117, 4540, 2070, 4604, 787 , 4536} }, // Haunch of Meat
            { 5, new List<uint>{ 117, 4540, 2070, 4604, 787 , 4537} }, // Haunch of Meat
            { 10, new List<uint>{ 2287, 4541, 414, 4605, 4592, 4538} }, // Haunch of Meat
            { 20, new List<uint>{ 3770, 4542, 422, 4606, 4593, 4538 } }, // Mutton Chop
            { 25, new List<uint>{ 3771, 4544, 1707, 4607, 4594, 4539 } }, // Wild Hog Shank
            { 35, new List<uint>{ 4599, 4601, 3927, 4608, 6887 } }, // Cured Ham Steak
            { 45, new List<uint>{ 8952, 8950, 8932, 8948, 8957} }, // Roasted Quail
            { 61, new List<uint>{ 27854, 27855, 27856, 27857, 27858, 27859 } }, // Smoked Talbuk Venison -- make sure this is only used in TBC
            { 65, new List<uint>{ 29451, 29449, 29450, 29448, 29452, 29453 } }, // Clefthoof Ribs
            { 75, new List<uint>{ 35953 } }, // Mead Basted Caribouhl au
            { 85, new List<uint>{ 35953 } },
        };

    private WoWLocalPlayer Me = ObjectManager.Me;
    private List<uint> CurrentFoodList = new List<uint> { 0 };
    public static Timer stateTimer = new Timer();
    private DatabaseNPC foodVendor;

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

            if (Helpers.OutOfFood())
            {
                foodVendor = Database.GetFoodVendor();
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
        SetFoodToBuy();

        if (Me.Level > 10)
            NPCBlackList.AddNPCListToBlacklist(new[] { 5871, 8307, 3489 });

        if (Me.Position.DistanceTo(foodVendor.Position) >= 6)
        {
            GoToTask.ToPosition(foodVendor.Position);
        }
        else
        {
            if (Helpers.NpcIsAbsentOrDead(foodVendor))
                return;

            Helpers.CloseWindow();
            GoToTask.ToPositionAndIntecractWithNpc(foodVendor.Position, foodVendor.Id, 2);
            Main.Logger("Nearest Vendor from player:\n" + "Name: " + foodVendor.Name + "[" + foodVendor.Id + "]\nPosition: " + foodVendor.Position.ToStringXml() + "\nDistance: " + foodVendor.Position.DistanceTo(Me.Position) + " yrds");

            List<string> vendorItemList = Helpers.GetVendorItemList();
            string foodNameToBuy = vendorItemList.FirstOrDefault(i => CurrentFoodList.Select(ItemsManager.GetNameById).Contains(i));
            wManagerSetting.CurrentSetting.FoodName = foodNameToBuy;

            Helpers.BuyItem(foodNameToBuy, wManagerSetting.CurrentSetting.FoodAmount);
            Helpers.AddItemToDoNotSellList(foodNameToBuy);
            SetFoodInWRobot();

            Thread.Sleep(2000);

            GoToTask.ToPositionAndIntecractWithNpc(foodVendor.Position, foodVendor.Id, 3);

            // 2nd try?
            if (Helpers.OutOfFood())
            {
                vendorItemList = Helpers.GetVendorItemList();
                foodNameToBuy = vendorItemList.FirstOrDefault(i => CurrentFoodList.Select(ItemsManager.GetNameById).Contains(i));
                wManagerSetting.CurrentSetting.FoodName = foodNameToBuy;
                Helpers.BuyItem(foodNameToBuy, wManagerSetting.CurrentSetting.FoodAmount);
                Helpers.AddItemToDoNotSellList(foodNameToBuy);
                SetFoodInWRobot();
            }

            Thread.Sleep(1000);

            Helpers.CloseWindow();
        }
    }

    private void SetFoodToBuy()
    {
        CurrentFoodList = FoodDictionary
            .Where(i => i.Key <= Me.Level)
            .OrderBy(i => i.Key)
            .LastOrDefault().Value;
    }

    private void SetFoodInWRobot()
    {
        string food = Helpers.GetBestConsumableFromBags(PMConsumableType.Food);
        if (!string.IsNullOrWhiteSpace(food))
        {
            wManagerSetting.CurrentSetting.FoodName = food;
            Helpers.AddItemToDoNotSellList(food);
            Main.Logger("Select food: " + food);
        }
    }
}

