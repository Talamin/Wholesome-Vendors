using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using PoisonMaster;
using Timer = robotManager.Helpful.Timer;
using System.Threading;

public class BuyDrinkState : State
{
    public override string DisplayName => "Buying Drink";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private Timer stateTimer = new Timer();
    private DatabaseNPC drinkVendor;
    private int drinkToBuy;

    private readonly Dictionary<int, int> WaterDictionary = new Dictionary<int, int>
        {
            { 75, 33444 }, // Pungent Seal Whey
            { 71, 33444 }, // Pungent Seal Whey -- make sure this is only used in WotLK
            { 65, 27860 }, // Purified Draenic Water
            { 61, 28399 }, // Filtered Draenic Water -- make sure this is only used in TBC
            { 50, 8766 }, // Morning Glory Dew
            { 40, 1645 }, // Moonberry Juice
            { 30, 1708 }, // Sweet Nectar
            { 20, 1205 }, // Melon Juice
            { 10, 1179 }, // Ice Cold Milk
            { 5, 159 }, // Refreshing Spring water
            { 0, 159 }, // Refreshing Spring water
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

            if (Me.Level > 10) // to be moved
                NPCBlackList.AddNPCListToBlacklist(new[] { 5871, 8307, 3489 });

            if (Helpers.OutOfDrink())
            {
                drinkVendor = SelectBestDrinkVendor();
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
        Main.Logger("Nearest Vendor from player:\n" + "Name: " + drinkVendor.Name + "[" + drinkVendor.Name + "]\nPosition: " + drinkVendor.Position.ToStringXml() + "\nDistance: " + drinkVendor.Position.DistanceTo(Me.Position) + " yrds");
        
        if (Me.Position.DistanceTo(drinkVendor.Position) >= 6)
            GoToTask.ToPosition(drinkVendor.Position);

        if (Helpers.NpcIsAbsentOrDead(drinkVendor))
            return;

        string drinkNameToBuy = ItemsManager.GetNameById(drinkToBuy);
        wManagerSetting.CurrentSetting.DrinkName = drinkNameToBuy;

        for (int i = 0; i <= 5; i++)
        {
            GoToTask.ToPositionAndIntecractWithNpc(drinkVendor.Position, drinkVendor.Id, i);
            Helpers.BuyItem(drinkNameToBuy, wManagerSetting.CurrentSetting.DrinkAmount);
            Helpers.AddItemToDoNotSellList(drinkNameToBuy);
            wManagerSetting.CurrentSetting.DrinkName = drinkNameToBuy;
            Helpers.CloseWindow();
            Thread.Sleep(1000);
            if (ItemsManager.GetItemCountById((uint)drinkToBuy) >= wManagerSetting.CurrentSetting.DrinkAmount)
                return;
        }
        Main.Logger($"Failed to buy {drinkNameToBuy}, blacklisting vendor");
        NPCBlackList.AddNPCToBlacklist(drinkVendor.Id);
    }

    private DatabaseNPC SelectBestDrinkVendor()
    {
        drinkToBuy = 0;
        foreach(int drink in GetListUsableDrink())
        {
            DatabaseNPC vendorWithThisDrink = Database.GetDrinkVendor(new HashSet<int>() { drink });
            if(vendorWithThisDrink !=null)
            {
                drinkToBuy = drink;
                return vendorWithThisDrink;
            }
        }
        return null;
    }

    private HashSet<int> GetListUsableDrink()
    {
        HashSet<int> listDrink = new HashSet<int>();
        foreach (KeyValuePair<int,int> drink in WaterDictionary)
        {
            if (drink.Key <= Me.Level)
                listDrink.Add((int)drink.Value);
        }
        return listDrink;
    }
}

