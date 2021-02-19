using DatabaseManager.Enums;
using DatabaseManager.Filter;
using DatabaseManager.Tables;
using DatabaseManager.Types;
using DatabaseManager.WoW;
using System.Collections.Generic;
using System.Linq;
using wManager.Wow.Class;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Database
{
    private static CreatureFilter AmmoVendorFilter = new CreatureFilter
    {
        ContinentId = (ContinentId)Usefuls.ContinentId,
        ExcludeIds = NPCBlackList.myBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction, ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
        new List<UnitNPCFlags>
        {
                UnitNPCFlags.SellsAmmo
        }),
    };

    private static CreatureFilter FoodVendorFilter = new CreatureFilter
    {
        ContinentId = (ContinentId)Usefuls.ContinentId,
        ExcludeIds = NPCBlackList.myBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction,ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.SellsFood
            }),
    };

    private static CreatureFilter PoisonVendorFilter = new CreatureFilter
    {
        ExcludeIds = NPCBlackList.myBlacklist,
        ContinentId = (ContinentId)Usefuls.ContinentId,
        Faction = new Faction(ObjectManager.Me.Faction, ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.VENDOR_POISON
            }),
    };

    private static CreatureFilter RepairVendorFilter = new CreatureFilter
    {
        ContinentId = (ContinentId)Usefuls.ContinentId,
        ExcludeIds = NPCBlackList.myBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction, ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.CanRepair
            }),
    };

    private static CreatureFilter SellVendorFilter = new CreatureFilter
    {
        ContinentId = (ContinentId)Usefuls.ContinentId,
        ExcludeIds = NPCBlackList.myBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction, ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.CanSell
            }),
    };

    public static DatabaseNPC GetAmmoVendor(HashSet<int> usableAmmo)
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            AmmoVendorFilter.HasItems = new ItemIds(ContainedIn.Merchant, usableAmmo);
            HashSet<int> usableZones = GetListUsableZones();
            //to be removed
            List<creature> testZone = DbCreature
                .Get(FoodVendorFilter)
                .Where(c => usableZones.Contains(c.zoneId))
                .ToList();
            testZone.ForEach(c => Main.Logger($"{c.Name} is available)"));
            //
            creature ammoVendor = DbCreature
                .Get(AmmoVendorFilter)
                .Where(q=> usableZones.Contains(q.zoneId))
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return ammoVendor == null ? null : new DatabaseNPC(ammoVendor);

        }
        else
        {
            Npc ammoVendor = NpcDB.ListNpc
                .Where(q => (int)q.Faction == ObjectManager.Me.Faction 
                    && (q.VendorItemClass == Npc.NpcVendorItemClass.Arrow || q.VendorItemClass == Npc.NpcVendorItemClass.Bullet) 
                    && q.ContinentId == (ContinentId)Usefuls.ContinentId 
                    && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
                    && q.Active)
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return ammoVendor == null ? null : new DatabaseNPC(ammoVendor);
        }
    }

    public static DatabaseNPC GetDrinkVendor(HashSet<int> usableDrink)
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            // TO BE REMOVED
            //int myZoneId = Lua.LuaDoString<int>($"return GetCurrentMapAreaID()");
            //List<creature> testZone = DbCreature
            //    .Get(FoodVendorFilter)
            //    .Where(c => c.zoneId == myZoneId - 1)
            //    .ToList();
            //testZone.ForEach(c => Main.Logger($"{c.Name} is in my zone ({c.zoneId})"));
            //

            FoodVendorFilter.HasItems = new ItemIds(ContainedIn.Merchant, usableDrink);
            creature drinkVendor = DbCreature
                .Get(FoodVendorFilter)
                .Where(q => !NPCBlackList.OnlyFoodBlacklist.Contains(q.id) && !NPCBlackList.myBlacklist.Contains(q.id))
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return drinkVendor == null ? null : new DatabaseNPC(drinkVendor);
        }
        else
        {
            Npc drinkVendor = NpcDB.ListNpc
                .Where(q => (int)q.Faction == ObjectManager.Me.Faction
                    && (q.VendorItemClass == Npc.NpcVendorItemClass.Consumable || q.VendorItemClass == Npc.NpcVendorItemClass.Food)
                    && q.ContinentId == (ContinentId)Usefuls.ContinentId
                    && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
                    && q.Active)
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return drinkVendor == null ? null : new DatabaseNPC(drinkVendor);
        }
    }
    public static DatabaseNPC GetFoodVendor(HashSet<int> usableFood)
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            FoodVendorFilter.HasItems = new ItemIds(ContainedIn.Merchant, usableFood);
            creature foodVendor = DbCreature
                .Get(FoodVendorFilter)
                .Where(q => !NPCBlackList.myBlacklist.Contains(q.id))
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return foodVendor == null ? null : new DatabaseNPC(foodVendor);
        }
        else
        {
            Npc foodVendor = NpcDB.ListNpc
                .Where(q => (int)q.Faction == ObjectManager.Me.Faction
                    && (q.VendorItemClass == Npc.NpcVendorItemClass.Consumable || q.VendorItemClass == Npc.NpcVendorItemClass.Food)
                    && q.ContinentId == (ContinentId)Usefuls.ContinentId
                    && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
                    && q.Active)
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return foodVendor == null ? null : new DatabaseNPC(foodVendor);
        }
    }
    public static DatabaseNPC GetPoisonVendor(HashSet<int> usablePoison)
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            PoisonVendorFilter.HasItems = new ItemIds(ContainedIn.Merchant, usablePoison);
            creature poisonVendor = DbCreature
                .Get(PoisonVendorFilter)
                .Where(q => !NPCBlackList.myBlacklist.Contains(q.id))
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return poisonVendor == null ? null : new DatabaseNPC(poisonVendor);
        }
        else
        {
            Npc poisonVendor = NpcDB.ListNpc
                .Where(q => (int)q.Faction == ObjectManager.Me.Faction
                    && (q.VendorItemClass == Npc.NpcVendorItemClass.Potion)
                    && q.ContinentId == (ContinentId)Usefuls.ContinentId
                    && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
                    && q.Active)
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return poisonVendor == null ? null : new DatabaseNPC(poisonVendor);
        }
    }
    public static DatabaseNPC GetRepairVendor()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            creature repairVendor = DbCreature
                .Get(RepairVendorFilter)
                .Where(q => !NPCBlackList.myBlacklist.Contains(q.id))
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return repairVendor == null ? null : new DatabaseNPC(repairVendor);
        }
        else
        {
            Npc repairVendor = NpcDB.ListNpc.Where(q => (int)q.Faction == ObjectManager.Me.Faction
                    && (q.Type == Npc.NpcType.Repair)
                    && q.ContinentId == (ContinentId)Usefuls.ContinentId
                    && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
                    && q.Active)
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return repairVendor == null ? null : new DatabaseNPC(repairVendor);
        }
    }
    public static DatabaseNPC GetSellVendor()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            creature sellVendor = DbCreature.Get(SellVendorFilter)
                .Where(q => !NPCBlackList.myBlacklist.Contains(q.id))
                .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
                .FirstOrDefault();

            return sellVendor == null ? null : new DatabaseNPC(sellVendor);
        }
        else
        {
            Npc sellVendor = NpcDB.ListNpc.Where(q => (int)q.Faction == ObjectManager.Me.Faction
                    && (q.Type == Npc.NpcType.Repair || q.Type == Npc.NpcType.Vendor)
                    && q.ContinentId == (ContinentId)Usefuls.ContinentId
                    && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
                    && q.Active)
            .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
            .FirstOrDefault();

            return sellVendor == null ? null : new DatabaseNPC(sellVendor);
        }
    }

    private static HashSet<int> GetListUsableZones()
    {
        HashSet<int> listZones = new HashSet<int>();
        foreach(KeyValuePair<int,int> zones in ZoneLevelDictionary)
        {
            if (zones.Value <= ObjectManager.Me.Level)
            {
                listZones.Add(zones.Key);
            }
        }
        return listZones;
    }

    private static readonly Dictionary<int, int> ZoneLevelDictionary = new Dictionary<int, int>
    {
        {14,10 }, //Kalimdor
        {15,10}, //Azeroth
        {465,1}, //AzuremystIsle
        {28,1}, //DunMorogh
        {5,1}, //Durotar
        {31,1}, //Elwynn
        {463,1}, //EversongWoods
        {42,1}, //Teldrassil
        {21,1}, //Tirisfal
        {481,10 }, //SilvermoonCity
        {11,10}, //Barrens
        {477,10}, //BloodmystIsle
        {43,10}, //Darkshore
        {464,10}, //Ghostlands
        {342,10}, //Ironforge
        {36,10}, //LochModan
        {10,10}, //Mulgore
        {322,10}, //Ogrimmar
        {22,10}, //Silverpine
        {302,10}, //Stormwind
        {472,10}, //TheExodar
        {363,10}, //ThunderBluff
        {383,10}, //Undercity
        {40,10}, //Westfall
        {37,15}, //Redridge
        {82,15}, //StonetalonMountains
        {44,18}, //Ashenvale
        {35,18}, //Duskwood
        {25,20}, //Hilsbrad
        {41,20}, //Wetlands
        {62,25}, //ThousandNeedles
        {16,30}, //Alterac
        {17,30}, //Arathi
        {102,30}, //Desolace
        {142,30}, //Dustwallow
        {38,30}, //Stranglethorn
        {18,35}, //Badlands
        {39,35}, //SwampOfSorrows
        {27,40}, //Hinterlands
        {162,40}, //Tanaris
        {122,42}, //Feralas
        {182,45}, //Aszhara
        {20,45}, //BlastedLands
        {29,45}, //SearingGorge
        {183,48}, //Felwood
        {202,48}, //UngoroCrater
        {30,50}, //BurningSteppes
        {23,51}, //WesternPlaguelands
        {24,53}, //EasternPlaguelands
        {282,53}, //Winterspring
        {242,55}, //Moonglade
        {262,55}, //Silithus
        {466,58}, //Hellfire
        {467,60}, //Zangarmarsh
        {479,62}, //TerokkarForest
        {476,65}, //BladesEdgeMountains
        {478,65}, //Nagrand
        {480,67}, //Netherstorm
        {474,67}, //ShadowmoonValley
        {482,65}, //ShattrathCity
        {487,68}, //BoreanTundra
        {32,68}, //DeadwindPass
        {492,68}, //HowlingFjord
        {489,71}, //Dragonblight
        {491,73}, //GrizzlyHills
        {497,75}, //ZulDrak
        {494,76}, //SholazarBasin
        {511,77}, //CrystalsongForest
        {542,77}, //HrothgarsLanding
        {605,77}, //IcecrownCitadel
        {505,80}, //Dalaran
    };
}

