using Newtonsoft.Json;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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
            Stopwatch watch = Stopwatch.StartNew();
            IsPopulated = false;
            Assembly assembly = Assembly.GetExecutingAssembly();
            string zipPath = Others.GetCurrentDirectory + @"Data\WVM.zip";
            string jsonPath = Others.GetCurrentDirectory + @"Data\WVM.json";

            // unzip json into data folder
            if (!File.Exists(jsonPath))
            {
                Main.Logger($"Extracting WVM.json to your data folder");
                File.Delete(zipPath);
                using (Stream compressedStream = assembly.GetManifestResourceStream("WholesomeVendors.Database.WVM.zip"))
                {
                    using (FileStream outputFileStream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write))
                    {
                        compressedStream.CopyTo(outputFileStream);
                        compressedStream.Close();
                    }
                }
                ZipFile.ExtractToDirectory(zipPath, Others.GetCurrentDirectory + @"Data");
                File.Delete(zipPath);
            }

            using (StreamReader reader = new StreamReader(jsonPath))
            {
                string jsonFile = reader.ReadToEnd();
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        Main.LoggerError($"Deserialization error: {args.CurrentObject} => {args.ErrorContext.Error}");
                    }
                };
                FullJSONModel fullJsonModel = JsonConvert.DeserializeObject<FullJSONModel>(jsonFile, settings);
                _drinks = fullJsonModel.Waters;
                _foods = fullJsonModel.Foods;
                _ammos = fullJsonModel.Ammos;
                _poisons = fullJsonModel.Poisons;
                _bags = fullJsonModel.Bags
                    .FindAll(bag => bag.ContainerSlots.ToString() == PluginSettings.CurrentSetting.BagsCapacity);
                _sellers = fullJsonModel.Sellers;
                _repairers = fullJsonModel.Repairers;
                _trainers = fullJsonModel.Trainers
                    .FindAll(trainer => trainer.subname != null && trainer.subname.Contains(ObjectManager.Me.WowClass.ToString()));
                _mailboxes = fullJsonModel.Mailboxes
                    .FindAll(mailbox => mailbox.GameObject.map == 0 
                        || mailbox.GameObject.map == 1
                        || mailbox.GameObject.map == 571
                        || mailbox.GameObject.map == 530);
                _mounts = fullJsonModel.Mounts
                    .FindAll(mount => mount.AssociatedItem != null && (mount.AssociatedItem.AllowableRace & (int)Helpers.GetFactions()) != 0);
                PluginCache.RecordKnownMounts();
                _ridingSpells = fullJsonModel.RidingSpells;
                foreach (ModelSpell ridingSpell in _ridingSpells)
                {
                    ridingSpell.NpcTrainer.VendorTemplates.RemoveAll(npc => !npc.IsFriendly);
                }

                // TEMP
                List< ModelCreatureTemplate> allVendors = new List<ModelCreatureTemplate>();
                allVendors.AddRange(_repairers);
                allVendors.AddRange(_trainers);
                allVendors.AddRange(_sellers);
                foreach (ModelGameObjectTemplate mailbox in _mailboxes)
                {
                    Main.Logger($"--------- Mailbox {mailbox.name} in {mailbox.GameObject.map}");
                    List<ModelCreatureTemplate> sellersAroundMB = allVendors
                        .Where(npc => npc.Creature.GetSpawnPosition.DistanceTo(mailbox.GameObject.GetSpawnPosition) < 200)
                        .ToList();
                    Logging.Write($".go xyz {mailbox.GameObject.position_x.ToString().Replace(",", ".")} {mailbox.GameObject.position_y.ToString().Replace(",", ".")} {(mailbox.GameObject.position_z + 5).ToString().Replace(",", ".")} {mailbox.GameObject.map}");
                    
                    if (sellersAroundMB.Count <= 0)
                    {
                        Main.LoggerError("NO SELLER AROUND !");
                    }
                    else if (sellersAroundMB.All(seller => seller.IsNeutralOrFriendly))
                    {
                        Main.Logger($"All sellers are friendly");
                    }
                    else if (sellersAroundMB.All(seller => seller.IsHostile))
                    {
                        Main.Logger($"All sellers are hostile");
                    }
                    else
                    {
                        Main.LoggerError($"Sellers are MIXED {sellersAroundMB.FindAll(s => s.IsNeutralOrFriendly).Count} friendly / {sellersAroundMB.FindAll(s => s.IsHostile).Count} hostile");
                    }
                }
            }

            Main.Logger($"Initialization took {watch.ElapsedMilliseconds}ms");

            EventsLua.AttachEventLua("PLAYER_LEVEL_UP", m => UpdateDNSList());
            EventsLua.AttachEventLua("PLAYER_ENTERING_WORLD", m => UpdateDNSList());
            EventsLua.AttachEventLua("PLAYER_LEAVING_WORLD", m => UpdateDNSList());
            EventsLua.AttachEventLua("WORLD_MAP_UPDATE", m => UpdateDNSList());

            UpdateDNSList();

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

            // ammo
            itemsToRemove.AddRange(wManagerSetting.CurrentSetting.DoNotSellList.Where(dns => _ammos.Exists(ammo => ammo.Name == dns)));
            if (PluginSettings.CurrentSetting.AmmoAmount > 0)
            {
                itemsToAdd.AddRange(GetUsableAmmos().Select(ammo => ammo.Name));
            }

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

        public static List<ModelItemTemplate> GetAllUsableDrinks()
        {
            int minLevel = PluginSettings.CurrentSetting.BestDrink ? 10 : 20;
            List<ModelItemTemplate> result = _drinks.FindAll(drink =>
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

        public static List<ModelItemTemplate> GetAllUsableFoods()
        {
            int minLevel = PluginSettings.CurrentSetting.BestFood ? 10 : 20;
            List<ModelItemTemplate> result = _foods.FindAll(food =>
                food.RequiredLevel <= ObjectManager.Me.Level
                && food.RequiredLevel > ObjectManager.Me.Level - minLevel);

            if (PluginSettings.CurrentSetting.FoodType != "Any")
                result.RemoveAll(food => food.FoodType != FoodTypeCode[PluginSettings.CurrentSetting.FoodType]);

            return result;
        }

        public static List<ModelItemTemplate> GetUsableAmmos()
        {
            string rangedWeaponType = PluginCache.RangedWeaponType;
            List<ModelItemTemplate> result = new List<ModelItemTemplate>();
            if (rangedWeaponType == "Bows" || rangedWeaponType == "Crossbows")
            {
                result = _ammos.FindAll(ammo => ammo.Subclass == 2 && ammo.RequiredLevel <= ObjectManager.Me.Level);
            }
            if (rangedWeaponType == "Guns")
            {
                result = _ammos.FindAll(ammo => ammo.Subclass == 3 && ammo.RequiredLevel <= ObjectManager.Me.Level);
            }
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

        public static ModelNpcVendor GetNearestItemVendor(ModelItemTemplate item)
        {
            if (item == null) return null;

            List<ModelNpcVendor> pot = item.VendorsSellingThisItem
                .Where(vendor => NPCBlackList.IsVendorValid(vendor.CreatureTemplate)
                    && (ObjectManager.Me.Level > 10 || vendor.CreatureTemplate.Creature.GetSpawnPosition.DistanceTo(ObjectManager.Me.Position) < 500))
                .OrderBy(vendor => ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition))
                .ToList();

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
    }
}
