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
using wManager.Wow.Bot.Tasks;

namespace PoisonMaster
{
    public class Helpers
    {
        private static bool saveWRobotSettingRepair;
        private static bool saveWRobotSettingSell;
        private static bool saveWRobotSettingTrain;

        public static int GetMoney => (int)ObjectManager.Me.GetMoneyCopper;
        
        public static bool IsHorde()
        {
            return ObjectManager.Me.Faction == (uint)PlayerFactions.Orc || ObjectManager.Me.Faction == (uint)PlayerFactions.Tauren
                || ObjectManager.Me.Faction == (uint)PlayerFactions.Undead || ObjectManager.Me.Faction == (uint)PlayerFactions.BloodElf
                || ObjectManager.Me.Faction == (uint)PlayerFactions.Troll;
        }

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
                        //Main.Logger($"{s.DisplayName} => {s.Priority}");
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
        public static void AddItemToDoNotMailList(string itemName)
        {
            if(!wManagerSetting.CurrentSetting.DoNotMailList.Contains(itemName))
            {
                wManagerSetting.CurrentSetting.DoNotMailList.Add(itemName);
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

        public static bool MailboxIsAbsent(GameObjects Object)
        {
            if (ObjectManager.GetObjectWoWGameObject().Count(x => x.Name == Object.Name) <= 0)
            {
                Main.Logger("Looks like " + Object.Name + " is not here, blacklisting");
                NPCBlackList.AddNPCToBlacklist(Object.Id);
                return true;
            }
            return false;
        }

        public static List<WoWItemQuality> GetListQualityToSell()
        {
            List<WoWItemQuality> listQualitySell = new List<WoWItemQuality>();

            if (PluginSettings.CurrentSetting.SellGrayItems)
                listQualitySell.Add(WoWItemQuality.Poor);
            if (PluginSettings.CurrentSetting.SellWhiteItems)
                listQualitySell.Add(WoWItemQuality.Common);
            if (PluginSettings.CurrentSetting.SellGreenItems)
                listQualitySell.Add(WoWItemQuality.Uncommon);
            if (PluginSettings.CurrentSetting.SellBlueItems)
                listQualitySell.Add(WoWItemQuality.Rare);
            if (PluginSettings.CurrentSetting.SellPurpleItems)
                listQualitySell.Add(WoWItemQuality.Epic);

            return listQualitySell;
        }

        public static List<WoWItemQuality> GetListQualityToMail()
        {
            List<WoWItemQuality> listQualityMail = new List<WoWItemQuality>();

            if (PluginSettings.CurrentSetting.MailGrayItems)
                listQualityMail.Add(WoWItemQuality.Poor);
            if (PluginSettings.CurrentSetting.MailWhiteItems)
                listQualityMail.Add(WoWItemQuality.Common);
            if (PluginSettings.CurrentSetting.MailGreenItems)
                listQualityMail.Add(WoWItemQuality.Uncommon);
            if (PluginSettings.CurrentSetting.MailBlueItems)
                listQualityMail.Add(WoWItemQuality.Rare);
            if (PluginSettings.CurrentSetting.MailPurpleItems)
                listQualityMail.Add(WoWItemQuality.Epic);

            return listQualityMail;
        }

        public static List<string> GetItemsToSell()
        {
            List<string> listItemsToSell = new List<string>();
            foreach (WoWItem item in Bag.GetBagItem())
            {
                if (item != null
                    && !wManagerSetting.CurrentSetting.DoNotSellList.Contains(item.Name)
                    && ShouldSellByQuality(item)
                    && item.GetItemInfo.ItemSellPrice > 0)
                    listItemsToSell.Add(item.Name);
            }
            return listItemsToSell;
        }

        public static void SellItems(DatabaseNPC vendor)
        {
            if (!PluginSettings.CurrentSetting.AllowSell)
                return;

            Main.Logger("Selling items");
            List<WoWItem> bagItems = Bag.GetBagItem();
            int nbItemsInBags = bagItems.Count;

            List<string> listItemsToSell = GetItemsToSell();

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

        private static bool ShouldSellByQuality(WoWItem item)
        {
            if (item.GetItemInfo.ItemRarity == 0 && PluginSettings.CurrentSetting.SellGrayItems) return true;
            if (item.GetItemInfo.ItemRarity == 1 && PluginSettings.CurrentSetting.SellWhiteItems) return true;
            if (item.GetItemInfo.ItemRarity == 2 && PluginSettings.CurrentSetting.SellGreenItems) return true;
            if (item.GetItemInfo.ItemRarity == 3 && PluginSettings.CurrentSetting.SellBlueItems) return true;
            if (item.GetItemInfo.ItemRarity == 4 && PluginSettings.CurrentSetting.SellPurpleItems) return true;
            return false;
        }

        public static void OverrideWRobotUserSettings()
        {
            if (PluginSettings.CurrentSetting.AllowRepair)
            {
                saveWRobotSettingRepair = wManagerSetting.CurrentSetting.Repair; // save user setting
                wManagerSetting.CurrentSetting.Repair = false; // disable user setting
            }

            if (PluginSettings.CurrentSetting.AllowSell)
            {
                saveWRobotSettingSell = wManagerSetting.CurrentSetting.Selling; // save user setting
                wManagerSetting.CurrentSetting.Selling = false; // disable user setting
            }

            if (PluginSettings.CurrentSetting.AllowTrain)
            {
                saveWRobotSettingTrain = wManagerSetting.CurrentSetting.TrainNewSkills; // save user setting
                wManagerSetting.CurrentSetting.TrainNewSkills = false; // disable user setting
            }

            wManagerSetting.CurrentSetting.Save();
        }

        public static void RestoreWRobotUserSettings()
        {
            wManagerSetting.CurrentSetting.Repair = saveWRobotSettingRepair;
            wManagerSetting.CurrentSetting.Selling = saveWRobotSettingSell;
            wManagerSetting.CurrentSetting.TrainNewSkills = saveWRobotSettingTrain;

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
            if (PluginSettings.CurrentSetting.VendorItems.Exists(i => i.Name == itemName))
            {
                PluginSettings.VendorItem vendorItem = PluginSettings.CurrentSetting.VendorItems.Find(i => i.Name == itemName);
                //Main.Logger($"We have {GetMoney} Copper on our Bank and we found Item {foodItem.Name} with the price of {foodItem.Price} and with Stacksize of {foodItem.Stack} ");
                if (GetMoney < vendorItem.Price * amount / vendorItem.Stack)
                {
                    Main.Logger($"You need {vendorItem.Price * amount / vendorItem.Stack} copper to buy {amount} x {itemName} but you only have {GetMoney}");
                    return false;
                }
            }
            return true;
        }

        public static bool PlayerIsInOutland()
        {
            return (ContinentId)Usefuls.ContinentId == ContinentId.Expansion01
                && !PlayerInBloodElfStartingZone()
                && !PlayerInDraneiStartingZone();
        }

        public static void CheckMailboxNearby(DatabaseNPC vendor)
        {
            if (!PluginSettings.CurrentSetting.AllowMail)
                return;

            Main.Logger($"Checking for a mailbox nearby {vendor.Name}");

            GameObjects mailbox = Database.GetMailboxNearby(vendor);

            if (mailbox == null)
                return;

            if (ObjectManager.Me.Position.DistanceTo(mailbox.Position) >= 10)
                GoToTask.ToPositionAndIntecractWithGameObject(mailbox.Position, mailbox.Id);

            if (ObjectManager.Me.Position.DistanceTo(mailbox.Position) < 10)
                if (MailboxIsAbsent(mailbox))
                    return;

            bool needRunAgain = true;
            for (int i = 7; i > 0 && needRunAgain; i--)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(mailbox.Position, mailbox.Id);
                Thread.Sleep(500);
                Mail.SendMessage(PluginSettings.CurrentSetting.MailingRecipient,
                    "Post",
                    "Message",
                    wManagerSetting.CurrentSetting.ForceMailList,
                    wManagerSetting.CurrentSetting.DoNotMailList,
                    GetListQualityToMail(),
                    out needRunAgain);
            }
            if (!needRunAgain)
                Main.Logger($"Sent Items to {PluginSettings.CurrentSetting.MailingRecipient}");

            Mail.CloseMailFrame();
        }
    }
}
