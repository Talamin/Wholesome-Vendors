using robotManager.FiniteStateMachine;
using System;
using System.Linq;
using System.Threading;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static PoisonMaster.PMEnums;
using robotManager.Products;
using System.Collections.Generic;
using wManager.Wow.Class;
using wManager.Wow.Bot.Tasks;
using static PluginSettings;

namespace PoisonMaster
{
    public class Helpers
    {
        private static bool saveWRobotSettingRepair;

        public static int GetMoney => (int)ObjectManager.Me.GetMoneyCopper;

        public static void AddState(Engine engine, State state, string replace)
        {
            bool statedAdded = engine.States.Exists(s => s.DisplayName == state.DisplayName);

            if (!statedAdded && engine != null)
            {
                try
                {
                    State stateToReplace = engine.States.Find(s => s.DisplayName == replace);

                    if (stateToReplace == null)
                    {
                        Main.Logger($"Couldn't find state {replace}");
                        return;
                    }

                    int priorityToSet = stateToReplace.Priority;


                    // Move all superior states one slot up
                    foreach (State s in engine.States)
                    {
                        if (s.Priority > priorityToSet)
                            s.Priority++;
                    }

                    engine.AddState(state);
                    state.Priority = priorityToSet + 1;
                    //Main.Logger($"Adding state {state.DisplayName} with prio {priorityToSet}");
                    engine.AddState(state);
                    engine.States.Sort();
                }
                catch (Exception ex)
                {
                    Main.Logger("Erreur : {0}" + ex.ToString());
                }
            }
        }

        public static void CloseWindow()
        {
            try
            {
                Memory.WowMemory.LockFrame();
                Lua.LuaDoString("CloseQuest()");
                Lua.LuaDoString("CloseGossip()");
                Lua.LuaDoString("CloseBankFrame()");
                Lua.LuaDoString("CloseMail()");
                Lua.LuaDoString("CloseMerchant()");
                Lua.LuaDoString("ClosePetStables()");
                Lua.LuaDoString("CloseTaxiMap()");
                Lua.LuaDoString("CloseTrainer()");
                Lua.LuaDoString("CloseAuctionHouse()");
                Lua.LuaDoString("CloseGuildBankFrame()");
                Lua.LuaDoString("CloseLoot()");
                Lua.RunMacroText("/Click QuestFrameCloseButton");
                Lua.LuaDoString("ClearTarget()");
                Thread.Sleep(150);
            }
            catch (Exception e)
            {
                Main.LoggerError("public static void CloseWindow(): " + e);
            }
            finally
            {
                Memory.WowMemory.UnlockFrame();
            }
        }
        /*
        public static bool OutOfFood()
        {
            var allFoodAmount = Bag.GetBagItem()
                .Where(
                    i => PMConsumableType.Food.ToString() == ItemsManager.GetItemSpell(i.Name) &&
                         i.GetItemInfo.ItemMinLevel <= ObjectManager.Me.Level)
                .Select(i => ItemsManager.GetItemCountById((uint)i.Entry))
                .Aggregate(0, (i, i2) => i + i2);

            if (allFoodAmount < 1 && wManagerSetting.CurrentSetting.FoodAmount > 0)
                return true;

            return false;
        }

        public static bool OutOfDrink()
        {
            var allDrinkAmount = Bag.GetBagItem()
                .Where(
                    i => PMConsumableType.Drink.ToString() == ItemsManager.GetItemSpell(i.Name) 
                    && i.GetItemInfo.ItemMinLevel <= ObjectManager.Me.Level)
                .Select(i => ItemsManager.GetItemCountById((uint)i.Entry))
                .Aggregate(0, (i, i2) => i + i2);

            if (allDrinkAmount < 1 && wManagerSetting.CurrentSetting.DrinkAmount > 0)
                return true;

            return false;
        }
        */
        public static string GetRangedWeaponType()
        {
            uint myRangedWeapon = ObjectManager.Me.GetEquipedItemBySlot(InventorySlot.INVSLOT_RANGED);

            if (myRangedWeapon == 0)
                return null;
            else
            {
                List<WoWItem> equippedItems = EquippedItems.GetEquippedItems();
                foreach (WoWItem equippedItem in equippedItems)
                {
                    if (equippedItem.GetItemInfo.ItemSubType == "Crossbows" || equippedItem.GetItemInfo.ItemSubType == "Bows")
                        return "Bows";
                    if (equippedItem.GetItemInfo.ItemSubType == "Guns")
                        return "Guns";
                }
                return null;
            }
        }

        public static void AddItemToDoNotSellList(string itemName)
        {
            if (!wManagerSetting.CurrentSetting.DoNotSellList.Contains(itemName))
            {
                wManagerSetting.CurrentSetting.DoNotSellList.Add(itemName);
                wManagerSetting.CurrentSetting.Save();
            }
        }

        public static void RemoveItemFromDoNotSellList(string itemName)
        {
            if (wManagerSetting.CurrentSetting.DoNotSellList.Contains(itemName))
            {
                wManagerSetting.CurrentSetting.DoNotSellList.Remove(itemName);
                wManagerSetting.CurrentSetting.Save();
            }
        }

        public static void SoftRestart()
        {
            Products.InPause = true;
            Thread.Sleep(100);
            Products.InPause = false;
        }

        public static void Restart()
        {
            new Thread(() =>
            {
                Products.ProductStop();
                Thread.Sleep(2000);
                Products.ProductStart();
            }).Start();
        }

        public static string GetWoWVersion()
        {
            return Lua.LuaDoString<string>("v, b, d, t = GetBuildInfo(); return v");
        }

        public static string GetBestConsumableFromBags(PMConsumableType consumableType)
        {
            WoWItem bestConsumable = Bag.GetBagItem()
                .Where(i => i != null
                    && !string.IsNullOrWhiteSpace(i.Name)
                    && ItemsManager.GetItemSpell(i.Name) == SpellListManager.SpellNameInGameByName(consumableType.ToString())
                    && i.GetItemInfo.ItemMinLevel <= ObjectManager.Me.Level
                    && i.IsValid)
                .OrderByDescending(i => i.GetItemInfo.ItemLevel)
                .ThenBy(i => ItemsManager.GetItemCountById((uint)i.Entry))
                .FirstOrDefault();

            return bestConsumable == null ? null : bestConsumable.Name;
        }

        public static string GetBestFromVendor(PMConsumableType consumableType)
        {
            // this works only when you had item in the bag in current wow session because http://wow.gamepedia.com/API_GetItemSpell seem to use wow cache. 
            List<string> itemNames = GetVendorItemList();
            string bestConsumable = itemNames
                .Where(i => i != null
                    && !string.IsNullOrWhiteSpace(i) 
                    && ItemsManager.GetItemSpell(i) == SpellListManager.SpellNameInGameByName(consumableType.ToString()) 
                    && new ItemInfo(i).ItemMinLevel <= ObjectManager.Me.Level)
                .OrderByDescending(i => new ItemInfo(i).ItemLevel)
                .FirstOrDefault();

            return bestConsumable == null ? null : bestConsumable;
        }

        public static List<string> GetVendorItemList()
        {
            return Lua.LuaDoString<List<string>>(@"local r = {}
                                            for i=1,GetMerchantNumItems() do 
	                                            local n=GetMerchantItemInfo(i);
	                                            if n then table.insert(r, tostring(n)); end
                                            end
                                            return unpack(r);");
        }

        public static void BuyItem(string name, int amount, int stackValue)
        {
            double numberOfStacksToBuy = Math.Ceiling(amount / (double)stackValue);
            Main.Logger($"Buying {amount} x {name}");
            Lua.LuaDoString(string.Format(@"
                    local itemName = ""{0}""
                    local quantity = {1}
                    for i=1, GetMerchantNumItems() do
                        local name = GetMerchantItemInfo(i)
                        if name and name == itemName then 
                            BuyMerchantItem(i, quantity)
                        end
                    end", name, (int)numberOfStacksToBuy));
        }

        public static bool NpcIsAbsentOrDead(DatabaseNPC npc)
        {
            if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Name == npc.Name) <= 0)
            {
                Main.Logger("Looks like " + npc.Name + " is not here, blacklisting");
                NPCBlackList.AddNPCToBlacklist(npc.Id);
                return true;
            }
            return false;
        }

        public static List<WoWItemQuality> GetListQualityToSell()
        {
            List<WoWItemQuality> listQualitySell = new List<WoWItemQuality>();

            if (wManagerSetting.CurrentSetting.SellGray)
                listQualitySell.Add(WoWItemQuality.Poor);
            if (wManagerSetting.CurrentSetting.SellWhite)
                listQualitySell.Add(WoWItemQuality.Common);
            if (wManagerSetting.CurrentSetting.SellGreen)
                listQualitySell.Add(WoWItemQuality.Uncommon);
            if (wManagerSetting.CurrentSetting.SellBlue)
                listQualitySell.Add(WoWItemQuality.Rare);
            if (wManagerSetting.CurrentSetting.SellPurple)
                listQualitySell.Add(WoWItemQuality.Epic);

            return listQualitySell;
        }

        public static void SellItems(DatabaseNPC vendor)
        {
            Main.Logger("Selling items");
            List<WoWItem> bagItems = Bag.GetBagItem();
            int nbItemsInBags = bagItems.Count;

            List<string> listItemsToSell = new List<string>();
            foreach (WoWItem item in bagItems)
            {
                if (item != null 
                    && !wManagerSetting.CurrentSetting.DoNotSellList.Contains(item.Name) 
                    && item.GetItemInfo.ItemSellPrice > 0)
                    listItemsToSell.Add(item.Name);
            }

            if (listItemsToSell.Count <= 0)
                return;

            Main.Logger($"Found {listItemsToSell.Count} items to sell");

            for (int i = 1; i <= 5; i++)
            {
                Main.Logger($"Attempt {i}");
                GoToTask.ToPositionAndIntecractWithNpc(vendor.Position, vendor.Id, i);
                Vendor.SellItems(listItemsToSell, wManagerSetting.CurrentSetting.DoNotSellList, GetListQualityToSell());
                Thread.Sleep(200);
                if (Bag.GetBagItem().Count < nbItemsInBags)
                    break;
            }
        }

        public static void OverrideWRobotUserSettings()
        {
            if (PluginSettings.CurrentSetting.AutoRepair)
            {
                saveWRobotSettingRepair = wManagerSetting.CurrentSetting.Repair; // save user setting
                wManagerSetting.CurrentSetting.Repair = false; // disable user setting
            }

            wManagerSetting.CurrentSetting.Save();
        }

        public static void RestoreWRobotUserSettings()
        {
            wManagerSetting.CurrentSetting.Repair = saveWRobotSettingRepair;
            wManagerSetting.CurrentSetting.Save();
        }

        public static bool PlayerInBloodElfStartingZone()
        {
            string zone = Lua.LuaDoString<string>("return GetRealZoneText();");
            return zone == "Eversong Woods" || zone == "Ghostlands" || zone == "Silvermoon City";
        }

        public static bool PlayerInDraneiStartingZone()
        {
            string zone = Lua.LuaDoString<string>("return GetRealZoneText();");
            return zone == "Azuremyst Isle" || zone == "Bloodmyst Isle" || zone == "The Exodar";
        }

        public static bool OpenRecordVendorItems(List<string> itemsToRecord)
        {
            string vendorItems = Lua.LuaDoString<string>($@"local items = """"
                                for i=1, GetMerchantNumItems() do 
                                    local name, texture, price, quantity, numAvailable, isPurchasable, isUsable, extendedCost = GetMerchantItemInfo(i);
                                    if name then 
                                        items = items .. ""|"" .. name .. ""$"" .. price .. ""£"" .. quantity;
                                    end
                                end
                                return items;");

            if (string.IsNullOrEmpty(vendorItems))
                return false;

            string[] allItems = vendorItems.Split('|');

            foreach (string item in allItems)
            {
                if (string.IsNullOrEmpty(item))
                    continue;
                string name = item.Split('$')[0];
                int stack = Int32.Parse(item.Split('£')[1]);
                int price = Int32.Parse(item.Substring(0, item.IndexOf('£')).Split('$')[1]);

                if (itemsToRecord.Contains(name) && !PluginSettings.CurrentSetting.VendorItems.Exists(i => i.Name == name))
                    PluginSettings.CurrentSetting.VendorItems.Add(new PluginSettings.VendorItem(name, stack, price));
            }
            PluginSettings.CurrentSetting.Save();
            return true;
        }

        public static bool HaveEnoughMoneyFor(int amount, string itemName)
        {
            if (CurrentSetting.VendorItems.Exists(i => i.Name == itemName))
            {
                VendorItem foodItem = CurrentSetting.VendorItems.Find(i => i.Name == itemName);
                if (GetMoney < foodItem.Price / foodItem.Stack * amount)
                    return false;
            }
            return true;
        }

        public static bool PlayerIsInOutland()
        {
            return (ContinentId)Usefuls.ContinentId == ContinentId.Expansion01
                && !PlayerInBloodElfStartingZone()
                && !PlayerInDraneiStartingZone();
        }
    }
}
