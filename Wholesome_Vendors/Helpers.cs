using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WholesomeVendors.Blacklist;
using WholesomeVendors.Database;
using WholesomeVendors.Database.Models;
using WholesomeVendors.WVSettings;
using wManager;
using wManager.Wow;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors
{
    public class Helpers
    {
        private static bool saveWRobotSettingRepair;
        private static bool saveWRobotSettingSell;
        private static bool saveWRobotSettingTrain;

        public static bool UsingDungeonProduct()
        {
            return Products.ProductName.ToLower().Trim().Replace(" ", "")
                == "Wholesome Dungeon Crawler".ToLower().Trim().Replace(" ", "");
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

        public static void AddItemToDoNotSellAndMailList(string itemName)
        {
            if (!wManagerSetting.CurrentSetting.DoNotSellList.Contains(itemName))
            {
                wManagerSetting.CurrentSetting.DoNotSellList.Add(itemName);
            }
            if (!wManagerSetting.CurrentSetting.DoNotMailList.Contains(itemName))
            {
                wManagerSetting.CurrentSetting.DoNotMailList.Add(itemName);
            }
            wManagerSetting.CurrentSetting.Save();
        }

        public static void RemoveItemFromDoNotSellAndMailList(List<ModelItemTemplate> items)
        {
            foreach (ModelItemTemplate item in items)
            {
                if (wManagerSetting.CurrentSetting.DoNotSellList.Contains(item.Name))
                {
                    wManagerSetting.CurrentSetting.DoNotSellList.Remove(item.Name);
                }
                if (wManagerSetting.CurrentSetting.DoNotMailList.Contains(item.Name))
                {
                    wManagerSetting.CurrentSetting.DoNotMailList.Remove(item.Name);
                }
            }
            wManagerSetting.CurrentSetting.Save();
        }

        public static void RemoveItemFromDoNotSellAndMailList(string itemName)
        {
            if (wManagerSetting.CurrentSetting.DoNotSellList.Contains(itemName))
            {
                wManagerSetting.CurrentSetting.DoNotSellList.Remove(itemName);
            }
            if (wManagerSetting.CurrentSetting.DoNotMailList.Contains(itemName))
            {
                wManagerSetting.CurrentSetting.DoNotMailList.Remove(itemName);
            }
            wManagerSetting.CurrentSetting.Save();
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

        public static void SellItems()
        {
            if (!PluginSettings.CurrentSetting.AllowSell || PluginCache.ItemsToSell.Count <= 0)
                return;

            Main.Logger($"Found {PluginCache.ItemsToSell.Count} items to sell");
            Vendor.SellItems(PluginCache.ItemsToSell, wManagerSetting.CurrentSetting.DoNotSellList, GetListQualityToSell());
            Thread.Sleep(1000);
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

        public static bool HaveEnoughMoneyFor(int amount, ModelItemTemplate item) => PluginCache.Money >= item.BuyPrice * amount / item.BuyCount;

        public static void CheckMailboxNearby(ModelCreatureTemplate npc)
        {
            if (!PluginSettings.CurrentSetting.AllowMail)
                return;

            Main.Logger($"Checking for a mailbox nearby {npc.name}");

            ModelGameObjectTemplate mailbox = MemoryDB.GetNearestMailBoxFrom(npc);

            if (mailbox == null)
            {
                Main.Logger($"Couldn't find a mailbox nearby {npc.name}");
                return; 
            }
            else
            {
                Main.Logger($"Sending mail to {PluginSettings.CurrentSetting.MailingRecipient}");
            }

            if (ObjectManager.Me.Position.DistanceTo(mailbox.GameObject.GetSpawnPosition) >= 10)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(mailbox.GameObject.GetSpawnPosition, mailbox.entry);
            }

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
    }
}
