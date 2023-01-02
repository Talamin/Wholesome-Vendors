using Dapper;
using Newtonsoft.Json;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WholesomeToolbox;
using WholesomeVendors.Blacklist;
using WholesomeVendors.Database.Models;
using WholesomeVendors.WVSettings;
using wManager;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.Database
{
    class MemoryDB
    {
        private static SQLiteConnection _con;
        private static SQLiteCommand _cmd;

        private static List<ModelItemTemplate> _drinks;
        private static List<ModelItemTemplate> _foods;
        private static List<ModelItemTemplate> _ammos;
        private static List<ModelItemTemplate> _poisons;
        private static List<ModelItemTemplate> _bags;
        private static List<ModelCreatureTemplate> _sellers;
        private static List<ModelCreatureTemplate> _repairers;
        private static List<ModelCreatureTemplate> _trainers;
        private static List<ModelGameObjectTemplate> _mailboxes;
        private static List<ModelSpell> _mounts;
        private static List<ModelSpell> _ridingSpells;

        public static bool IsPopulated;

        public static void Initialize()
        {
            IsPopulated = false;
            string baseDirectory = Others.GetCurrentDirectory + @"Data\WoWDb335;Cache=Shared;";
            _con = new SQLiteConnection("Data Source=" + baseDirectory);
            _con.Open();
            _cmd = _con.CreateCommand();

            CreateIndices();

            Stopwatch drinksWatch = Stopwatch.StartNew();

            // WATERS
            string drinkItemsSql = $@"
                SELECT * FROM item_template it
                WHERE it.class = 0
                    AND it.subclass = 5
                    AND spellcategory_1 = 59
                    AND BuyCount = 5;
            ";
            List<ModelItemTemplate> drinks = _con.Query<ModelItemTemplate>(drinkItemsSql)
                .OrderByDescending(p => p.RequiredLevel)
                .ToList();
            foreach (ModelItemTemplate drink in drinks)
            {
                drink.VendorsSellingThisItem = QueryNpcVendorByItem(drink.Entry);
                drink.VendorsSellingThisItem.RemoveAll(v => v.CreatureTemplate == null || v.CreatureTemplate.Creature == null);
            }
            _drinks = drinks;
            //Main.Logger($"Process time (Water) : {drinksWatch.ElapsedMilliseconds} ms");

            // FOODS
            Stopwatch foodWatch = Stopwatch.StartNew();
            string foodtemsSql = $@"
                SELECT * FROM item_template it
                WHERE it.class = 0
                    AND it.subclass = 5
                    AND spellcategory_1 = 11
                    AND BuyCount = 5
                    AND FlagsExtra = 0
                    AND FoodType > 0
                    AND RequiredReputationFaction = 0;
            ";
            List<ModelItemTemplate> foods = _con.Query<ModelItemTemplate>(foodtemsSql)
                .OrderByDescending(p => p.RequiredLevel)
                .ToList();
            foreach (ModelItemTemplate food in foods)
            {
                food.VendorsSellingThisItem = QueryNpcVendorByItem(food.Entry);
                food.VendorsSellingThisItem.RemoveAll(v => v.CreatureTemplate == null || v.CreatureTemplate.Creature == null);
            }
            _foods = foods;
            //Main.Logger($"Process time (Food) : {foodWatch.ElapsedMilliseconds} ms");

            // AMMOS
            Stopwatch ammoWatch = Stopwatch.StartNew();
            string ammoSql = $@"
                SELECT * FROM item_template it
                WHERE it.InventoryType = 24
                    AND BuyCount = 200
                    AND Flags = 0
                    AND AllowableClass = -1
                    AND RequiredReputationFaction  = 0;
            ";
            List<ModelItemTemplate> ammos = _con.Query<ModelItemTemplate>(ammoSql)
                .OrderByDescending(ammo => ammo.RequiredLevel)
                .ToList();
            foreach (ModelItemTemplate ammo in ammos)
            {
                ammo.VendorsSellingThisItem = QueryNpcVendorByItem(ammo.Entry);
                ammo.VendorsSellingThisItem.RemoveAll(v => v.CreatureTemplate == null || v.CreatureTemplate.Creature == null);
            }
            _ammos = ammos;
            //Main.Logger($"Process time (Ammo) : {ammoWatch.ElapsedMilliseconds} ms");

            // POISONS
            Stopwatch poisonWatch = Stopwatch.StartNew();
            string poisonSql = $@"
                SELECT * FROM item_template
                WHERE displayid = 13707 -- Deadly
	                OR displayid = 13710 -- Instant
            ";
            List<ModelItemTemplate> poisons = _con
                .Query<ModelItemTemplate>(poisonSql)
                .OrderByDescending(p => p.RequiredLevel)
                .ToList();
            foreach (ModelItemTemplate poison in poisons)
            {
                poison.VendorsSellingThisItem = QueryNpcVendorByItem(poison.Entry);
                poison.VendorsSellingThisItem.RemoveAll(v => v.CreatureTemplate == null || v.CreatureTemplate.Creature == null);
            }
            _poisons = poisons;
            //Main.Logger($"Process time (Poison) : {poisonWatch.ElapsedMilliseconds} ms");

            // BAGS
            Stopwatch bagsWatch = Stopwatch.StartNew();
            string bagsSql = $@"
                SELECT * FROM item_template
                WHERE class = 1
                    AND subclass = 0
                    AND ContainerSlots = {PluginSettings.CurrentSetting.BagsCapacity}
                    AND BuyPrice > 0
                    AND maxcount  = 0
                    AND Quality < 4
                    AND Flags  = 0
                    AND RequiredLevel = 0
            ";
            List<ModelItemTemplate> bags = _con
                .Query<ModelItemTemplate>(bagsSql)
                .ToList();
            foreach (ModelItemTemplate bag in bags)
            {
                bag.VendorsSellingThisItem = QueryNpcVendorByItem(bag.Entry);
                bag.VendorsSellingThisItem.RemoveAll(v => v.CreatureTemplate == null || v.CreatureTemplate.Creature == null);
            }
            bags.RemoveAll(b => b.VendorsSellingThisItem.Count <= 0);
            _bags = bags;
            //Main.Logger($"Process time (Bags) : {bagsWatch.ElapsedMilliseconds} ms");

            // SELLERS
            Stopwatch sellersWatch = Stopwatch.StartNew();
            string sellersSql = $@"
                SELECT * FROM creature_template
                WHERE npcflag & 128;
            ";
            List<ModelCreatureTemplate> sellers = _con.Query<ModelCreatureTemplate>(sellersSql).ToList();
            int[] sellersIds = sellers.Select(r => r.entry).ToArray();
            List<ModelCreature> sellersCrea = QueryCreaturesByEntries(sellersIds);
            foreach (ModelCreatureTemplate seller in sellers)
            {
                seller.Creature = sellersCrea.Find(sc => sc.id == seller.entry);
            }
            sellers.RemoveAll(v => v.Creature == null);
            _sellers = sellers;
            //Main.Logger($"Process time (Sellers) : {sellersWatch.ElapsedMilliseconds} ms");

            // REPAIRERS
            Stopwatch repairersWatch = Stopwatch.StartNew();
            string repairersSql = $@"
                SELECT * FROM creature_template
                WHERE npcflag & 4096;
            ";
            List<ModelCreatureTemplate> repairers = _con.Query<ModelCreatureTemplate>(repairersSql).ToList();
            int[] repairersIds = repairers.Select(r => r.entry).ToArray();
            List<ModelCreature> repairCreas = QueryCreaturesByEntries(repairersIds);
            foreach (ModelCreatureTemplate repairer in repairers)
            {
                repairer.Creature = repairCreas.Find(cr => cr.id == repairer.entry);
            }
            repairers.RemoveAll(v => v.Creature == null);
            _repairers = repairers;
            //Main.Logger($"Process time (Repairers) : {repairersWatch.ElapsedMilliseconds} ms");

            // TRAINERS
            Stopwatch trainersWatch = Stopwatch.StartNew();
            string trainersSql = $@"
                SELECT * FROM creature_template
                WHERE npcflag & 16
	                AND npcflag  & 32
	                AND subname LIKE '%{ObjectManager.Me.WowClass}%'
            ";
            List<ModelCreatureTemplate> trainers = _con.Query<ModelCreatureTemplate>(trainersSql).ToList();
            int[] trainersIds = trainers.Select(r => r.entry).ToArray();
            List<ModelCreature> trainerCreas = QueryCreaturesByEntries(trainersIds);
            foreach (ModelCreatureTemplate trainer in trainers)
            {
                trainer.Creature = trainerCreas.Find(tc => tc.id == trainer.entry);
            }
            trainers.RemoveAll(v => v.Creature == null);
            _trainers = trainers;
            //Main.Logger($"Process time (Trainers) : {trainersWatch.ElapsedMilliseconds} ms");

            // MAILBOXES
            Stopwatch mailboxesWatch = Stopwatch.StartNew();
            string mailboxesSql = $@"
                SELECT * FROM gameobject_template
                WHERE name = 'Mailbox'
            ";
            List<ModelGameObjectTemplate> mailboxes = _con.Query<ModelGameObjectTemplate>(mailboxesSql).ToList();
            foreach (ModelGameObjectTemplate mailbox in mailboxes)
            {
                mailbox.GameObject = QueryGameObjectByEntry(mailbox.entry);
            }
            mailboxes.RemoveAll(v => v.GameObject == null);
            _mailboxes = mailboxes;
            //Main.Logger($"Process time (Mailboxes) : {mailboxesWatch.ElapsedMilliseconds} ms");

            // MOUNTS
            Stopwatch mountsWatch = Stopwatch.StartNew();
            string mountsSql = $@"
                SELECT * FROM spell
                WHERE attributes = 269844752
            ";
            List<ModelSpell> mountsSpells = _con.Query<ModelSpell>(mountsSql).ToList();
            foreach (ModelSpell mountSpell in mountsSpells)
            {
                ModelItemTemplate item = QueryItemTemplateBySpell(mountSpell.Id);
                if (item != null && (item.AllowableRace & (int)Helpers.GetFactions()) != 0)
                    mountSpell.AssociatedItem = item;
            }
            _mounts = mountsSpells;
            PluginCache.RecordKnownMounts();
            //Main.Logger($"Process time (Mounts) : {mountsWatch.ElapsedMilliseconds} ms");

            // RIDING SPELLS
            Stopwatch ridingSpellsWatch = Stopwatch.StartNew();
            string ridingSql = $@"
                SELECT * FROM spell
                WHERE effectMiscValue_2 = 762 
                    AND effect_2 = 118
            ";
            List<ModelSpell> ridingSpells = _con.Query<ModelSpell>(ridingSql).ToList();
            foreach (ModelSpell ridingSpell in ridingSpells)
            {
                ridingSpell.NpcTrainer = QueryNpcTrainerBySpellID(ridingSpell.Id);
                ridingSpell.NpcTrainer.VendorTemplates.RemoveAll(npc => !npc.IsFriendly);
            }
            _ridingSpells = ridingSpells;
            //Main.Logger($"Process time (Riding spells) : {ridingSpellsWatch.ElapsedMilliseconds} ms");

            _con.Dispose();
            EventsLua.AttachEventLua("PLAYER_LEVEL_UP", m => UpdateDNSList());
            EventsLua.AttachEventLua("PLAYER_ENTERING_WORLD", m => UpdateDNSList());
            EventsLua.AttachEventLua("PLAYER_LEAVING_WORLD", m => UpdateDNSList());
            EventsLua.AttachEventLua("WORLD_MAP_UPDATE", m => UpdateDNSList());
            UpdateDNSList();

            // JSON export
            Stopwatch jsonsWatch = Stopwatch.StartNew();
            try
            {
                if (File.Exists(Others.GetCurrentDirectory + @"\Data\WVM.json"))
                    File.Delete(Others.GetCurrentDirectory + @"\Data\WVM.json");

                using (StreamWriter file = File.CreateText(Others.GetCurrentDirectory + @"\Data\WVM.json"))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(file, new JsonExport(_drinks, _foods, _ammos, _poisons, _bags, _sellers, _repairers,
                        _trainers, _mailboxes, _mounts, _ridingSpells));
                }
            }
            catch (Exception e)
            {
                Logging.WriteError("WriteJSONFromDBResult > " + e.Message);
            }
            //Main.Logger($"Process time (JSON) : {jsonsWatch.ElapsedMilliseconds} ms");

            IsPopulated = true;
        }

        public static void Dispose()
        {
            IsPopulated = false;
        }

        private static void UpdateDNSList()
        {
            if (PluginCache.IsInInstance) return;

            List<string> itemsToAdd = new List<string>();
            List<string> itemsToRemove = new List<string>();

            // food
            itemsToRemove.AddRange(wManagerSetting.CurrentSetting.DoNotSellList.Where(dns => _foods.Exists(food => food.Name == dns)));
            if (PluginSettings.CurrentSetting.FoodNbToBuy > 0)
            {
                itemsToAdd.AddRange(GetAllUsableFoods().Select(food => food.Name));
            }

            // drink
            itemsToRemove.AddRange(wManagerSetting.CurrentSetting.DoNotSellList.Where(dns => _drinks.Exists(drink => drink.Name == dns)));
            if (PluginSettings.CurrentSetting.DrinkNbToBuy > 0)
            {
                itemsToAdd.AddRange(GetAllUsableDrinks().Select(drink => drink.Name));
            }

            itemsToRemove.RemoveAll(item => itemsToAdd.Contains(item));
            WTSettings.RemoveItemFromDoNotSellAndMailList(itemsToRemove);
            WTSettings.AddItemToDoNotSellAndMailList(itemsToAdd);
        }

        public static List<ModelItemTemplate> GetInstantPoisons => _poisons.FindAll(p => p.displayid == 13710);
        public static List<ModelItemTemplate> GetDeadlyPoisons => _poisons.FindAll(p => p.displayid == 13707);
        public static List<ModelItemTemplate> GetAllPoisons => _poisons;

        public static List<ModelItemTemplate> GetAllDrinks => _drinks;
        public static List<ModelItemTemplate> GetAllUsableDrinks()
        {
            int minLevel = PluginSettings.CurrentSetting.BestDrink ? 10 : 20;
            List<ModelItemTemplate> result = GetAllDrinks.FindAll(drink =>
                drink.RequiredLevel <= ObjectManager.Me.Level
                && drink.RequiredLevel > ObjectManager.Me.Level - minLevel);

            return result;
        }

        public static List<ModelItemTemplate> GetBags => _bags;

        public static List<ModelSpell> GetAllMounts => _mounts;
        public static List<ModelSpell> GetNormalMounts => _mounts.FindAll(m => m.effectBasePoints_2 == 59);
        public static List<ModelSpell> GetEpicMounts => _mounts.FindAll(m => m.effectBasePoints_2 == 99);
        public static List<ModelSpell> GetFlyingMounts => _mounts.FindAll(m => m.effectBasePoints_2 == 149);
        public static List<ModelSpell> GetEpicFlyingMounts => _mounts.FindAll(m => m.effectBasePoints_2 >= 279);

        public static ModelSpell GetRidingSpellById(int id) => _ridingSpells.Find(rs => rs.Id == id);

        public static List<ModelSpell> GetKnownMounts => _mounts
            .FindAll(m => PluginCache.KnownMountSpells.Contains(m.Id))
            .OrderByDescending(m => m.effectBasePoints_2)
            .ToList();

        public static ModelSpell GetMyBestMount => GetKnownMounts.FirstOrDefault();

        public static List<ModelItemTemplate> GetAllFoods => _foods;
        public static List<ModelItemTemplate> GetAllUsableFoods()
        {
            int minLevel = PluginSettings.CurrentSetting.BestFood ? 10 : 20;
            List<ModelItemTemplate> result = GetAllFoods.FindAll(food =>
                food.RequiredLevel <= ObjectManager.Me.Level
                && food.RequiredLevel > ObjectManager.Me.Level - minLevel);

            if (PluginSettings.CurrentSetting.FoodType != "Any")
                result.RemoveAll(food => food.FoodType != FoodTypeCode[PluginSettings.CurrentSetting.FoodType]);

            return result;
        }

        private static readonly Dictionary<string, int> FoodTypeCode = new Dictionary<string, int>()
        {
            { "Meat", 1 },
            { "Fish", 2 },
            { "Cheese", 3 },
            { "Bread", 4 },
            { "Fungus", 5 },
            { "Fruit", 6 },
        };

        public static List<ModelItemTemplate> GetUsableAmmos()
        {
            string rangedWeaponType = PluginCache.RangedWeaponType;
            if (rangedWeaponType == "Bows" || rangedWeaponType == "Crossbows")
            {
                return _ammos.FindAll(ammo => ammo.Subclass == 2 && ammo.RequiredLevel <= ObjectManager.Me.Level);
            }
            if (rangedWeaponType == "Guns")
            {
                return _ammos.FindAll(ammo => ammo.Subclass == 3 && ammo.RequiredLevel <= ObjectManager.Me.Level);
            }
            return null;
        }

        public static ModelNpcVendor GetNearestItemVendor(ModelItemTemplate item)
        {
            if (item == null) return null;

            return item.VendorsSellingThisItem
                .Where(vendor => NPCBlackList.IsVendorValid(vendor.CreatureTemplate)
                    && (ObjectManager.Me.Level > 10 || vendor.CreatureTemplate.Creature.GetSpawnPosition.DistanceTo(ObjectManager.Me.Position) < 500))
                .OrderBy(vendor => ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition))
                .FirstOrDefault();
        }

        public static ModelCreatureTemplate GetNearestSeller()
        {
            return _sellers
                .Where(vendor => NPCBlackList.IsVendorValid(vendor))
                .OrderBy(seller => ObjectManager.Me.Position.DistanceTo(seller.Creature.GetSpawnPosition))
                .FirstOrDefault();
        }

        public static ModelCreatureTemplate GetNearestRepairer()
        {
            return _repairers
                .Where(vendor => NPCBlackList.IsVendorValid(vendor))
                .OrderBy(repairer => ObjectManager.Me.Position.DistanceTo(repairer.Creature.GetSpawnPosition))
                .FirstOrDefault();
        }

        public static ModelGameObjectTemplate GetNearestMailBoxFrom(ModelCreatureTemplate npc)
        {
            return _mailboxes
                .Where(mailbox => NPCBlackList.IsMailBoxValid(mailbox)
                    && mailbox.GameObject.GetSpawnPosition.DistanceTo(npc.Creature.GetSpawnPosition) < 300)
                .OrderBy(mailbox => ObjectManager.Me.Position.DistanceTo(mailbox.GameObject.GetSpawnPosition))
                .FirstOrDefault();
        }

        public static ModelCreatureTemplate GetNearestTrainer()
        {
            return _trainers
                .Where(vendor => NPCBlackList.IsVendorValid(vendor)
                    && (ObjectManager.Me.Level <= vendor.minLevel || vendor.minLevel > 15 || vendor.entry == 328)) // Allow Zaldimar Wefhellt (goldshire mage trainer)
                .OrderBy(trainer => ObjectManager.Me.Position.DistanceTo(trainer.Creature.GetSpawnPosition))
                .FirstOrDefault();
        }

        private static List<ModelNpcVendor> QueryNpcVendorByItem(int itemID)
        {
            string npcVendorSql = $@"
                SELECT * FROM npc_vendor
                WHERE item = {itemID}
            ";
            List<ModelNpcVendor> result = _con.Query<ModelNpcVendor>(npcVendorSql).ToList();
            int[] creatureEntries = result.Select(r => r.entry).ToArray();
            List<ModelCreatureTemplate> templates = QueryCreatureTemplatesByEntries(creatureEntries);
            List<ModelCreature> creatures = QueryCreaturesByEntries(creatureEntries);
            foreach (ModelNpcVendor vendor in result)
            {
                vendor.CreatureTemplate = templates.Find(t => t.entry == vendor.entry);
                vendor.CreatureTemplate.Creature = creatures.Find(c => c.id == vendor.entry);
            }
            return result;
        }

        private static List<ModelCreatureTemplate> QueryCreatureTemplatesByEntries(int[] ctEntries)
        {
            string cTemplateSql = $@"
                SELECT * FROM creature_template
                WHERE entry IN ({string.Join(",", ctEntries)})
            ";
            List<ModelCreatureTemplate> result = _con.Query<ModelCreatureTemplate>(cTemplateSql).ToList();
            return result;
        }

        private static List<ModelCreature> QueryCreaturesByEntries(int[] ctEntries)
        {
            string creaSql = $@"
                SELECT * FROM creature
                WHERE id IN ({string.Join(",", ctEntries)})
            ";
            List<ModelCreature> result = _con.Query<ModelCreature>(creaSql).ToList();
            return result;
        }

        private static ModelGameObject QueryGameObjectByEntry(int goEntry)
        {
            string goSql = $@"
                SELECT * FROM gameobject
                WHERE id = {goEntry}
            ";
            ModelGameObject result = _con.Query<ModelGameObject>(goSql).FirstOrDefault();
            return result;
        }

        private static ModelItemTemplate QueryItemTemplateBySpell(int spellId)
        {
            string itSql = $@"
                SELECT * FROM item_template
                WHERE (spellid_2 = {spellId}
                    OR (spellid_2 = 0 AND spellid_1 = {spellId}));
            ";
            ModelItemTemplate result = _con.Query<ModelItemTemplate>(itSql).FirstOrDefault();
            if (result != null)
            {
                result.VendorsSellingThisItem = QueryNpcVendorByItem(result.Entry);
            }
            return result;
        }

        private static ModelNpcTrainer QueryNpcTrainerBySpellID(int spellId)
        {
            string sql = $@"
                SELECT * FROM npc_trainer
                WHERE SpellID = {spellId};
            ";
            ModelNpcTrainer result = _con.Query<ModelNpcTrainer>(sql).FirstOrDefault();
            if (result != null)
            {
                string sqlTrainerIds = $@"
                    SELECT ID FROM npc_trainer
                    WHERE SpellID = {-result.ID};
                ";
                int[] vendorTemplateIds = _con.Query<int>(sqlTrainerIds).ToArray();
                result.VendorTemplates = QueryCreatureTemplatesByEntries(vendorTemplateIds);
                List<ModelCreature> creatures = QueryCreaturesByEntries(vendorTemplateIds);
                foreach (ModelCreatureTemplate template in result.VendorTemplates)
                {
                    template.Creature = creatures.Find(creature => creature.id == template.entry);
                }
            }
            return result;
        }

        private static void CreateIndices()
        {
            Stopwatch stopwatchIndices = Stopwatch.StartNew();
            ExecuteQuery($@"
                CREATE INDEX IF NOT EXISTS `idx_creature_id` ON `creature` (`id`);
                CREATE INDEX IF NOT EXISTS `idx_creature_template_entry` ON `creature_template` (`entry`);
                CREATE INDEX IF NOT EXISTS `idx_npc_vendor_item` ON `npc_vendor` (`item`);
                CREATE INDEX IF NOT EXISTS `idx_spell_attributes` ON `spell` (`attributes`);
                CREATE INDEX IF NOT EXISTS `idx_item_template_spellid_2` ON `item_template` (`spellid_2`);
                CREATE INDEX IF NOT EXISTS `idx_item_template_spellid_1` ON `item_template` (`spellid_1`);
                CREATE INDEX IF NOT EXISTS `idx_npc_trainer_spellid` ON `npc_trainer` (`SpellID`);
            ");
            //Main.Logger($"Process time (Indices) : {stopwatchIndices.ElapsedMilliseconds} ms");
        }

        private static void ExecuteQuery(string query)
        {
            _cmd.CommandText = query;
            _cmd.ExecuteNonQuery();
        }
    }

    class JsonExport
    {
        public List<ModelItemTemplate> Waters { get; }
        public List<ModelItemTemplate> Foods { get; }
        public List<ModelItemTemplate> Ammos { get; }
        public List<ModelItemTemplate> Bags { get; }
        public List<ModelItemTemplate> Poisons { get; }
        public List<ModelCreatureTemplate> Sellers { get; }
        public List<ModelCreatureTemplate> Repairers { get; }
        public List<ModelCreatureTemplate> Trainers { get; }
        public List<ModelGameObjectTemplate> MailBoxes { get; }
        public List<ModelSpell> Mounts { get; }
        public List<ModelSpell> RidingSpells { get; }

        public JsonExport(List<ModelItemTemplate> waters,
            List<ModelItemTemplate> foods,
            List<ModelItemTemplate> ammos,
            List<ModelItemTemplate> poisons,
            List<ModelItemTemplate> bags,
            List<ModelCreatureTemplate> sellers,
            List<ModelCreatureTemplate> repairers,
            List<ModelCreatureTemplate> trainers,
            List<ModelGameObjectTemplate> mailboxes,
            List<ModelSpell> mounts,
            List<ModelSpell> ridingSpells)
        {
            Waters = waters;
            Foods = foods;
            Ammos = ammos;
            Poisons = poisons;
            Sellers = sellers;
            Repairers = repairers;
            Trainers = trainers;
            MailBoxes = mailboxes;
            Bags = bags;
            Mounts = mounts;
            RidingSpells = ridingSpells;
        }
    }
}
