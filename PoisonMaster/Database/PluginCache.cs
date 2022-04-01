using PoisonMaster;
using System.Collections.Generic;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Vendors.Database
{
    public class PluginCache
    {
        public static List<WoWItem> BagItems { get; private set; }
        public static int NbFreeSlots { get; private set; }
        public static List<string> ItemsToSell { get; private set; }
        public static bool Initialized { get; private set; }
        public static string RangedWeaponType { get; private set; }
        public static int Money { get; private set; }
        public static int EmptyContainerSlots { get; private set; }
        public static bool IsInInstance { get; private set; }
        public static bool IsInBloodElfStartingZone { get; private set; }
        public static bool IsInDraeneiStartingZone { get; private set; }
        public static bool IsInOutlands { get; private set; }
        public static int RidingSkill { get; private set; }
        public static List<int> KnownMountSpells { get; private set; } = new List<int>();

        public static bool Know75Mount => KnownMountSpells.Exists(ms => MemoryDB.GetNormalMounts.Exists(nm => nm.Id == ms));
        public static bool Know150Mount => KnownMountSpells.Exists(ms => MemoryDB.GetEpicMounts.Exists(nm => nm.Id == ms));
        public static bool Know225Mount => KnownMountSpells.Exists(ms => MemoryDB.GetFlyingMounts.Exists(nm => nm.Id == ms));
        public static bool Know300Mount => KnownMountSpells.Exists(ms => MemoryDB.GetEpicFlyingMounts.Exists(nm => nm.Id == ms));

        private static object _cacheLock = new object();

        public static void Initialize()
        {
            lock (_cacheLock)
            {
                RecordBags();
                RecordRangedWeaponType();
                RecordMoney();
                RecordContinentAndInstancee();
                RecordSkills();
                EventsLua.AttachEventLua("BAG_UPDATE", m => RecordBags());
                EventsLua.AttachEventLua("PLAYER_EQUIPMENT_CHANGED", m => RecordRangedWeaponType());
                EventsLua.AttachEventLua("PLAYER_MONEY", m => RecordMoney());
                EventsLua.AttachEventLua("WORLD_MAP_UPDATE", m => RecordContinentAndInstancee());
                EventsLua.AttachEventLua("SKILL_LINES_CHANGED", m => RecordSkills());
                EventsLua.AttachEventLua("COMPANION_LEARNED", m => RecordKnownMounts());
                EventsLua.AttachEventLua("COMPANION_UNLEARNED", m => RecordKnownMounts());
                Initialized = true;
            }
        }

        public static void Dispose()
        {
            lock (_cacheLock)
            {
                Initialized = false;
            }
        }

        public static void RecordKnownMounts()
        {
            int[] mountsIds = Lua.LuaDoString<int[]>($@"
                local numComp  = GetNumCompanions('MOUNT');
                local result = {{}};
                for i=1, numComp, 1 do
                    local creatureID, creatureName, spellID, icon, active = GetCompanionInfo('MOUNT', i);
                    result[i] = spellID;
                end
                return unpack(result);
            ");
            foreach (int id in mountsIds)
            {
                if (id > 0 && !KnownMountSpells.Contains(id))
                    KnownMountSpells.Add(id);
            }
        }

        private static void RecordSkills()
        {
            RidingSkill = Skill.GetValue(SkillLine.Riding);
        }

        private static void RecordBags()
        {
            lock (_cacheLock)
            {
                RecordBagItems();
                RecordNbFreeSLots();
                RecordItemsToSell();
                RecordEmptyContainerSlots();
            }
        }

        private static void RecordEmptyContainerSlots()
        {
            lock (_cacheLock)
            {
                int result = 0;
                for (int i = 0; i < 5; i++)
                {
                    string bagName = Lua.LuaDoString<string>($"return GetBagName({i});");
                    if (bagName.Equals(""))
                        result++;
                }
                EmptyContainerSlots = result;
            }
        }

        private static void RecordContinentAndInstancee()
        {
            IsInBloodElfStartingZone = Helpers.PlayerInBloodElfStartingZone();
            IsInDraeneiStartingZone = Helpers.PlayerInDraneiStartingZone();
            IsInOutlands = Helpers.PlayerIsInOutland();
            IsInInstance = Lua.LuaDoString<bool>($@"
                    local isInstance, instanceType = IsInInstance();
                    return instanceType ~= 'none';
                ");
        }

        private static void RecordMoney()
        {
            lock (_cacheLock)
            {
                Money = (int)ObjectManager.Me.GetMoneyCopper;
            }
        }

        private static void RecordRangedWeaponType()
        {
            lock (_cacheLock)
            {
                uint myRangedWeapon = ObjectManager.Me.GetEquipedItemBySlot(InventorySlot.INVSLOT_RANGED);
                if (myRangedWeapon != 0)
                {
                    List<WoWItem> equippedItems = EquippedItems.GetEquippedItems();
                    foreach (WoWItem equippedItem in equippedItems)
                    {
                        if (equippedItem.GetItemInfo.ItemSubType == "Crossbows" || equippedItem.GetItemInfo.ItemSubType == "Bows")
                        {
                            RangedWeaponType = "Bows";
                            return;
                        }
                        if (equippedItem.GetItemInfo.ItemSubType == "Guns")
                        {
                            RangedWeaponType = "Guns";
                            return;
                        }
                    }
                }
                RangedWeaponType = null;
            }
        }

        private static void RecordNbFreeSLots()
        {
            lock (_cacheLock)
            {
                NbFreeSlots = Bag.GetContainerNumFreeSlotsByType(BagType.Unspecified);
            }
        }

        private static void RecordBagItems()
        {
            lock (_cacheLock)
            {
                BagItems = Bag.GetBagItem();
            }
        }

        private static void RecordItemsToSell()
        {
            lock (_cacheLock)
            {
                List<string> listItemsToSell = new List<string>();
                foreach (WoWItem item in BagItems)
                {
                    if (item != null
                        && !wManagerSetting.CurrentSetting.DoNotSellList.Contains(item.Name)
                        && ShouldSellByQuality(item)
                        && item.GetItemInfo.ItemSellPrice > 0)
                    {
                        listItemsToSell.Add(item.Name);
                    }
                }
                ItemsToSell = listItemsToSell;
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
    }
}
