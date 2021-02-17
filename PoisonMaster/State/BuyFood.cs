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
using Timer = robotManager.Helpful.Timer;
using PoisonMaster;

    public class BuyFood : State
    {
        public override string DisplayName
        {
            get { return "Buying Food"; }
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

        private CreatureFilter BuyVendorFilter = new CreatureFilter
        {
            ContinentId = (ContinentId)Usefuls.ContinentId,

            ExcludeIds = Blacklist.myBlacklist,

            Faction = new Faction(ObjectManager.Me.Faction,
                ReactionType.Friendly),

            NpcFlags = new NpcFlag(Operator.Or,
                new List<UnitNPCFlags>
                {
                UnitNPCFlags.SellsFood
                }),

            Range = new Range(ObjectManager.Me.Position)          //is needed for auto-order function by .Get() method
        };

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

        private static readonly WoWLocalPlayer Me = ObjectManager.Me;
        private static List<uint> CurrentFoodList = new List<uint> { 0 };
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
                if (ObjectManager.Me.InCombat || ObjectManager.Me.InCombatFlagOnly || ObjectManager.Me.IsDead || Me.Level <= 3)
                {
                    return false;
                }
                if (!PluginSettings.CurrentSetting.AllowAutobuyFood)
                {
                    return false;
                }
                if (Helpers.Money < 1000)
                {
                    return false;
                }
                if (Helpers.OutOfFoodVar && wManagerSetting.CurrentSetting.FoodAmount > 0 && checktimer.IsReady)
                {
                    DisplayName = "Buying Food";
                    wManagerSetting.CurrentSetting.TryToUseBestBagFoodDrink = false;
                    SetFood();
                    SetBuyables();
                    return true;
                }
                checktimer = new Timer(5000);
                return false;
            }
        }

        // If NeedToRun() == true
        public override void Run()
        {
            Database.ChooseDatabaseBuyVendorFoodNPC();
            if (ObjectManager.Me.Level > 10)
            {
                Blacklist.AddBlacklist(new[] { 5871, 8307, 3489 });
            }
            if (Database.BuyVendorsFood != null)
            {
                while (ObjectManager.Me.Position.DistanceTo(Database.BuyVendorsFood.Position) >= 6)
                {
                    if (MovementManager.InMovement)
                    {
                        break;
                    }
                    if (ObjectManager.Me.InCombatFlagOnly)
                    {
                        Logging.Write("Being  Attacked");
                        break;
                    }
                    Thread.Sleep(800 + Usefuls.Latency);
                    GoToTask.ToPosition(Database.BuyVendorsFood.Position);
                    break;
                }
                if (ObjectManager.Me.Position.DistanceTo(Database.BuyVendorsFood.Position) <= 5)
                {
                    if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Name == Database.BuyVendorsFood.Name) <= 0)
                    {
                        Logging.Write("Looks like " + Database.BuyVendorsFood + " is not here, we choose another one");
                        if (!Blacklist.myBlacklist.Contains(Database.BuyVendorsFood.id))
                        {
                            Blacklist.myBlacklist.Add(Database.BuyVendorsFood.id);
                            Thread.Sleep(50);
                            return;
                        }
                    }
                    Helpers.CloseWindow();
                    GoToTask.ToPositionAndIntecractWithNpc(Database.BuyVendorsFood.Position, Database.BuyVendorsFood.id, 2);
                    Logging.Write("Running to Food Vendor");
                    Logging.Write("Nearest Vendor from player:\n" + "Name: " + Database.BuyVendorsFood?.Name + "[" + Database.BuyVendorsFood?.id + "]\nPosition: " + Database.BuyVendorsFood?.Position.ToStringXml() + "\nDistance: " + Database.BuyVendorsFood?.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
                    if (Helpers.OutOfFood())
                    {
                        List<string> vendorItemList = GetVendorItemList();
                        string foodNameToBuy = vendorItemList.FirstOrDefault(i => CurrentFoodList.Select(ItemsManager.GetNameById).Contains(i));
                        wManagerSetting.CurrentSetting.FoodName = foodNameToBuy;
                        BuyItem(foodNameToBuy, wManagerSetting.CurrentSetting.FoodAmount);
                    Logging.Write("We have bought " + wManagerSetting.CurrentSetting.FoodAmount + " of " + foodNameToBuy);
                    }
                    Thread.Sleep(2000);
                    GoToTask.ToPositionAndIntecractWithNpc(Database.BuyVendorsFood.Position, Database.BuyVendorsFood.id, 3);
                    if (Helpers.OutOfFood())
                    {
                        List<string> vendorItemList = GetVendorItemList();
                        string foodNameToBuy = vendorItemList.FirstOrDefault(i => CurrentFoodList.Select(ItemsManager.GetNameById).Contains(i));
                        wManagerSetting.CurrentSetting.FoodName = foodNameToBuy;
                        BuyItem(foodNameToBuy, wManagerSetting.CurrentSetting.FoodAmount);
                    }
                    Thread.Sleep(2000);
                    Helpers.CloseWindow();
                }
            }

        }

        private void SetBuyables()
        {
            CurrentFoodList = FoodDictionary.Where(i => i.Key <= Me.Level).OrderBy(i => i.Key).LastOrDefault().Value;
        }
        private void SetFood()
        {
            string food = GetBestFromBagBySpell(ConsumableType.Food);
            if (!string.IsNullOrWhiteSpace(food))
            {
                wManagerSetting.CurrentSetting.FoodName = food;
                if (!wManagerSetting.CurrentSetting.DoNotSellList.Contains(food))
                {
                    wManagerSetting.CurrentSetting.DoNotSellList.Add(food);
                }
                Logging.WriteDebug("Select food: " + food);
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
            Logging.WriteDebug("[AutoSelectFoodAndDrink] Buying " + amount + " " + name);
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

