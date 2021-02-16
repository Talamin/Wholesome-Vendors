using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace PoisonMaster
{
    class Helpers
    {
        public static bool OutOfFoodVar;
        public static bool OutOfDrinkVar;
        public static List<WoWItem> EquippedRanged;
        public static string RangedWeaponType = "";
        internal static int Money => (int)ObjectManager.Me.GetMoneyCopper;
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
                        Logging.WriteDebug($"Couldn't find state {replace}");
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
                    Logging.Write($"Adding state {state.DisplayName} with prio {priorityToSet}");
                    engine.AddState(state);
                    engine.States.Sort();
                }
                catch (Exception ex)
                {
                    Logging.WriteDebug("Erreur : {0}" + ex.ToString());
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
                Logging.WriteError("public static void CloseWindow(): " + e);
            }
            finally
            {
                Memory.WowMemory.UnlockFrame();
            }
        }

        private enum ConsumableType
        {
            Food,
            Drink
        }
        public static bool OutOfFood()
        {
            var allFoodAmount = Bag.GetBagItem()
                .Where(
                    i => ConsumableType.Food.ToString() == ItemsManager.GetItemSpell(i.Name) &&
                         i.GetItemInfo.ItemMinLevel <= ObjectManager.Me.Level)
                .Select(i => ItemsManager.GetItemCountById((uint)i.Entry))
                .Aggregate(0, (i, i2) => i + i2);

            if (allFoodAmount < 10 && wManagerSetting.CurrentSetting.FoodAmount > 0)
            {
                Logging.WriteDebug("Food: " + allFoodAmount);
                return true;
            }

            return false;
        }

        public static bool OutOfDrink()
        {
            var allDrinkAmount = Bag.GetBagItem()
                .Where(
                    i => ConsumableType.Drink.ToString() == ItemsManager.GetItemSpell(i.Name) &&
                         i.GetItemInfo.ItemMinLevel <= ObjectManager.Me.Level)
                .Select(i => ItemsManager.GetItemCountById((uint)i.Entry))
                .Aggregate(0, (i, i2) => i + i2);


            if (allDrinkAmount < 10 && wManagerSetting.CurrentSetting.DrinkAmount > 0)
            {
                Logging.WriteDebug("Drinks: " + allDrinkAmount);
                return true;
            }

            return false;
        }

        public static bool HaveRanged()
        {
            if (ObjectManager.Me.GetEquipedItemBySlot(InventorySlot.INVSLOT_RANGED) != 0)
            {
                return true;
            }
            return false;  
        }

        public static void CheckEquippedItems()
        {
            if(HaveRanged())
            {
                EquippedRanged = EquippedItems.GetEquippedItems();
                foreach (WoWItem equippedItem in EquippedRanged)
                {
                    if (equippedItem.GetItemInfo.ItemSubType == "Crossbows" || equippedItem.GetItemInfo.ItemSubType == "Bows")
                    {
                        RangedWeaponType = "Bows";
                    }
                    if (equippedItem.GetItemInfo.ItemSubType == "Guns")
                    {
                        RangedWeaponType = "Guns";
                    }
                }
            }
        }
    }
}
