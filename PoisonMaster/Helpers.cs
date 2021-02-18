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

namespace PoisonMaster
{
    public class Helpers
    {
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
                        if (s.Priority >= priorityToSet)
                            s.Priority++;
                    }

                    state.Priority = priorityToSet;
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

        public static bool OutOfFood()
        {
            var allFoodAmount = Bag.GetBagItem()
                .Where(
                    i => PMConsumableType.Food.ToString() == ItemsManager.GetItemSpell(i.Name) &&
                         i.GetItemInfo.ItemMinLevel <= ObjectManager.Me.Level)
                .Select(i => ItemsManager.GetItemCountById((uint)i.Entry))
                .Aggregate(0, (i, i2) => i + i2);

            //Main.Logger("Food in total: " + allFoodAmount);

            if (allFoodAmount < 1 && wManagerSetting.CurrentSetting.FoodAmount > 0)
            {
                //Main.Logger("Food: " + allFoodAmount);
                return true;
            }

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

            //Main.Logger("Drinks in total: " + allDrinkAmount);
            if (allDrinkAmount < 1 && wManagerSetting.CurrentSetting.DrinkAmount > 0)
            {
                //Main.Logger("Drinks: " + allDrinkAmount);
                return true;
            }

            return false;
        }

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

        public static void SoftRestart()
        {
            Products.InPause = true;
            Thread.Sleep(100);
            Products.InPause = false;
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
            Main.Logger("Buying " + amount + " " + name);
            Lua.LuaDoString(string.Format(@"
                    local itemName = ""{0}""
                    local quantity = {1}
                    for i=1, GetMerchantNumItems() do
                        local name = GetMerchantItemInfo(i)
                        if name and name == itemName then 
                            BuyMerchantItem(i, quantity)
                        end
                    end", name, amount / stackValue));
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
    }
}
