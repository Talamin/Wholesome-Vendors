using Dapper;
using Db_To_Json.VendorsPlugin.JSONModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Db_To_Json.VendorsPlugin
{
    internal class VendorsPluginGeneration
    {
        private static readonly string _jsonFileName = "WVM.json";
        private static readonly string _zipName = "WVM.zip";
        private static readonly string _vendorsJsonOutputPath = $"{JSONGenerator.OutputPath}{JSONGenerator.PathSep}{_jsonFileName}";
        private static readonly string _vendorsJsonCopyToPath = $"{Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName}{JSONGenerator.PathSep}Wholesome_Vendors{JSONGenerator.PathSep}Database";
        private static readonly string _zipFilePath = $"{_vendorsJsonCopyToPath}{JSONGenerator.PathSep}{_zipName}";

        public static void Generate(SQLiteConnection con, SQLiteCommand cmd)
        {
            Console.WriteLine("----- Starting generation for Vendors plugin -----");
            Stopwatch totalWatch = Stopwatch.StartNew();

            // Drinks
            Stopwatch drinksWatch = Stopwatch.StartNew();
            string drinkItemsSql = $@"
                SELECT * FROM item_template it
                WHERE it.class = 0
                    AND it.subclass = 5
                    AND spellcategory_1 = 59
                    AND BuyCount = 5;
            ";
            List<VendorsModelItemTemplate> drinks = con.Query<VendorsModelItemTemplate>(drinkItemsSql)
                .OrderByDescending(p => p.RequiredLevel)
                .ToList();
            foreach (VendorsModelItemTemplate drink in drinks)
            {
                drink.VendorsSellingThisItem = QueryNpcVendorByItem(con, drink.Entry);
                drink.VendorsSellingThisItem.RemoveAll(v => v.CreatureTemplate == null || v.CreatureTemplate.Creature == null);
            }
            Console.WriteLine($"[Vendors] Water took {drinksWatch.ElapsedMilliseconds}ms");

            // Foods
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
            List<VendorsModelItemTemplate> foods = con.Query<VendorsModelItemTemplate>(foodtemsSql)
                .OrderByDescending(p => p.RequiredLevel)
                .ToList();
            foreach (VendorsModelItemTemplate food in foods)
            {
                food.VendorsSellingThisItem = QueryNpcVendorByItem(con, food.Entry);
                food.VendorsSellingThisItem.RemoveAll(v => v.CreatureTemplate == null || v.CreatureTemplate.Creature == null);
            }
            Console.WriteLine($"[Vendors] Foods took {foodWatch.ElapsedMilliseconds}ms");

            // Ammos
            Stopwatch ammoWatch = Stopwatch.StartNew();
            string ammoSql = $@"
                SELECT * FROM item_template it
                WHERE it.InventoryType = 24
                    AND BuyCount = 200
                    AND Flags = 0
                    AND AllowableClass = -1
                    AND RequiredReputationFaction  = 0
                    AND FlagsExtra = 0
                    AND bonding = 0;
            ";
            List<VendorsModelItemTemplate> ammos = con.Query<VendorsModelItemTemplate>(ammoSql)
                .OrderByDescending(ammo => ammo.RequiredLevel)
                .ToList();
            foreach (VendorsModelItemTemplate ammo in ammos)
            {
                ammo.VendorsSellingThisItem = QueryNpcVendorByItem(con, ammo.Entry);
                ammo.VendorsSellingThisItem.RemoveAll(v => v.CreatureTemplate == null || v.CreatureTemplate.Creature == null);
            }
            Console.WriteLine($"[Vendors] Ammos took {ammoWatch.ElapsedMilliseconds}ms");

            // Poisons
            Stopwatch poisonWatch = Stopwatch.StartNew();
            string poisonSql = $@"
                SELECT * FROM item_template
                WHERE displayid = 13707 -- Deadly
	                OR displayid = 13710 -- Instant
            ";
            List<VendorsModelItemTemplate> poisons = con
                .Query<VendorsModelItemTemplate>(poisonSql)
                .OrderByDescending(p => p.RequiredLevel)
                .ToList();
            foreach (VendorsModelItemTemplate poison in poisons)
            {
                poison.VendorsSellingThisItem = QueryNpcVendorByItem(con, poison.Entry);
                poison.VendorsSellingThisItem.RemoveAll(v => v.CreatureTemplate == null || v.CreatureTemplate.Creature == null);
            }
            Console.WriteLine($"[Vendors] Poisons took {poisonWatch.ElapsedMilliseconds}ms");

            // Bags
            Stopwatch bagsWatch = Stopwatch.StartNew();
            string bagsSql = $@"
                SELECT * FROM item_template
                WHERE class = 1
                    AND subclass = 0
                    AND BuyPrice > 0
                    AND maxcount  = 0
                    AND Quality < 4
                    AND Flags  = 0
                    AND RequiredLevel = 0
            ";
            List<VendorsModelItemTemplate> bags = con
                .Query<VendorsModelItemTemplate>(bagsSql)
                .ToList();
            foreach (VendorsModelItemTemplate bag in bags)
            {
                bag.VendorsSellingThisItem = QueryNpcVendorByItem(con, bag.Entry);
                bag.VendorsSellingThisItem.RemoveAll(v => v.CreatureTemplate == null || v.CreatureTemplate.Creature == null);
            }
            bags.RemoveAll(b => b.VendorsSellingThisItem.Count <= 0);
            Console.WriteLine($"[Vendors] Poisons took {bagsWatch.ElapsedMilliseconds}ms");

            // Sellers
            Stopwatch sellersWatch = Stopwatch.StartNew();
            string sellersSql = $@"
                SELECT * FROM creature_template
                WHERE npcflag & 128;
            ";
            List<VendorsModelCreatureTemplate> sellers = con.Query<VendorsModelCreatureTemplate>(sellersSql).ToList();
            int[] sellersIds = sellers.Select(r => r.entry).ToArray();
            List<VendorsModelCreature> sellersCrea = QueryCreaturesByEntries(con, sellersIds);
            foreach (VendorsModelCreatureTemplate seller in sellers)
            {
                seller.Creature = sellersCrea.Find(sc => sc.id == seller.entry);
            }
            sellers.RemoveAll(v => v.Creature == null);
            Console.WriteLine($"[Vendors] Sellers took {sellersWatch.ElapsedMilliseconds}ms");

            // Repairers
            Stopwatch repairersWatch = Stopwatch.StartNew();
            string repairersSql = $@"
                SELECT * FROM creature_template
                WHERE npcflag & 4096;
            ";
            List<VendorsModelCreatureTemplate> repairers = con.Query<VendorsModelCreatureTemplate>(repairersSql).ToList();
            int[] repairersIds = repairers.Select(r => r.entry).ToArray();
            List<VendorsModelCreature> repairCreas = QueryCreaturesByEntries(con, repairersIds);
            foreach (VendorsModelCreatureTemplate repairer in repairers)
            {
                repairer.Creature = repairCreas.Find(cr => cr.id == repairer.entry);
            }
            repairers.RemoveAll(v => v.Creature == null);
            Console.WriteLine($"[Vendors] Repairers took {repairersWatch.ElapsedMilliseconds}ms");

            // Trainers
            Stopwatch trainersWatch = Stopwatch.StartNew();
            string trainersSql = $@"
                SELECT * FROM creature_template
                WHERE npcflag & 16
	                AND npcflag  & 32
            ";
            List<VendorsModelCreatureTemplate> trainers = con.Query<VendorsModelCreatureTemplate>(trainersSql).ToList();
            int[] trainersIds = trainers.Select(r => r.entry).ToArray();
            List<VendorsModelCreature> trainerCreas = QueryCreaturesByEntries(con, trainersIds);
            foreach (VendorsModelCreatureTemplate trainer in trainers)
            {
                trainer.Creature = trainerCreas.Find(tc => tc.id == trainer.entry);
            }
            trainers.RemoveAll(v => v.Creature == null);
            Console.WriteLine($"[Vendors] Trainers took {trainersWatch.ElapsedMilliseconds}ms");

            // Mailboxes
            Stopwatch mailboxesWatch = Stopwatch.StartNew();
            string mailboxesSql = $@"
                SELECT * FROM gameobject_template
                WHERE name = 'Mailbox'
            ";
            List<VendorsModelGameObjectTemplate> mailboxes = con.Query<VendorsModelGameObjectTemplate>(mailboxesSql).ToList();
            foreach (VendorsModelGameObjectTemplate mailbox in mailboxes)
            {
                mailbox.GameObject = QueryGameObjectByEntry(con, mailbox.entry);
            }
            mailboxes.RemoveAll(v => v.GameObject == null);
            Console.WriteLine($"[Vendors] Mailboxes took {mailboxesWatch.ElapsedMilliseconds}ms");

            // Mounts
            Stopwatch mountsWatch = Stopwatch.StartNew();
            string mountsSql = $@"
                SELECT * FROM spell
                WHERE attributes = 269844752
            ";
            List<VendorsModelSpell> mountsSpells = con.Query<VendorsModelSpell>(mountsSql).ToList();
            foreach (VendorsModelSpell mountSpell in mountsSpells)
            {
                VendorsModelItemTemplate item = QueryItemTemplateBySpell(con, mountSpell.Id);
                if (item != null)
                    mountSpell.AssociatedItem = item;
            }
            Console.WriteLine($"[Vendors] Mounts took {mountsWatch.ElapsedMilliseconds}ms");

            // Riding spells
            Stopwatch ridingSpellsWatch = Stopwatch.StartNew();
            string ridingSql = $@"
                SELECT * FROM spell
                WHERE effectMiscValue_2 = 762 
                    AND effect_2 = 118
            ";
            List<VendorsModelSpell> ridingSpells = con.Query<VendorsModelSpell>(ridingSql).ToList();
            foreach (VendorsModelSpell ridingSpell in ridingSpells)
            {
                ridingSpell.NpcTrainer = QueryNpcTrainerBySpellID(con, ridingSpell.Id);
                //ridingSpell.NpcTrainer.VendorTemplates.RemoveAll(npc => !npc.IsFriendly);
            }
            Console.WriteLine($"[Vendors] Riding spells took {ridingSpellsWatch.ElapsedMilliseconds}ms");

            // Weapon skills
            Stopwatch weaponSkillsWatch = Stopwatch.StartNew();
            string weaponSkillsSql = $@"
                SELECT * FROM spell
                WHERE (""Attributes"" = 192
                    AND EquippedItemSubclass  <> 0
                    AND Effect_2 = 60
                    AND SpellIconID > 1) OR ID = 15590;
            ";
            List<VendorsModelSpell> weaponSpells = con.Query<VendorsModelSpell>(weaponSkillsSql).ToList();
            foreach (VendorsModelSpell weaponSpell in weaponSpells)
            {
                weaponSpell.NpcTrainers = QueryNpcWeaponTrainersBySpellID(con, weaponSpell.Id);
            }
            weaponSpells.RemoveAll(ws => ws.NpcTrainers.Count == 0);
            Console.WriteLine($"[Vendors] Weapon spells took {weaponSkillsWatch.ElapsedMilliseconds}ms");


            File.Delete(_vendorsJsonOutputPath);
            File.Delete(_zipFilePath);
            File.Delete(_vendorsJsonCopyToPath + $"{JSONGenerator.PathSep}{_jsonFileName}");

            using (StreamWriter file = File.CreateText(_vendorsJsonOutputPath))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, new VendorsJsonExport(drinks, foods, ammos, poisons, bags, sellers, repairers,
                    trainers, mailboxes, mountsSpells, ridingSpells, weaponSpells));
                Console.WriteLine($"[Vendors] JSON created in {_vendorsJsonOutputPath}");
                long fileSize = new FileInfo(_vendorsJsonOutputPath).Length;
                Console.WriteLine($"[Vendors] JSON size is {((float)fileSize / 1000000).ToString("0.00")} MB");
            }

            if (Directory.Exists(_vendorsJsonCopyToPath))
            {
                // Copy file to Vendor project
                //File.Copy(_vendorsJsonOutputPath, _vendorsJsonCopyToPath + $"{JSONGenerator.PathSep}{_jsonFileName}", true);
                //Console.WriteLine($"[Vendors] JSON copied to {_vendorsJsonCopyToPath + $"{JSONGenerator.PathSep}{_jsonFileName}"}");
                // Zip file to Vendor project
                using (ZipArchive zip = ZipFile.Open(_zipFilePath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(_vendorsJsonOutputPath, _jsonFileName);
                    Console.WriteLine($"[Vendors] JSON compressed in {_zipFilePath}");
                    long fileSize = new FileInfo(_zipFilePath).Length;
                    Console.WriteLine($"[Vendors] Compressed JSON size is {((float)fileSize / 1000000).ToString("0.00")} MB");
                }
            }
            else
            {
                Console.WriteLine($"ERROR: Directory {_vendorsJsonCopyToPath} does not exist");
            }

            Console.WriteLine($"[Vendors] Total took {totalWatch.ElapsedMilliseconds}ms");
        }

        private static VendorsModelGameObject QueryGameObjectByEntry(SQLiteConnection con, int goEntry)
        {
            string goSql = $@"
                SELECT * FROM gameobject
                WHERE id = {goEntry}
            ";
            VendorsModelGameObject result = con.Query<VendorsModelGameObject>(goSql).FirstOrDefault();
            return result;
        }

        private static VendorsModelItemTemplate QueryItemTemplateBySpell(SQLiteConnection con, int spellId)
        {
            string itSql = $@"
                SELECT * FROM item_template
                WHERE (spellid_2 = {spellId}
                    OR (spellid_2 = 0 AND spellid_1 = {spellId}));
            ";
            VendorsModelItemTemplate result = con.Query<VendorsModelItemTemplate>(itSql).FirstOrDefault();
            if (result != null)
            {
                result.VendorsSellingThisItem = QueryNpcVendorByItem(con, result.Entry);
            }
            return result;
        }

        private static List<VendorsModelCreature> QueryCreaturesByEntries(SQLiteConnection con, int[] ctEntries)
        {
            string creaSql = $@"
                SELECT * FROM creature
                WHERE id IN ({string.Join(",", ctEntries)})
            ";
            List<VendorsModelCreature> result = con.Query<VendorsModelCreature>(creaSql).ToList();
            return result;
        }

        private static List<VendorsModelCreatureTemplate> QueryCreatureTemplatesByEntries(SQLiteConnection con, int[] ctEntries)
        {
            string cTemplateSql = $@"
                SELECT * FROM creature_template
                WHERE entry IN ({string.Join(",", ctEntries)})
            ";
            List<VendorsModelCreatureTemplate> result = con.Query<VendorsModelCreatureTemplate>(cTemplateSql).ToList();
            return result;
        }

        private static List<ModelNpcVendor> QueryNpcVendorByItem(SQLiteConnection con, int itemID)
        {
            string npcVendorSql = $@"
                SELECT * FROM npc_vendor
                WHERE item = {itemID}
            ";
            List<ModelNpcVendor> result = con.Query<ModelNpcVendor>(npcVendorSql).ToList();
            int[] creatureEntries = result.Select(r => r.entry).ToArray();
            List<VendorsModelCreatureTemplate> templates = QueryCreatureTemplatesByEntries(con, creatureEntries);
            List<VendorsModelCreature> creatures = QueryCreaturesByEntries(con, creatureEntries);
            foreach (ModelNpcVendor vendor in result)
            {
                vendor.CreatureTemplate = templates.Find(t => t.entry == vendor.entry);
                vendor.CreatureTemplate.Creature = creatures.Find(c => c.id == vendor.entry);
            }
            return result;
        }

        private static List<VendorsModelNpcTrainer> QueryNpcWeaponTrainersBySpellID(SQLiteConnection con, int spellId)
        {
            string sql = $@"
                SELECT * FROM npc_trainer
                WHERE SpellID = {spellId};
            ";

            List<VendorsModelNpcTrainer> result = con.Query<VendorsModelNpcTrainer>(sql).ToList();
            foreach (VendorsModelNpcTrainer trainer in result)
            {
                trainer.VendorTemplates = QueryCreatureTemplatesByEntries(con, new int[] { trainer.ID });
                List<VendorsModelCreature> creatures = QueryCreaturesByEntries(con, new int[] { trainer.ID });
                foreach (VendorsModelCreatureTemplate template in trainer.VendorTemplates)
                {
                    template.Creature = creatures.Find(creature => creature.id == template.entry);
                }
            }
            return result;
        }

        private static VendorsModelNpcTrainer QueryNpcTrainerBySpellID(SQLiteConnection con, int spellId)
        {
            string sql = $@"
                SELECT * FROM npc_trainer
                WHERE SpellID = {spellId};
            ";
            VendorsModelNpcTrainer result = con.Query<VendorsModelNpcTrainer>(sql).FirstOrDefault();
            if (result != null)
            {
                string sqlTrainerIds = $@"
                    SELECT ID FROM npc_trainer
                    WHERE SpellID = {-result.ID};
                ";
                int[] vendorTemplateIds = con.Query<int>(sqlTrainerIds).ToArray();
                result.VendorTemplates = QueryCreatureTemplatesByEntries(con, vendorTemplateIds);
                List<VendorsModelCreature> creatures = QueryCreaturesByEntries(con, vendorTemplateIds);
                foreach (VendorsModelCreatureTemplate template in result.VendorTemplates)
                {
                    template.Creature = creatures.Find(creature => creature.id == template.entry);
                }
            }
            return result;
        }
    }

    class VendorsJsonExport
    {
        public List<VendorsModelItemTemplate> Waters { get; }
        public List<VendorsModelItemTemplate> Foods { get; }
        public List<VendorsModelItemTemplate> Ammos { get; }
        public List<VendorsModelItemTemplate> Bags { get; }
        public List<VendorsModelItemTemplate> Poisons { get; }
        public List<VendorsModelCreatureTemplate> Sellers { get; }
        public List<VendorsModelCreatureTemplate> Repairers { get; }
        public List<VendorsModelCreatureTemplate> Trainers { get; }
        public List<VendorsModelGameObjectTemplate> MailBoxes { get; }
        public List<VendorsModelSpell> Mounts { get; }
        public List<VendorsModelSpell> RidingSpells { get; }
        public List<VendorsModelSpell> WeaponSpells { get; }

        public VendorsJsonExport(List<VendorsModelItemTemplate> waters,
            List<VendorsModelItemTemplate> foods,
            List<VendorsModelItemTemplate> ammos,
            List<VendorsModelItemTemplate> poisons,
            List<VendorsModelItemTemplate> bags,
            List<VendorsModelCreatureTemplate> sellers,
            List<VendorsModelCreatureTemplate> repairers,
            List<VendorsModelCreatureTemplate> trainers,
            List<VendorsModelGameObjectTemplate> mailboxes,
            List<VendorsModelSpell> mounts,
            List<VendorsModelSpell> ridingSpells,
            List<VendorsModelSpell> weaponSpells)
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
            WeaponSpells = weaponSpells;
        }
    }
}
