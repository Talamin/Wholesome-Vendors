using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using PoisonMaster;
using Timer = robotManager.Helpful.Timer;
using static PoisonMaster.PMEnums;

public class BuyDrinkState : State
{
    public override string DisplayName => "Buying Drink";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private Timer stateTimer = new Timer();
    private DatabaseNPC drinkVendor;
    private static uint drinkToBuy = 159;

    private readonly Dictionary<int, uint> WaterDictionary = new Dictionary<int, uint>
        {
            { 0, 159 }, // Refreshing Spring water
            { 5, 159 }, // Refreshing Spring water
            { 10, 1179 }, // Ice Cold Milk
            { 20, 1205 }, // Melon Juice
            { 30, 1708 }, // Sweet Nectar
            { 40, 1645 }, // Moonberry Juice
            { 50, 8766 }, // Morning Glory Dew
            { 61, 28399 }, // Filtered Draenic Water -- make sure this is only used in TBC
            { 65, 27860 }, // Purified Draenic Water
            { 71, 33444 }, // Pungent Seal Whey -- make sure this is only used in WotLK
            { 75, 33444 } // Pungent Seal Whey
        };

    public override bool NeedToRun
    {
        get
        {
            if (!stateTimer.IsReady
            || Me.Level <= 3
            || !PluginSettings.CurrentSetting.AutoBuyWater
            || !wManagerSetting.CurrentSetting.RestingMana
            || Helpers.GetMoney < 1000
            || wManagerSetting.CurrentSetting.DrinkAmount <= 0)
                return false;

            stateTimer = new Timer(5000);

            if (Helpers.OutOfDrink())
            {
                drinkVendor = Database.GetDrinkVendor();
                if (drinkVendor == null)
                {
                    Main.Logger("Couldn't find drink vendor");
                    return false;
                }
                return true;
            }
            return false;
        }
    }

    public override void Run()
    {
        SetDrinkToBuy();

        if (Me.Level > 10)
            NPCBlackList.AddNPCListToBlacklist(new[] { 5871, 8307, 3489 });

        if (Me.Position.DistanceTo(drinkVendor.Position) >= 6)
        {
            GoToTask.ToPosition(drinkVendor.Position);
        }
        else
        {
            if (Helpers.NpcIsAbsentOrDead(drinkVendor))
                return;

            Helpers.CloseWindow();
            GoToTask.ToPositionAndIntecractWithNpc(drinkVendor.Position, drinkVendor.Id, 2);
            Main.Logger("Nearest Vendor from player:\n" + "Name: " + drinkVendor.Name + "[" + drinkVendor.Id + "]\nPosition: " + drinkVendor.Position.ToStringXml() + "\nDistance: " + drinkVendor.Position.DistanceTo(Me.Position) + " yrds");

            string drinkNameToBuy = ItemsManager.GetNameById(drinkToBuy);
            if (string.IsNullOrWhiteSpace(drinkNameToBuy))
                drinkNameToBuy = Helpers.GetBestFromVendor(PMConsumableType.Drink);

            Helpers.BuyItem(drinkNameToBuy, wManagerSetting.CurrentSetting.DrinkAmount);
            Helpers.AddItemToDoNotSellList(drinkNameToBuy);
            SetDrinkInWRobot();

            Thread.Sleep(2000);

            GoToTask.ToPositionAndIntecractWithNpc(drinkVendor.Position, drinkVendor.Id, 3);

            // 2nd try?
            if (Helpers.OutOfDrink())
            {
                drinkNameToBuy = ItemsManager.GetNameById(drinkToBuy);
                if (string.IsNullOrWhiteSpace(drinkNameToBuy))
                    drinkNameToBuy = Helpers.GetBestFromVendor(PMConsumableType.Drink);

                wManagerSetting.CurrentSetting.DrinkName = drinkNameToBuy;
                Helpers.BuyItem(drinkNameToBuy, wManagerSetting.CurrentSetting.DrinkAmount);
                Helpers.AddItemToDoNotSellList(drinkNameToBuy);
                SetDrinkInWRobot();
            }

            Thread.Sleep(1000);

            Helpers.CloseWindow();
        }
    }

    private void SetDrinkToBuy()
    {
        drinkToBuy = WaterDictionary
            .Where(i => i.Key <= Me.Level)
            .OrderBy(i => i.Key)
            .LastOrDefault().Value;
    }

    private void SetDrinkInWRobot()
    {
        string drink = Helpers.GetBestConsumableFromBags(PMConsumableType.Drink);
        if (drink != null && wManagerSetting.CurrentSetting.RestingMana)
        {
            wManagerSetting.CurrentSetting.DrinkName = drink;
            Helpers.AddItemToDoNotSellList(drink);
            Main.Logger("Set drink: " + drink);
        }
    }
}

