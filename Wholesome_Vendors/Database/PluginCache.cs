using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WholesomeToolbox;
using WholesomeVendors.WVSettings;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.Database
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
        public static bool InLoadingScreen { get; private set; }

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
                EventsLuaWithArgs.OnEventsLuaStringWithArgs += OnEventsLuaWithArgs;
                Initialized = true;
            }
        }

        public static void Dispose()
        {
            lock (_cacheLock)
            {
                EventsLuaWithArgs.OnEventsLuaStringWithArgs -= OnEventsLuaWithArgs;
                Initialized = false;
            }
        }

        private static void OnEventsLuaWithArgs(string id, List<string> args)
        {
            switch (id)
            {
                case "BAG_UPDATE":
                    RecordBags();
                    break;
                case "PLAYER_EQUIPMENT_CHANGED":
                    RecordRangedWeaponType();
                    break;
                case "PLAYER_MONEY":
                    RecordMoney();
                    break;
                case "WORLD_MAP_UPDATE":
                case "PLAYER_ENTERING_WORLD":
                case "PLAYER_LEAVING_WORLD":
                    CacheInLoadingScreen();
                    RecordContinentAndInstancee();
                    break;
                case "SKILL_LINES_CHANGED":
                    RecordSkills();
                    break;
                case "COMPANION_LEARNED":
                    RecordKnownMounts();
                    break;
                case "COMPANION_UNLEARNED":
                    RecordKnownMounts();
                    break;
                case "INSTANCE_LOCK_STOP":
                case "COMMENTATOR_ENTER_WORLD":
                    CacheInLoadingScreen();
                    break;
            }
        }

        private static void CacheInLoadingScreen()
        {
            InLoadingScreen = true;
            Task.Run(async delegate
            {
                await Task.Delay(2000);
                InLoadingScreen = false;
            });
        }

        public static void RecordKnownMounts()
        {
            int[] mountsIds = WTItem.WotlKGetKnownMountsIds();
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
                RecordItemsToSell();
                RecordBagSlotsAndFreeSlots();
            }
        }

        private static void RecordBagSlotsAndFreeSlots()
        {
            string[] result = Lua.LuaDoString<string[]>($@"
                    local result = {{}}

                    local nbEmptyBagSlots = 0;
                    for i=0,3,1
                    do
                        local bagLink = GetContainerItemLink(0, 0-i);
                        if (bagLink == nil) then
                            nbEmptyBagSlots = nbEmptyBagSlots + 1
                        end
                    end
                    table.insert(result, nbEmptyBagSlots);

                    local nbFreeSlots = 0;
                    for i = 0, 5, 1 do
                        local numberOfFreeSlots, BagType = GetContainerNumFreeSlots(i);
                        if BagType == 0 then
                            nbFreeSlots = nbFreeSlots + numberOfFreeSlots;
                        end
                    end
                    table.insert(result, nbFreeSlots);
                    
                    return unpack(result);                    
                ");

            if (result.Length > 1)
            {
                EmptyContainerSlots = int.Parse(result[0]);
                NbFreeSlots = int.Parse(result[1]);
            }
            else
            {
                Main.LoggerError($"RecordEmptyContainerSlots() -> Couldn't unpack result!");
            }
        }

        private static void RecordContinentAndInstancee()
        {
            IsInBloodElfStartingZone = WTLocation.ZoneInBloodElfStartingZone(WTLocation.GetRealZoneText);
            IsInDraeneiStartingZone = WTLocation.ZoneInDraneiStartingZone(WTLocation.GetRealZoneText);
            IsInOutlands = WTLocation.PlayerInOutlands();
            IsInInstance = WTLocation.IsInInstance();
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
                    if (item == null
                        || !ShouldSellByQuality(item)
                        || item.GetItemInfo.ItemSellPrice <= 0)
                    {
                        continue;
                    }
                    // Don't sell items that can potentially be equipped later
                    if (item.IsEquippableItem && item.GetItemInfo.ItemEquipLoc != "INVTYPE_AMMO")
                    {
                        if (item.GetItemInfo.ItemMinLevel > ObjectManager.Me.Level && item.GetItemInfo.ItemRarity > 1)
                            WTSettings.AddItemToDoNotSellAndMailList(new List<string>() { item.Name });
                        else
                            WTSettings.RemoveItemFromDoNotSellAndMailList(new List<string>() { item.Name });
                    }

                    if (!wManagerSetting.CurrentSetting.DoNotSellList.Contains(item.Name))
                    {
                        listItemsToSell.Add(item.Name);
                    }
                }

                WTSettings.AddItemToDoNotSellAndMailList(
                    BagItems.Select(bItem => bItem.Name)
                        .Where(item => !listItemsToSell.Contains(item))
                        .ToList()
                    );
                WTSettings.RemoveItemFromDoNotSellAndMailList(listItemsToSell);

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
