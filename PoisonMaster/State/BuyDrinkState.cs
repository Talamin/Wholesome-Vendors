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
            || !CurrentSetting.AutoBuyWater
            || wManagerSetting.CurrentSetting.DrinkAmount <= 0)
                return false;

            stateTimer = new Timer(5000);

            if (Me.Level > 10) // to be moved
                NPCBlackList.AddNPCListToBlacklist(new[] { 5871, 8307, 3489 });

            SetDrinkAndVendor();

            if (DrinkIdToBuy == 0)
                return false;

            if (DrinkVendor == null)
            {
                Main.Logger("Couldn't find drink vendor");
                return false;
            }

            if (!Helpers.HaveEnoughMoneyFor(DrinkAmountToBuy, DrinkNameToBuy))
                return false;

            if (ItemsManager.GetItemCountById((uint)DrinkIdToBuy) <= 3)
                return true;

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

            wManagerSetting.CurrentSetting.DrinkName = DrinkNameToBuy;
            wManagerSetting.CurrentSetting.Save();
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
            allDrinks.Add(ItemsManager.GetNameById(drink.Value));

        return allDrinks;
    }

    private void ClearDoNotSellListFromDrinks()
    {
        foreach (KeyValuePair<int, int> drink in WaterDictionary)
            Helpers.RemoveItemFromDoNotSellList(ItemsManager.GetNameById(drink.Value));
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
                DrinkNameToBuy = ItemsManager.GetNameById((uint)DrinkIdToBuy);
                return;
            }
        }
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

