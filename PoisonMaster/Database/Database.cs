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

    public static DatabaseNPC GetAmmoVendor()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            AmmoVendor.HasItems = new ItemIds(ContainedIn.Merchant, BuyAmmoState.BuyingAmmuniton);
            creature ammoVendor = DbCreature
                .Get(AmmoVendor)
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

    public static DatabaseNPC GetDrinkVendor()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
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

    public readonly Dictionary<int, string> ArrowDictionary = new Dictionary<int, string>
    {
        { 80,"Zone"},
    };
}

