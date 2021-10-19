using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class BuyDrinkState : State
{
    public override string DisplayName => "WV Buying Drink";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private Timer stateTimer = new Timer();
    private DatabaseNPC DrinkVendor;
    private int DrinkIdToBuy;
    private string DrinkNameToBuy;
    private int DrinkAmountToBuy => PluginSettings.CurrentSetting.DrinkNbToBuy;

    private static readonly Dictionary<int, HashSet<int>> WaterDictionary = new Dictionary<int, HashSet<int>>
        {
            { 75, new HashSet<int>{ 33445, 41731, 42777 } },
            { 70, new HashSet<int>{ 33444 } }, // Pungent Seal Whey 
            { 65, new HashSet<int>{ 27860, 35954 } }, // Purified Draenic Water
            { 60, new HashSet<int>{ 28399 } }, // Filtered Draenic Water 
            { 45, new HashSet<int>{ 8766 } }, // Morning Glory Dew
            { 35, new HashSet<int>{ 1645 } }, // Moonberry Juice
            { 25, new HashSet<int>{ 1708 } }, // Sweet Nectar
            { 15, new HashSet<int>{ 1205 } }, // Melon Juice
            { 10, new HashSet<int>{ 1179 } }, // Ice Cold Milk
            { 0, new HashSet<int>{ 159 } }, // Refreshing Spring water
        };

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !stateTimer.IsReady
                || Me.Level <= 3
                || DrinkAmountToBuy <= 0
                || Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);

            SetDrinkAndVendor();

            if (DrinkIdToBuy > 0
                && GetNbDrinksInBags() <= 3)
                return DrinkVendor != null;

            return false;
        }
    }

    public override void Run()
    {
        Main.Logger($"Buying {DrinkAmountToBuy} x {DrinkNameToBuy} [{DrinkIdToBuy}] at vendor {DrinkVendor.Name}");

        Helpers.CheckMailboxNearby(DrinkVendor);

        if (Me.Position.DistanceTo(DrinkVendor.Position) >= 10)
            GoToTask.ToPosition(DrinkVendor.Position);

        if (Me.Position.DistanceTo(DrinkVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(DrinkVendor))
                return;

            ClearDoNotSellListFromDrinks();
            Helpers.AddItemToDoNotSellList(DrinkNameToBuy);
            Helpers.AddItemToDoNotMailList(DrinkNameToBuy);

            List<string> allDrinksNames = GetPotentialDrinksNames();

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(DrinkVendor.Position, DrinkVendor.Id, i);
                Thread.Sleep(500);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.OpenRecordVendorItems(allDrinksNames)) // also checks if vendor window is open
                {
                    // Sell first
                    Helpers.SellItems(DrinkVendor);
                    if (!Helpers.HaveEnoughMoneyFor(DrinkAmountToBuy, DrinkNameToBuy))
                    {
                        Main.Logger("Not enough money. Item prices sold by this vendor are now recorded.");
                        Helpers.CloseWindow();
                        return;
                    }
                    PluginSettings.VendorItem vendorItem = PluginSettings.CurrentSetting.VendorItems.Find(item => item.Name == DrinkNameToBuy);
                    Helpers.BuyItem(DrinkNameToBuy, DrinkAmountToBuy, vendorItem.Stack);
                    Helpers.CloseWindow();
                    Thread.Sleep(1000);
                    if (ItemsManager.GetItemCountById((uint)DrinkIdToBuy) >= DrinkAmountToBuy)
                        return;
                }
            }
            Main.Logger($"Failed to buy {DrinkNameToBuy}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(DrinkVendor.Id);
        }
    }

    private List<string> GetPotentialDrinksNames()
    {
        List<string> allDrinks = new List<string>();

        foreach (var drinks in WaterDictionary)
            foreach (var drink in drinks.Value)
                allDrinks.Add(Database.GetItemName(drink));

        return allDrinks;
    }

    private void ClearDoNotSellListFromDrinks()
    {
        foreach (var drinks in WaterDictionary)
            foreach (var drink in drinks.Value)
                Helpers.RemoveItemFromDoNotSellList(Database.GetItemName(drink));
    }

    private void SetDrinkAndVendor()
    {
        DrinkIdToBuy = 0;
        DrinkVendor = null;

        foreach (KeyValuePair<int, HashSet<int>> drinkEntry in WaterDictionary.Where(f => f.Key <= Me.Level))
        {
            foreach (int drinkId in drinkEntry.Value)
            {
                DatabaseNPC vendorWithThisDrink = Database.GetDrinkVendor(new HashSet<int>() { drinkId });

                // Skip to lower tier drink if we don't have enough money for this tier
                if (!Helpers.HaveEnoughMoneyFor(DrinkAmountToBuy, Database.GetItemName(drinkId)))
                    break;

                if (vendorWithThisDrink != null)
                {
                    if (DrinkVendor == null || vendorWithThisDrink.Position.DistanceTo2D(Me.Position) < DrinkVendor.Position.DistanceTo2D(Me.Position))
                    {
                        DrinkIdToBuy = drinkId;
                        DrinkNameToBuy = Database.GetItemName(drinkId);
                        DrinkVendor = vendorWithThisDrink;
                    }
                }
            }
            if (DrinkVendor != null)
                break;
        }

        if (DrinkVendor == null)
        {
            Main.Logger($"Couldn't find any drink vendor");
            return;
        }

        List<int> listDrinksInBags = GetListDrinksFromBags();
        if (listDrinksInBags.Count > 0)
        {
            string drinkToSet = Database.GetItemName(listDrinksInBags.Last());
            if (drinkToSet != wManagerSetting.CurrentSetting.DrinkName)
            {
                Main.Logger($"Setting drink to {drinkToSet}");
                wManagerSetting.CurrentSetting.DrinkName = drinkToSet;
                wManagerSetting.CurrentSetting.Save();
            }
        }
    }

    private List<int> GetListUsableDrink()
    {
        List<int> listDrink = new List<int>();
        foreach (var drinks in WaterDictionary)
        {
            if (drinks.Key <= Me.Level)
            {
                foreach (var drink in drinks.Value)
                {
                    listDrink.Add(drink);
                }
            }
        }
        return listDrink;
    }

    private int GetNbDrinksInBags()
    {
        int nbDrinksInBags = 0;
        GetListDrinksFromBags().ForEach(f => nbDrinksInBags += ItemsManager.GetItemCountById((uint)f));
        return nbDrinksInBags;
    }

    private List<int> GetListDrinksFromBags() // From best to worst
    {
        List<int> drinksInBags = new List<int>();
        foreach (int drink in GetListUsableDrink())
            if (ItemsManager.GetItemCountById((uint)drink) > 0)
                drinksInBags.Add(drink);

        return drinksInBags;
    }
}

