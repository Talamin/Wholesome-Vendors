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

public class BuyDrinkState : State
{
    public override string DisplayName { get; set; } = "WV Buying Drink";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private Timer stateTimer = new Timer();
    private ModelNpcVendor DrinkVendor;
    private ModelItemTemplate DrinkToBuy;

    private int DrinkAmountSetting => PluginSettings.CurrentSetting.DrinkNbToBuy;
    private int NbDrinksInBag;
    private int AmountToBuy;

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !stateTimer.IsReady
                || Me.Level <= 3
                || DrinkAmountSetting <= 0
                || Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);
            DrinkVendor = null;
            DrinkToBuy = null;

            NbDrinksInBag = GetNbDrinksInBags();
            AmountToBuy = DrinkAmountSetting - NbDrinksInBag;

            if (NbDrinksInBag <= DrinkAmountSetting / 2)
            {
                foreach (ModelItemTemplate drink in MemoryDB.GetAllUsableDrinks)
                {
                    if (Helpers.HaveEnoughMoneyFor(AmountToBuy, drink))
                    {
                        ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(drink);
                        if (vendor != null)
                        {
                            DrinkToBuy = drink;
                            DrinkVendor = vendor;
                            // Normal
                            if (NbDrinksInBag <= DrinkAmountSetting / 10)
                            {
                                DisplayName = $"Buying {AmountToBuy} x {DrinkToBuy.Name} at vendor {DrinkVendor.CreatureTemplate.name}";
                                return true;
                            }
                            // Drive-by
                            if (NbDrinksInBag <= DrinkAmountSetting / 2
                                && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                            {
                                DisplayName = $"Drive-by buying {AmountToBuy} x {DrinkToBuy.Name} at vendor {DrinkVendor.CreatureTemplate.name}";
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }

    public override void Run()
    {
        Main.Logger(DisplayName);
        Vector3 vendorPos = DrinkVendor.CreatureTemplate.Creature.GetSpawnPosition;

        Helpers.CheckMailboxNearby(DrinkVendor.CreatureTemplate);

        if (Me.Position.DistanceTo(vendorPos) >= 10)
            GoToTask.ToPosition(vendorPos);

        if (Me.Position.DistanceTo(vendorPos) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(DrinkVendor.CreatureTemplate))
                return;

            ClearObsoleteDrinks();
            Helpers.AddItemToDoNotSellList(DrinkToBuy.Name);
            Helpers.AddItemToDoNotMailList(DrinkToBuy.Name);

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(vendorPos, DrinkVendor.entry, i);
                Thread.Sleep(500);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.IsVendorGossipOpen())
                {
                    Helpers.SellItems(DrinkVendor.CreatureTemplate);

                    Helpers.BuyItem(DrinkToBuy.Name, DrinkAmountSetting - GetNbDrinksInBags(), DrinkToBuy.BuyCount);
                    Helpers.CloseWindow();
                    Thread.Sleep(1000);

                    if (GetNbDrinksInBags() >= DrinkAmountSetting)
                        return;
                }
            }
            Main.Logger($"Failed to buy {DrinkToBuy.Name}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(DrinkVendor.entry);
        }
    }

    private void ClearObsoleteDrinks()
    {
        foreach (ModelItemTemplate drink in MemoryDB.GetAllDrinks)
        {
            Helpers.RemoveItemFromDoNotSellList(drink.Name);
        }
    }

    private int GetNbDrinksInBags()
    {
        int nbDrinksInBags = 0;
        List<WoWItem> items = Bag.GetBagItem();
        List<ModelItemTemplate> allDrinks = MemoryDB.GetAllUsableDrinks;
        string drinkToSet = null;
        foreach (WoWItem item in items)
        {
            if (allDrinks.Exists(ua => ua.Entry == item.Entry))
            {
                nbDrinksInBags += ItemsManager.GetItemCountById((uint)item.Entry);
                drinkToSet = item.Name;
            }
        }

        if (drinkToSet != null && wManagerSetting.CurrentSetting.DrinkName != drinkToSet)
        {
            Main.Logger($"Setting drink to {drinkToSet}");
            wManagerSetting.CurrentSetting.DrinkName = drinkToSet;
            wManagerSetting.CurrentSetting.Save();
        }

        return nbDrinksInBags;
    }
}

