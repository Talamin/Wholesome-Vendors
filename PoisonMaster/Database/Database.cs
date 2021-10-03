using DatabaseManager.Enums;
using DatabaseManager.Filter;
using DatabaseManager.Tables;
using DatabaseManager.Types;
using DatabaseManager.WoW;
using System.Collections.Generic;
using System.Linq;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Database
{
    private static CreatureFilter AmmoVendorFilter = new CreatureFilter
    {
        ExcludeIds = NPCBlackList.SessionBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction, ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
        new List<UnitNPCFlags>
        {
                UnitNPCFlags.SellsAmmo,
                UnitNPCFlags.CanSell
        }),
    };

    private static CreatureFilter FoodVendorFilter = new CreatureFilter
    {
        ExcludeIds = NPCBlackList.SessionBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction, ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.SellsFood,
                UnitNPCFlags.CanSell
            }),
    };

    private static CreatureFilter PoisonVendorFilter = new CreatureFilter
    {
        ExcludeIds = NPCBlackList.SessionBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction, ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.VENDOR_POISON
            }),
    };

    private static CreatureFilter RepairVendorFilter = new CreatureFilter
    {
        ExcludeIds = NPCBlackList.SessionBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction, ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.CanRepair
            }),
    };

    private static CreatureFilter SellVendorFilter = new CreatureFilter
    {
        ExcludeIds = NPCBlackList.SessionBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction, ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.CanSell
            }),
    };

    private static CreatureFilter TrainerFilter = new CreatureFilter
    {
        ExcludeIds = NPCBlackList.SessionBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction, ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.CanTrain
            }),
    };

    private static GameObjectFilter MailboxFilter = new GameObjectFilter
    {
        ExcludeIds = NPCBlackList.SessionBlacklist,
        Type = GameObjectType.Mailbox
    };

    public static GameObjects GetMailboxNearby(DatabaseNPC npc)
    {
        MailboxFilter.ContinentId = (ContinentId)Usefuls.ContinentId;
        gameobject Mailbox = DbGameObject
            .Get(MailboxFilter)
            .Where(q => q.Position.DistanceTo(npc.Position) <= 300)
            .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
            .FirstOrDefault();

        return Mailbox == null ? null : new GameObjects(Mailbox);
    }

    public static DatabaseNPC GetAmmoVendor(HashSet<int> usableAmmo)
    {
        AmmoVendorFilter.ContinentId = (ContinentId)Usefuls.ContinentId;
        AmmoVendorFilter.HasItems = new ItemIds(ContainedIn.Merchant, usableAmmo);
        HashSet<int> usableZones = GetListUsableZones();

        creature ammoVendor = DbCreature
            .Get(AmmoVendorFilter)
            .Where(q => usableZones.Contains(q.zoneId + 1)
                && !wManagerSetting.IsBlackListedNpcEntry(q.id))
            .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
            .FirstOrDefault();

        return ammoVendor == null ? null : new DatabaseNPC(ammoVendor);
    }

    public static DatabaseNPC GetDrinkVendor(HashSet<int> usableDrink)
    {
        FoodVendorFilter.ContinentId = (ContinentId)Usefuls.ContinentId;
        FoodVendorFilter.HasItems = new ItemIds(ContainedIn.Merchant, usableDrink);
        HashSet<int> usableZones = GetListUsableZones();
        creature drinkVendor = DbCreature
            .Get(FoodVendorFilter)
            .Where(q => !NPCBlackList.OnlyFoodBlacklist.Contains(q.id)
                && !wManagerSetting.IsBlackListedNpcEntry(q.id)
                && usableZones.Contains(q.zoneId + 1))
            .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
            .FirstOrDefault();

        return drinkVendor == null ? null : new DatabaseNPC(drinkVendor);
    }

    public static DatabaseNPC GetFoodVendor(HashSet<int> usableFood)
    {
        FoodVendorFilter.ContinentId = (ContinentId)Usefuls.ContinentId;
        FoodVendorFilter.HasItems = new ItemIds(ContainedIn.Merchant, usableFood);
        HashSet<int> usableZones = GetListUsableZones();

        creature foodVendor = DbCreature
            .Get(FoodVendorFilter)
            .Where(q => !NPCBlackList.OnlyFoodBlacklist.Contains(q.id)
                && usableZones.Contains(q.zoneId + 1)
                && !wManagerSetting.IsBlackListedNpcEntry(q.id))
            .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
            .FirstOrDefault();

        return foodVendor == null ? null : new DatabaseNPC(foodVendor);
    }

    public static DatabaseNPC GetPoisonVendor(HashSet<int> usablePoison)
    {
        PoisonVendorFilter.ContinentId = (ContinentId)Usefuls.ContinentId;
        PoisonVendorFilter.HasItems = new ItemIds(ContainedIn.Merchant, usablePoison);
        HashSet<int> usableZones = GetListUsableZones();
        creature poisonVendor = DbCreature
            .Get(PoisonVendorFilter)
            .Where(q => usableZones.Contains(q.zoneId + 1)
                && !wManagerSetting.IsBlackListedNpcEntry(q.id))
            .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
            .FirstOrDefault();

        return poisonVendor == null ? null : new DatabaseNPC(poisonVendor);
    }

    public static DatabaseNPC GetRepairVendor()
    {
        RepairVendorFilter.ContinentId = (ContinentId)Usefuls.ContinentId;
        HashSet<int> usableZones = GetListUsableZones();
        creature repairVendor = DbCreature
            .Get(RepairVendorFilter)
            .Where(q => usableZones.Contains(q.zoneId + 1)
                && !wManagerSetting.IsBlackListedNpcEntry(q.id))
            .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
            .FirstOrDefault();

        return repairVendor == null ? null : new DatabaseNPC(repairVendor);
    }

    public static DatabaseNPC GetSellVendor()
    {
        SellVendorFilter.ContinentId = (ContinentId)Usefuls.ContinentId;
        HashSet<int> usableZones = GetListUsableZones();
        creature sellVendor = DbCreature.Get(SellVendorFilter)
            .Where(q => usableZones.Contains(q.zoneId + 1)
                && !wManagerSetting.IsBlackListedNpcEntry(q.id))
            .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
            .FirstOrDefault();

        return sellVendor == null ? null : new DatabaseNPC(sellVendor);
    }

    public static DatabaseNPC GetTrainer()
    {
        TrainerFilter.ContinentId = (ContinentId)Usefuls.ContinentId;
        TrainerFilter.Trainer = (Train)ObjectManager.Me.WowClass;
        HashSet<int> usableZones = GetListUsableZones();

        creature trainer = DbCreature.Get(TrainerFilter)
            .Where(q => usableZones.Contains(q.zoneId + 1))
            .Where(q => ObjectManager.Me.Level < q.MinLevel || q.MinLevel > 20)
            .Where(q => !q.Name.Contains(" Trainer"))
            .OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position))
            .FirstOrDefault();

        return trainer == null ? null : new DatabaseNPC(trainer);
    }

    public static string GetItemName(int id)
    {
        ItemFilter filter = new ItemFilter
        {
            Ids = new HashSet<int> { id }
        };
        item_template item = DbItem.Get(filter).First();
        return item.name;
    }

    private static HashSet<int> GetListUsableZones()
    {
        HashSet<int> listZones = new HashSet<int>();
        foreach (KeyValuePair<int, int> zones in ZoneLevelDictionary)
        {
            if (zones.Value <= ObjectManager.Me.Level)
            {
                listZones.Add(zones.Key);
                //Main.Logger("Added: " + zones.Key + " to safe zones");
            }
        }
        return listZones;
    }

    private static readonly Dictionary<int, int> ZoneLevelDictionary = new Dictionary<int, int>
    {
        {465,1}, //AzuremystIsle
        {28,1}, //DunMorogh
        {5,1}, //Durotar
        {31,1}, //Elwynn
        {463,1}, //EversongWoods
        {42,1}, //Teldrassil
        {21,1}, //Tirisfal
        {10,1}, //Mulgore

        {481,5}, //SilvermoonCity
        {342,5}, //Ironforge
        {322,5}, //Ogrimmar
        {302,5}, //Stormwind
        {472,5}, //TheExodar
        {363,5}, //ThunderBluff
        {383,5}, //Undercity

        {14,10}, //Kalimdor
        {15,10}, //Azeroth

        {22,10}, //Silverpine
        {36,10}, //LochModan
        {464,10}, //Ghostlands
        {11,10}, //Barrens
        {43,10}, //Darkshore
        {477,10}, //BloodmystIsle
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

