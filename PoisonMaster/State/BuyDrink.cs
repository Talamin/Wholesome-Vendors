using DatabaseManager.Enums;
using DatabaseManager.Filter;
using DatabaseManager.Types;
using DatabaseManager.WoW;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Class;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using PoisonMaster;
using Timer = robotManager.Helpful.Timer;

    public class BuyDrink : State
    {
        public override string DisplayName
        {
            get { return "Buying Drink"; }
        }
        public override int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        private int _priority;

        public override List<State> NextStates
        {
            get { return new List<State>(); }
        }

        public override List<State> BeforeStates
        {
            get { return new List<State>(); }
        }
        public static int continentid = Usefuls.ContinentId;


        private static readonly Dictionary<int, uint> WaterDictionary = new Dictionary<int, uint>
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
        private static readonly WoWLocalPlayer Me = ObjectManager.Me;
        private static uint CurrentDrink = 159;
        private enum ConsumableType
        {
            Food,
            Drink
        }
        public static Timer checktimer = new Timer();
    // If this method return true, wrobot launch method Run(), if return false wrobot go to next state in FSM
        public override bool NeedToRun
        {
            get
            {
                if (!checktimer.IsReady || Me.Level <= 3 || !PluginSettings.CurrentSetting.AllowAutobuyWater || Helpers.Money < 1000)
                    return false;

                checktimer = new Timer(5000);

                if (Helpers.OutOfDrink() && wManagerSetting.CurrentSetting.DrinkAmount > 0)
                {
                    wManagerSetting.CurrentSetting.TryToUseBestBagFoodDrink = false;
                    SetDrink();
                    SetBuyables();
                    return true;
                }
                return false;
            }
        }

    // If NeedToRun() == true
    public override void Run()
        {
            if (ObjectManager.Me.Level > 10)
            {
                Blacklist.AddBlacklist(new[] { 5871, 8307, 3489 });
            }
            Database.ChooseDatabaseBuyVendorDrinkNPC();

            if (Database.BuyVendorsDrink != null)
            {
                while (ObjectManager.Me.Position.DistanceTo(Database.BuyVendorsDrink.Position) >= 6)
                {
                    if (MovementManager.InMovement)
                    {
                        break;
                    }
                    if (ObjectManager.Me.InCombatFlagOnly)
                    {
                        Main.Logger("Being Attacked");
                        break;
                    }
                    Thread.Sleep(800 + Usefuls.Latency);
                    GoToTask.ToPosition(Database.BuyVendorsDrink.Position);
                    break;
                }
                if (ObjectManager.Me.Position.DistanceTo(Database.BuyVendorsDrink.Position) <= 5)
                {
                    if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Name == Database.BuyVendorsDrink.Name) <= 0)
                    {
                        Main.Logger("Looks like " + Database.BuyVendorsDrink + " is not here, we choose another one");
                        if (!Blacklist.myBlacklist.Contains(Database.BuyVendorsDrink.id))
                        {
                            Blacklist.myBlacklist.Add(Database.BuyVendorsDrink.id);
                            Thread.Sleep(50);
                            return;
                        }
                    }
                    Helpers.CloseWindow();
                    GoToTask.ToPositionAndIntecractWithNpc(Database.BuyVendorsDrink.Position, Database.BuyVendorsDrink.id, 2);
                    Main.Logger("Running to Vendor");
                    Main.Logger("Nearest Vendor from player:\n" + "Name: " + Database.BuyVendorsDrink?.Name + "[" + Database.BuyVendorsDrink?.id + "]\nPosition: " + Database.BuyVendorsDrink?.Position.ToStringXml() + "\nDistance: " + Database.BuyVendorsDrink?.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
                    if (wManagerSetting.CurrentSetting.RestingMana && Helpers.OutOfDrink())
                    {
                        string drinkNameToBuy = ItemsManager.GetNameById(CurrentDrink);
                        if (string.IsNullOrWhiteSpace(drinkNameToBuy))
                        {
                            drinkNameToBuy = GetBestFromVendorBySpell(ConsumableType.Drink);
                        }
                        wManagerSetting.CurrentSetting.DrinkName = drinkNameToBuy;
                        BuyItem(drinkNameToBuy, wManagerSetting.CurrentSetting.DrinkAmount);
                        if (!wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(drinkNameToBuy))
                        {
                            wManager.wManagerSetting.CurrentSetting.DoNotSellList.Add(drinkNameToBuy);
                        }
                }
                    Thread.Sleep(2000);
                    GoToTask.ToPositionAndIntecractWithNpc(Database.BuyVendorsDrink.Position, Database.BuyVendorsDrink.id, 3);
                    if (wManagerSetting.CurrentSetting.RestingMana && Helpers.OutOfDrink())
                    {
                        string drinkNameToBuy = ItemsManager.GetNameById(CurrentDrink);
                        if (string.IsNullOrWhiteSpace(drinkNameToBuy))
                        {
                            drinkNameToBuy = GetBestFromVendorBySpell(ConsumableType.Drink);
                        }
                        wManagerSetting.CurrentSetting.DrinkName = drinkNameToBuy;
                        BuyItem(drinkNameToBuy, wManagerSetting.CurrentSetting.DrinkAmount);
                    if (!wManager.wManagerSetting.CurrentSetting.DoNotSellList.Contains(drinkNameToBuy))
                    {
                        wManager.wManagerSetting.CurrentSetting.DoNotSellList.Add(drinkNameToBuy);
                    }
                }
                    Thread.Sleep(2000);
                    Helpers.CloseWindow();
                }
            }

        }

        private void SetBuyables()
        {
            CurrentDrink = WaterDictionary.Where(i => i.Key <= Me.Level).OrderBy(i => i.Key).LastOrDefault().Value;
        }
        private void SetDrink()
        {
            string drink = GetBestFromBagBySpell(ConsumableType.Drink);
            if (!string.IsNullOrWhiteSpace(drink) && wManagerSetting.CurrentSetting.RestingMana)
            {
                wManagerSetting.CurrentSetting.DrinkName = drink;
                if (!wManagerSetting.CurrentSetting.DoNotSellList.Contains(drink))
                {
                    wManagerSetting.CurrentSetting.DoNotSellList.Add(drink);
                }
                Main.Logger("Select drink: " + drink);
            }
        }

        private string GetBestFromBagBySpell(ConsumableType consumableType)
        {
            try
            {
                var best = Bag.GetBagItem()
                    .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Name) && ItemsManager.GetItemSpell(i.Name) == SpellListManager.SpellNameInGameByName(consumableType.ToString()) && i.GetItemInfo.ItemMinLevel <= ObjectManager.Me.Level)
                    .OrderByDescending(i => i.GetItemInfo.ItemLevel)
                    .ThenBy(i => ItemsManager.GetItemCountById((uint)i.Entry))
                    .FirstOrDefault();

                if (best != null && best.IsValid && !string.IsNullOrWhiteSpace(best.Name))
                    return best.Name;
            }
            catch { }

            return string.Empty;
        }

        private string GetBestFromVendorBySpell(ConsumableType consumableType)
        {
            try
            {
                // this works only when you had item in the bag in current wow session because http://wow.gamepedia.com/API_GetItemSpell seem to use wow cache. 
                var itemNames = GetVendorItemList();
                var best = itemNames
                    .Where(i => !string.IsNullOrWhiteSpace(i) && ItemsManager.GetItemSpell(i) == SpellListManager.SpellNameInGameByName(consumableType.ToString()) && new ItemInfo(i).ItemMinLevel <= ObjectManager.Me.Level)
                    .OrderByDescending(i => new ItemInfo(i).ItemLevel)
                    .FirstOrDefault();


                if (!string.IsNullOrWhiteSpace(best))
                    return best;
            }
            catch { }

            return string.Empty;
        }

        private List<string> GetVendorItemList()
        {
            return Lua.LuaDoString<List<string>>(@"local r = {}
                                            for i=1,GetMerchantNumItems() do 
	                                            local n=GetMerchantItemInfo(i);
	                                            if n then table.insert(r, tostring(n)); end
                                            end
                                            return unpack(r);");
        }

        private void BuyItem(string name, int amount)
        {
            Main.Logger("[Select Drink] Buying " + amount + " " + name);
            Lua.LuaDoString(string.Format(@"
        local itemName = ""{0}""
        local quantity = {1}
        for i=1, GetMerchantNumItems() do
            local name = GetMerchantItemInfo(i)
            if name and name == itemName then 
                --DEFAULT_CHAT_FRAME:AddMessage(""Buying "" .. quantity .. "" stacks of "" .. itemName)
                BuyMerchantItem(i, quantity)
            end
        end", name, amount / 5));
        }

    }

