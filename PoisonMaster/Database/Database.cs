using DatabaseManager.Enums;
using DatabaseManager.Filter;
using DatabaseManager.Types;
using DatabaseManager.WoW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wManager.Wow.Class;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Database
{
    public static DatabaseNPC AmmoVendors = new DatabaseNPC();
    public static DatabaseNPC BuyVendorsDrink = new DatabaseNPC();
    public static DatabaseNPC BuyVendorsFood = new DatabaseNPC();
    public static DatabaseNPC BuyVendorsPoison = new DatabaseNPC();
    public static DatabaseNPC VendorsRepair = new DatabaseNPC();
    public static DatabaseNPC VendorsSell = new DatabaseNPC();

    private static CreatureFilter AmmoVendor = new CreatureFilter
    {
        ContinentId = (ContinentId)Usefuls.ContinentId,
        ExcludeIds = Blacklist.myBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction,
        ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
        new List<UnitNPCFlags>
        {
                UnitNPCFlags.SellsAmmo
        })
    };
    private static CreatureFilter BuyVendorFilter = new CreatureFilter
    {
        ContinentId = (ContinentId)Usefuls.ContinentId,
        ExcludeIds = Blacklist.myBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction,
            ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.SellsFood
            }),
    };
    private static CreatureFilter PoisonVendor = new CreatureFilter
    {
        ExcludeIds = Blacklist.myBlacklist,
        ContinentId = (ContinentId)Usefuls.ContinentId,
        Faction = new Faction(ObjectManager.Me.Faction,
            ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.VENDOR_POISON
            }),
    };
    private static CreatureFilter repairVendorFilter = new CreatureFilter
    {
        ContinentId = (ContinentId)Usefuls.ContinentId,
        ExcludeIds = Blacklist.myBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction,
            ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.CanRepair
            }),
    };
    private static CreatureFilter sellVendorFilter = new CreatureFilter
    {
        ContinentId = (ContinentId)Usefuls.ContinentId,
        ExcludeIds = Blacklist.myBlacklist,
        Faction = new Faction(ObjectManager.Me.Faction,
            ReactionType.Friendly),
        NpcFlags = new NpcFlag(Operator.Or,
            new List<UnitNPCFlags>
            {
                UnitNPCFlags.CanSell
            }),
    };

    public static void ChooseDatabaseAmmoNPC()
    {
        if(PluginSettings.CurrentSetting.Databasetype =="external")
        {
            var ammoVendor = DbCreature.
                Get(AmmoVendor).
                OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
                First();
            AmmoVendors.id = ammoVendor.id;
            AmmoVendors.Position = ammoVendor.Position;
            AmmoVendors.Name = ammoVendor.Name;
        }
        if(PluginSettings.CurrentSetting.Databasetype == "internal")
        {
            var ammoVendor = NpcDB.ListNpc.Where(q => (int)q.Faction == ObjectManager.Me.Faction 
            && (q.VendorItemClass == Npc.NpcVendorItemClass.Arrow || q.VendorItemClass == Npc.NpcVendorItemClass.Bullet) 
            && q.ContinentId == (ContinentId)Usefuls.ContinentId 
            && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
            && q.Active).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            FirstOrDefault();
            AmmoVendors.id = ammoVendor.Entry;
            AmmoVendors.Position = ammoVendor.Position;
            AmmoVendors.Name = ammoVendor.Name;
        }
    }

    public static void ChooseDatabaseBuyVendorDrinkNPC()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            var buyVendor = DbCreature.Get(BuyVendorFilter).
            Where(q => !Blacklist.OnlyFoodBlacklist.Contains(q.id) && !Blacklist.myBlacklist.Contains(q.id)).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            First();
            BuyVendorsDrink.id = buyVendor.id;
            BuyVendorsDrink.Position = buyVendor.Position;
            BuyVendorsDrink.Name = buyVendor.Name;
        }
        if (PluginSettings.CurrentSetting.Databasetype == "internal")
        {
            var buyVendor = NpcDB.ListNpc.Where(q => (int)q.Faction == ObjectManager.Me.Faction
            && (q.VendorItemClass == Npc.NpcVendorItemClass.Consumable || q.VendorItemClass == Npc.NpcVendorItemClass.Food)
            && q.ContinentId == (ContinentId)Usefuls.ContinentId
            && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
            && q.Active).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            FirstOrDefault();
            BuyVendorsDrink.id = buyVendor.Entry;
            BuyVendorsDrink.Position = buyVendor.Position;
            BuyVendorsDrink.Name = buyVendor.Name;
        }
    }
    public static void ChooseDatabaseBuyVendorFoodNPC()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            var buyVendor = DbCreature.Get(BuyVendorFilter).
            Where(q => !Blacklist.myBlacklist.Contains(q.id)).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            First();
            BuyVendorsFood.id = buyVendor.id;
            BuyVendorsFood.Position = buyVendor.Position;
            BuyVendorsFood.Name = buyVendor.Name;
        }
        if (PluginSettings.CurrentSetting.Databasetype == "internal")
        {
            var buyVendor = NpcDB.ListNpc.Where(q => (int)q.Faction == ObjectManager.Me.Faction
            && (q.VendorItemClass == Npc.NpcVendorItemClass.Consumable || q.VendorItemClass == Npc.NpcVendorItemClass.Food)
            && q.ContinentId == (ContinentId)Usefuls.ContinentId
            && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
            && q.Active).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            FirstOrDefault();
            BuyVendorsFood.id = buyVendor.Entry;
            BuyVendorsFood.Position = buyVendor.Position;
            BuyVendorsFood.Name = buyVendor.Name;
        }
    }
    public static void ChooseDatabaseBuyVendorPoisonNPC()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            var buyVendor = DbCreature.Get(PoisonVendor).
            Where(q => !Blacklist.myBlacklist.Contains(q.id)).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            First();
            BuyVendorsPoison.id = buyVendor.id;
            BuyVendorsPoison.Position = buyVendor.Position;
            BuyVendorsPoison.Name = buyVendor.Name;
        }
        if (PluginSettings.CurrentSetting.Databasetype == "internal")
        {
            var buyVendor = NpcDB.ListNpc.Where(q => (int)q.Faction == ObjectManager.Me.Faction
            && (q.VendorItemClass == Npc.NpcVendorItemClass.Potion)
            && q.ContinentId == (ContinentId)Usefuls.ContinentId
            && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
            && q.Active).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            FirstOrDefault();
            BuyVendorsPoison.id = buyVendor.Entry;
            BuyVendorsPoison.Position = buyVendor.Position;
            BuyVendorsPoison.Name = buyVendor.Name;
        }
    }
    public static void ChooseDatabaseVendorRepairNPC()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            var repairVendor = DbCreature.Get(repairVendorFilter).
            Where(q => !Blacklist.myBlacklist.Contains(q.id)).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            First();
            VendorsRepair.id = repairVendor.id;
            VendorsRepair.Position = repairVendor.Position;
            VendorsRepair.Name = repairVendor.Name;
        }
        if (PluginSettings.CurrentSetting.Databasetype == "internal")
        {
            var repairVendor = NpcDB.ListNpc.Where(q => (int)q.Faction == ObjectManager.Me.Faction
            && (q.Type == Npc.NpcType.Repair)
            && q.ContinentId == (ContinentId)Usefuls.ContinentId
            && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
            && q.Active).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            FirstOrDefault();
            VendorsRepair.id = repairVendor.Entry;
            VendorsRepair.Position = repairVendor.Position;
            VendorsRepair.Name = repairVendor.Name;
        }
    }
    public static void ChooseDatabaseSellVendorNPC()
    {
        if (PluginSettings.CurrentSetting.Databasetype == "external")
        {
            var sellVendor = DbCreature.Get(sellVendorFilter).
            Where(q => !Blacklist.myBlacklist.Contains(q.id)).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            First();
            VendorsSell.id = sellVendor.id;
            VendorsSell.Position = sellVendor.Position;
            VendorsSell.Name = sellVendor.Name;
        }
        if (PluginSettings.CurrentSetting.Databasetype == "internal")
        {
            var sellVendor = NpcDB.ListNpc.Where(q => (int)q.Faction == ObjectManager.Me.Faction
            && (q.Type == Npc.NpcType.Repair || q.Type == Npc.NpcType.Vendor)
            && q.ContinentId == (ContinentId)Usefuls.ContinentId
            && !wManager.wManagerSetting.IsBlackListedNpcEntry(q.Entry)
            && q.Active).
            OrderBy(q => ObjectManager.Me.Position.DistanceTo(q.Position)).
            FirstOrDefault();
            VendorsSell.id = sellVendor.Entry;
            VendorsSell.Position = sellVendor.Position;
            VendorsSell.Name = sellVendor.Name;
        }
    }
}

