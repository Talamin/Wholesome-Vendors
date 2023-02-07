using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WholesomeVendors.Database.Models;
using WholesomeVendors.Managers;
using WholesomeVendors.WVSettings;
using wManager;
using wManager.Wow;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.Utils
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

        // return true if arrived
        public static bool TravelToVendorRange(
            IVendorTimerManager vendorTimerManager,
            ModelCreatureTemplate vendorTemplate,
            string reason)
        {
            //vendorTimerManager.ClearReadies();
            Vector3 vendorPosition = vendorTemplate.Creature.GetSpawnPosition;
            if (ObjectManager.Me.Position.DistanceTo(vendorPosition) >= 30)
            {
                Logger.Log(reason);
                GoToTask.ToPosition(vendorPosition, 30);
                return false;
                /*
                vendorTimerManager.AddTimerToPreviousVendor(vendorTemplate);
                if (!MovementManager.InMovement)
                {
                    Logger.Log(reason);
                    List<Vector3> pathToVendor = PathFinder.FindPath(vendorPosition);
                    MovementManager.Go(pathToVendor);
                }
                */
            }
            return true;
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
                Logger.LogError("public static void CloseWindow(): " + e);
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

        public static bool NpcIsAbsentOrDead(IBlackListManager blackListManager, ModelCreatureTemplate npc)
        {
            if (ObjectManager.GetObjectWoWUnit().Count(x => x.IsAlive && x.Entry == npc.entry) <= 0)
            {
                Logger.Log($"{npc.name} [{npc.entry}] is absent or dead, blacklisting");
                blackListManager.AddNPCToBlacklist(npc.entry);
                return true;
            }
            Logger.Log($"{npc.name} [{npc.entry}] has been found");
            return false;
        }

        public static bool MailboxIsAbsent(IBlackListManager blackListManager, ModelGameObjectTemplate mailbox)
        {
            if (ObjectManager.GetObjectWoWGameObject().Count(x => x.Name == mailbox.name) <= 0)
            {
                Logger.Log("Looks like " + mailbox.name + " is not here, blacklisting");
                blackListManager.AddNPCToBlacklist(mailbox.entry);
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

        public static void SellItems(IPluginCacheManager pluginCacheManager)
        {
            if (!PluginSettings.CurrentSetting.AllowSell || pluginCacheManager.ItemsToSell.Count <= 0)
                return;

            Logger.Log($"Found {pluginCacheManager.ItemsToSell.Count} items to sell");
            // Careful, the list of item to sell we pass actually doesn't matter,
            // it works even with an empty list and sells everything
            Vendor.SellItems(pluginCacheManager.ItemsToSell.Select(item => item.Name).ToList(), wManagerSetting.CurrentSetting.DoNotSellList, GetListQualityToSell());
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
    }
}
