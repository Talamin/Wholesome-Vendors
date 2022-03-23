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

        private static object _cacheLock = new object();

        public static void Initialize()
        {
            lock(_cacheLock)
            {
                RecordBags();
                RecordRangedWeaponType();
                RecordMoney();
                EventsLua.AttachEventLua("BAG_UPDATE", m => RecordBags());
                EventsLua.AttachEventLua("PLAYER_EQUIPMENT_CHANGED", m => RecordRangedWeaponType());
                EventsLua.AttachEventLua("PLAYER_MONEY", m => RecordMoney());
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

        private static void RecordBags()
        {
            lock (_cacheLock)
            {
                RecordBagItems();
                RecordNbFreeSLots();
                RecordItemsToSell();
            }
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
