using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using PoisonMaster;
using Timer = robotManager.Helpful.Timer;
using System.Threading;
using static PluginSettings;
using System.Linq;

public class BuyDrinkState : State
{
    public override string DisplayName => "WV Buying Drink";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private Timer stateTimer = new Timer();
    private DatabaseNPC DrinkVendor;
    private int DrinkIdToBuy;
    private string DrinkNameToBuy;
    private int DrinkAmountToBuy => wManagerSetting.CurrentSetting.DrinkAmount;

    private readonly Dictionary<int, int> WaterDictionary = new Dictionary<int, int>
        {
            { 75, 33444 }, // Pungent Seal Whey -- make sure this is only used in WotLK
            { 65, 27860 }, // Purified Draenic Water
            { 55, 28399 }, // Filtered Draenic Water -- make sure this is only used in TBC
            { 45, 8766 }, // Morning Glory Dew
            { 35, 1645 }, // Moonberry Juice
            { 25, 1708 }, // Sweet Nectar
            { 15, 1205 }, // Melon Juice
            { 5, 1179 }, // Ice Cold Milk
            { 0, 159 }, // Refreshing Spring water
        };

    public override bool NeedToRun
    {
        get
        {
            if (!stateTimer.IsReady
                || Me.Level <= 3
                || !CurrentSetting.AutoBuyWater
                || wManagerSetting.CurrentSetting.DrinkAmount <= 0
                || Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);

            if (Me.Level > 10) // to be moved
                NPCBlackList.AddNPCListToBlacklist(new[] { 5871, 8307, 3489 });

            SetDrinkAndVendor();

            if (DrinkIdToBuy > 0
                && GetNbDrinksInBags() <= 3
                && Helpers.HaveEnoughMoneyFor(DrinkAmountToBuy, DrinkNameToBuy))
                return DrinkVendor != null;

            return false;
        }
    }

    public override void Run()
    {
        Main.Logger($"Buying {DrinkAmountToBuy} x {DrinkNameToBuy} at vendor {DrinkVendor.Name}");

        if (Me.Position.DistanceTo(DrinkVendor.Position) >= 10)
            GoToTask.ToPosition(DrinkVendor.Position);

        if (Me.Position.DistanceTo(DrinkVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(DrinkVendor))
                return;

            ClearDoNotSellListFromDrinks();
            Helpers.AddItemToDoNotSellList(DrinkNameToBuy);

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
                    VendorItem vendorItem = CurrentSetting.VendorItems.Find(item => item.Name == DrinkNameToBuy);
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

        foreach (KeyValuePair<int, int> drink in WaterDictionary)
            allDrinks.Add(Database.GetItemName(drink.Value));

        return allDrinks;
    }

    private void ClearDoNotSellListFromDrinks()
    {
        foreach (KeyValuePair<int, int> drink in WaterDictionary)
            Helpers.RemoveItemFromDoNotSellList(Database.GetItemName(drink.Value));
    }

    private void SetDrinkAndVendor()
    {
        DrinkIdToBuy = 0;
        DrinkVendor = null;
        foreach(int drink in GetListUsableDrink())
        {
            DatabaseNPC vendorWithThisDrink = Database.GetDrinkVendor(new HashSet<int>() { drink });
            if(vendorWithThisDrink !=null)
            {
                DrinkIdToBuy = drink;
                DrinkVendor = vendorWithThisDrink;
                DrinkNameToBuy = Database.GetItemName(DrinkIdToBuy);
                break;
            }
        }

        List<int> listDrinksInBags = GetListDrinksFromBags();
        if (listDrinksInBags.Count > 0)
        {
            string drinkToSet = Database.GetItemName(listDrinksInBags.Last());
            if (drinkToSet != wManagerSetting.CurrentSetting.DrinkName)
            {
                wManagerSetting.CurrentSetting.DrinkName = drinkToSet;
                wManagerSetting.CurrentSetting.Save();
                Main.Logger($"Setting drink to {drinkToSet}");
            }
        }
    }

    private List<int> GetListUsableDrink()
    {
        List<int> listDrink = new List<int>();
        foreach (KeyValuePair<int,int> drink in WaterDictionary)
        {
            if (drink.Key <= Me.Level)
                listDrink.Add((int)drink.Value);
        }
        return listDrink;
    }

    private int GetNbDrinksInBags()
    {
        int nbDrinksInBags = 0;
        GetListDrinksFromBags().ForEach(f => nbDrinksInBags += ItemsManager.GetItemCountById((uint)f));
        //Main.Logger($"We have {nbDrinksInBags} drink items in our bags");
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

