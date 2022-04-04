using robotManager.FiniteStateMachine;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Vendors.Database;
using Wholesome_Vendors.Database.Models;
using wManager;
using wManager.Wow;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace PoisonMaster
{
    public class Helpers
    {
        private static bool saveWRobotSettingRepair;
        private static bool saveWRobotSettingSell;
        private static bool saveWRobotSettingTrain;

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
                Lua.LuaDoString("CloseMerchant()");
                Lua.LuaDoString("CloseLoot()");
                Lua.LuaDoString("CloseQuest()");
                Lua.LuaDoString("CloseTrainer()");
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

        public enum Factions
        {
            Unknown = 0,
            Human = 1,
            Orc = 2,
            Dwarf = 4,
            NightElf = 8,
            Undead = 16,
            Tauren = 32,
            Gnome = 64,
            Troll = 128,
            Goblin = 256,
            BloodElf = 512,
            Draenei = 1024,
            Worgen = 2097152
        }

        public static Factions GetFactions()
        {
            switch ((PlayerFactions)ObjectManager.Me.Faction)
            {
                case PlayerFactions.Human: return Factions.Human;
                case PlayerFactions.Orc: return Factions.Orc;
                case PlayerFactions.Dwarf: return Factions.Dwarf;
                case PlayerFactions.NightElf: return Factions.NightElf;
                case PlayerFactions.Undead: return Factions.Undead;
                case PlayerFactions.Tauren: return Factions.Tauren;
                case PlayerFactions.Gnome: return Factions.Gnome;
                case PlayerFactions.Troll: return Factions.Troll;
                case PlayerFactions.Goblin: return Factions.Goblin;
                case PlayerFactions.BloodElf: return Factions.BloodElf;
                case PlayerFactions.Draenei: return Factions.Draenei;
                case PlayerFactions.Worgen: return Factions.Worgen;
                default: throw new Exception($"Couldn't get your faction");
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
            if (!wManagerSetting.CurrentSetting.DoNotMailList.Contains(itemName))
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

        public static bool NpcIsAbsentOrDead(ModelCreatureTemplate npc)
        {
            if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Name == npc.name) <= 0)
            {
                Main.Logger("Looks like " + npc.name + " is not here, blacklisting");
                NPCBlackList.AddNPCToBlacklist(npc.entry);
                return true;
            }
            return false;
        }

        public static bool MailboxIsAbsent(ModelGameObjectTemplate mailbox)
        {
            if (ObjectManager.GetObjectWoWGameObject().Count(x => x.Name == mailbox.name) <= 0)
            {
                Main.Logger("Looks like " + mailbox.name + " is not here, blacklisting");
                NPCBlackList.AddNPCToBlacklist(mailbox.entry);
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

        public static void SellItems(ModelCreatureTemplate vendor)
        {
            if (!PluginSettings.CurrentSetting.AllowSell || PluginCache.ItemsToSell.Count <= 0)
                return;

           // Main.Logger("Selling items");
            //List<WoWItem> bagItems = PluginCache.BagItems;
            //int nbItemsInBags = bagItems.Count;

            //List<string> listItemsToSell = PluginCache.ItemsToSell;

            //if (PluginCache.ItemsToSell.Count <= 0)
              //  return;

            Main.Logger($"Found {PluginCache.ItemsToSell.Count} items to sell");

            //for (int i = 1; i <= 5; i++)
            //{
                //Main.Logger($"Attempt {i}");
                //GoToTask.ToPositionAndIntecractWithNpc(vendor.Creature.GetSpawnPosition, vendor.entry, i);
                Vendor.SellItems(PluginCache.ItemsToSell, wManagerSetting.CurrentSetting.DoNotSellList, GetListQualityToSell());
                Thread.Sleep(1000);
            /*
                if (PluginCache.BagItems.Count < nbItemsInBags)
                {
                    break;
                }
                if (i >= 5)
                {
                    Main.Logger($"Failed to sell items after {i} attempts. Blacklisting vendor.");
                    NPCBlackList.AddNPCToBlacklist(vendor.entry);
                }
            //}*/
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

        public static bool IsVendorGossipOpen()
        {
            return Lua.LuaDoString<int>("return GetMerchantNumItems()") > 0;
        }

        public static bool HaveEnoughMoneyFor(int amount, ModelItemTemplate item) => PluginCache.Money >= (item.BuyPrice * amount / item.BuyCount);

        public static bool PlayerIsInOutland()
        {
            return (ContinentId)Usefuls.ContinentId == ContinentId.Expansion01
                && !PlayerInBloodElfStartingZone()
                && !PlayerInDraneiStartingZone();
        }

        public static void CheckMailboxNearby(ModelCreatureTemplate npc)
        {
            if (!PluginSettings.CurrentSetting.AllowMail)
                return;

            Main.Logger($"Checking for a mailbox nearby {npc.name}");

            ModelGameObjectTemplate mailbox = MemoryDB.GetNearestMailBoxFrom(npc);

            if (mailbox == null)
                return;

            if (ObjectManager.Me.Position.DistanceTo(mailbox.GameObject.GetSpawnPosition) >= 10)
                GoToTask.ToPositionAndIntecractWithGameObject(mailbox.GameObject.GetSpawnPosition, mailbox.entry);

            if (ObjectManager.Me.Position.DistanceTo(mailbox.GameObject.GetSpawnPosition) < 10
                && MailboxIsAbsent(mailbox))
                return;

            bool needRunAgain = true;
            for (int i = 7; i > 0 && needRunAgain; i--)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(mailbox.GameObject.GetSpawnPosition, mailbox.entry);
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

        public static void LearnSpellByName(string spellName)
        {
            Main.Logger($"Learning spell {spellName}");
            Lua.LuaDoString($@"
                for i=1,GetNumTrainerServices() do
                    local name = GetTrainerServiceInfo(i)
                    if (name == '{spellName}') then
                        BuyTrainerService(i)
                     end
                end
            ");
        }
    }
}
