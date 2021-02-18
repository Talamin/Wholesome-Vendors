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
    //private static ItemFilter AmmunitionFilter = new ItemFilter
    //{
    //    MinLevelRequired = (int)ObjectManager.Me.Level - 4,
    //    MaxLevelRequired = (int)ObjectManager.Me.Level,
    //    Type = new ItemClass(Projectile.Arrow)
    //};
    private static HashSet<int> ListOfZones = new HashSet<int>();

    private static CreatureFilter AmmoVendor = new CreatureFilter
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

    private static CreatureFilter BuyVendorFilter = new CreatureFilter
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

    private static CreatureFilter PoisonVendor = new CreatureFilter
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

    private static CreatureFilter repairVendorFilter = new CreatureFilter
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

    private static CreatureFilter sellVendorFilter = new CreatureFilter
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
            GetListUsableZones();
            AmmoVendor.HasItems = new ItemIds(ContainedIn.Merchant, usableAmmo);
            creature ammoVendor = DbCreature
                .Get(AmmoVendor)
                .Where(q=> ListOfZones.Contains(q.areaId))
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
            AmmoVendor.HasItems = new ItemIds(ContainedIn.Merchant, usableDrink);
            creature drinkVendor = DbCreature
                .Get(BuyVendorFilter)
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
    public static DatabaseNPC GetFoodVendor()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            creature foodVendor = DbCreature
                .Get(BuyVendorFilter)
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
    public static DatabaseNPC GetPoisonVendor()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            creature poisonVendor = DbCreature
                .Get(PoisonVendor)
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
                .Get(repairVendorFilter)
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
            creature sellVendor = DbCreature.Get(sellVendorFilter)
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

    private static void GetListUsableZones()
    {
        HashSet<int> listZones = new HashSet<int>();
        foreach(KeyValuePair<int,int> zones in ZoneLevelDictionary)
        {
            if (zones.Value <= ObjectManager.Me.Level)
            {
                listZones.Add(zones.Key);
            }
        }
        ListOfZones = listZones;
    }

    public static readonly Dictionary<int, int> ZoneLevelDictionary = new Dictionary<int, int>
    {
        {3524,1}, //AzuremystIsle
        {1,1}, //DunMorogh
        {14,1}, //Durotar
        {12,1}, //Elwynn
        {3430,1}, //EversongWoods
        {141,1}, //Teldrassil
        {85,1}, //Tirisfal
        {17,10}, //Barrens
        {3525,10}, //BloodmystIsle
        {148,10}, //Darkshore
        {3433,10}, //Ghostlands
        {1537,10}, //Ironforge
        {38,10}, //LochModan
        {215,10}, //Mulgore
        {1637,10}, //Ogrimmar
        {130,10}, //Silverpine
        {1519,10}, //Stormwind
        {3557,10}, //TheExodar
        {1638,10}, //ThunderBluff
        {1497,10}, //Undercity
        {40,10}, //Westfall
        {44,15}, //Redridge
        {406,15}, //StonetalonMountains
        {331,18}, //Ashenvale
        {10,18}, //Duskwood
        {267,20}, //Hilsbrad
        {11,20}, //Wetlands
        {400,25}, //ThousandNeedles
        {36,30}, //Alterac
        {45,30}, //Arathi
        {405,30}, //Desolace
        {15,30}, //Dustwallow
        {33,30}, //Stranglethorn
        {3,35}, //Badlands
        {8,35}, //SwampOfSorrows
        {47,40}, //Hinterlands
        {440,40}, //Tanaris
        {357,42}, //Feralas
        {16,45}, //Aszhara
        {4,45}, //BlastedLands
        {51,45}, //SearingGorge
        {361,48}, //Felwood
        {490,48}, //UngoroCrater
        {46,50}, //BurningSteppes
        {28,51}, //WesternPlaguelands
        {139,53}, //EasternPlaguelands
        {618,53}, //Winterspring
        {493,55}, //Moonglade
        {4298,55}, //ScarletEnclave
        {1377,55}, //Silithus
        {3483,58}, //Hellfire
        {3521,60}, //Zangarmarsh
        {3519,62}, //TerokkarForest
        {3522,65}, //BladesEdgeMountains
        {3518,65}, //Nagrand
        {3523,67}, //Netherstorm
        {3520,67}, //ShadowmoonValley
        {3537,68}, //BoreanTundra
        {41,68}, //DeadwindPass
        {495,68}, //HowlingFjord
        {65,71}, //Dragonblight
        {394,73}, //GrizzlyHills
        {66,75}, //ZulDrak
        {3711,76}, //SholazarBasin
        {2817,77}, //CrystalsongForest
        {4742,77}, //HrothgarsLanding
        {4812,77}, //IcecrownCitadel
        {210,77}, //IcecrownGlacier
        {4395,80}, //Dalaran

    };
}

