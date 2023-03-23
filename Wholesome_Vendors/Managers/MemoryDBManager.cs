using Newtonsoft.Json;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using WholesomeToolbox;
using WholesomeVendors.Database.Models;
using WholesomeVendors.Utils;
using WholesomeVendors.WVSettings;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.Managers
{
    internal class MemoryDBManager : IMemoryDBManager
    {
        private readonly IBlackListManager _blackListManager;

        private List<ModelItemTemplate> _drinks;
        private List<ModelItemTemplate> _foods;
        private List<ModelItemTemplate> _ammos;
        private List<ModelItemTemplate> _poisons;
        private List<ModelItemTemplate> _bags;
        private List<ModelCreatureTemplate> _sellers;
        private List<ModelCreatureTemplate> _repairers;
        private List<ModelCreatureTemplate> _trainers;
        private List<ModelGameObjectTemplate> _mailboxes;
        private List<ModelSpell> _mounts;
        private List<ModelSpell> _ridingSpells;
        private List<ModelSpell> _weaponSpells;

        public List<ModelItemTemplate> GetAllPoisons => _poisons;
        public List<ModelItemTemplate> GetAllAmmos => _ammos;
        public List<ModelItemTemplate> GetAllFoods => _foods;
        public List<ModelItemTemplate> GetAllDrinks => _drinks;
        public List<ModelItemTemplate> GetInstantPoisons => _poisons.FindAll(p => p.displayid == 13710);
        public List<ModelItemTemplate> GetDeadlyPoisons => _poisons.FindAll(p => p.displayid == 13707);

        public MemoryDBManager(IBlackListManager blackListManager)
        {
            _blackListManager = blackListManager;
        }

        public void Initialize()
        {
            Stopwatch watch = Stopwatch.StartNew();
            Assembly assembly = Assembly.GetExecutingAssembly();
            string zipPath = Others.GetCurrentDirectory + @"Data\WVM.zip";
            string jsonPath = Others.GetCurrentDirectory + @"Data\WVM.json";

            // unzip json into data folder
            if (!File.Exists(jsonPath))
            {
                Logger.Log($"Extracting WVM.json to your data folder");
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
                        Logger.LogError($"Deserialization error: {args.CurrentObject} => {args.ErrorContext.Error}");
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
                _ridingSpells = fullJsonModel.RidingSpells;
                _weaponSpells = fullJsonModel.WeaponSpells;
                foreach (ModelSpell ridingSpell in _ridingSpells)
                {
                    ridingSpell.NpcTrainer.VendorTemplates.RemoveAll(npc => !npc.IsFriendly);
                }
                _weaponSpells = fullJsonModel.WeaponSpells;
                foreach (ModelSpell weaponSpell in _weaponSpells)
                {
                    weaponSpell.NpcTrainers.RemoveAll(npc => npc.VendorTemplates.Any(vt => !vt.IsFriendly));
                }
            }

            FilterMailBoxes();

            Logger.Log($"Initialization took {watch.ElapsedMilliseconds}ms");
        }

        public void Dispose()
        {
        }

        private void FilterMailBoxes()
        {
            List<ModelCreatureTemplate> allVendors = new List<ModelCreatureTemplate>();
            allVendors.AddRange(_repairers);
            allVendors.AddRange(_trainers);
            allVendors.AddRange(_sellers);

            List<int> forceRemoveMailBoxes = new List<int>()
            {
                191953, // Dalaran
                194147, // Dalaran underbelly
                184134, // Shattrath
                184490, // Shadowmoon
                187113, // Sunwell plateau
                194027, // Shadow vault
                191952, // Dalaran
                191950, // Dalaran
            };

            List<int> forceHordeMailBoxes = new List<int>()
            {
                194788, // Argent tourney camp
                143985, // Thunder Bluff
                143988, // Tarren Mill
                173221, // Orgrimmar
                177044, // Undercity
                182567, // Zabra'Jin
                195555, // Orgrimmar
                195560, // Orgrimmar
                195558, // Orgrimmar
                195561, // Orgrimmar
                195557, // Orgrimmar
                195562, // Orgrimmar
                195554, // Orgrimmar
                195629, // Undercity
                195624, // Undercity
                195625, // Undercity
                195626, // Undercity
                195628, // Undercity
                195627, // Undercity
                195559, // Orgrimmar
            };

            List<int> forceAllianceMailBoxes = new List<int>()
            {
                32349, // Ironforge
                144131, // Stormwind
                171699, // Ironforge
                182949, // Exodar
                188123, // Darnassus
                194492, // Argent tourney camp
                195614, // Stormwind
                195615, // Stormwind
                195603, // Stormwind
                195604, // Stormwind
                195608, // Stormwind
                195616, // Stormwind
                195609, // Stormwind
                195613, // Stormwind
                195530, // Darnassus
                195529, // Darnassus
            };

            List<ModelGameObjectTemplate> result = new List<ModelGameObjectTemplate>();

            foreach (ModelGameObjectTemplate mailbox in _mailboxes)
            {
                if (forceRemoveMailBoxes.Contains(mailbox.entry)
                    || WTPlayer.IsHorde() && forceAllianceMailBoxes.Contains(mailbox.entry)
                    || !WTPlayer.IsHorde() && forceHordeMailBoxes.Contains(mailbox.entry))
                {
                    continue;
                }

                //Logger.Log($"****************************************");
                //Logger.Log($"Mailbox {mailbox.name} in {mailbox.GameObject.map}, ENTRY: {mailbox.entry}");
                List<ModelCreatureTemplate> sellersAroundMB = allVendors
                    .Where(npc => npc.Creature.GetSpawnPosition.DistanceTo(mailbox.GameObject.GetSpawnPosition) < 200)
                    .ToList();

                if (sellersAroundMB.Count <= 0
                    || sellersAroundMB.All(seller => seller.IsHostile))
                {
                    continue;
                }

                result.Add(mailbox);
            }
            /*
            // DEBUG TP
            bool lastBuggedReached = false;
            int lastClean = 195467;
            foreach (ModelGameObjectTemplate mailbox in result)
            {
                if (!lastBuggedReached)
                {
                    if (mailbox.entry == lastClean)
                    {
                        lastBuggedReached = true;
                    }
                    else
                    {
                        continue;
                    }
                }

                string tpToMailBoxCommand = $".go xyz {mailbox.GameObject.position_x.ToString().Replace(",", ".")} {mailbox.GameObject.position_y.ToString().Replace(",", ".")} {(mailbox.GameObject.position_z + 5).ToString().Replace(",", ".")} {mailbox.GameObject.map}";
                Logging.Write($"Going to MB {mailbox.entry}");
                Lua.LuaDoString($@"SendChatMessage(""{tpToMailBoxCommand}"")");
                Thread.Sleep(5000);

                while (ObjectManager.Me.IsDead)
                {
                    Thread.Sleep(1000);
                }
            }
            */
            _mailboxes = result;
        }

        public List<ModelItemTemplate> GetAllUsableDrinks()
        {
            int myLevel = (int)ObjectManager.Me.Level;
            List<ModelItemTemplate> allDrinks = _drinks
                .FindAll(drink => drink.RequiredLevel <= myLevel);

            List<ModelItemTemplate> bestDrinks = allDrinks
                .FindAll(drink => drink.RequiredLevel > myLevel - 10);
            List<ModelItemTemplate> usableDrinks = allDrinks
                .FindAll(drink => drink.RequiredLevel > myLevel - 20);

            if (PluginSettings.CurrentSetting.BestDrink && bestDrinks.Count > 0)
            {
                return bestDrinks;
            }

            return usableDrinks;
        }

        public List<ModelItemTemplate> GetAllUsableFoods()
        {
            int myLevel = (int)ObjectManager.Me.Level;
            List<ModelItemTemplate> allFoods = _foods.FindAll(food =>
                food.RequiredLevel <= ObjectManager.Me.Level);

            List<ModelItemTemplate> bestFoods = allFoods
                .FindAll(food => food.RequiredLevel > (myLevel - 10));
            List<ModelItemTemplate> usableFoods = allFoods
                .FindAll(food => food.RequiredLevel > (myLevel - 20));

            if (PluginSettings.CurrentSetting.FoodType != "Any")
            {
                bestFoods.RemoveAll(food => food.FoodType != FoodTypeCode[PluginSettings.CurrentSetting.FoodType]);
                usableFoods.RemoveAll(food => food.FoodType != FoodTypeCode[PluginSettings.CurrentSetting.FoodType]);
            }

            if (PluginSettings.CurrentSetting.BestFood && bestFoods.Count > 0)
            {
                return bestFoods;
            }

            return usableFoods;
        }

        public List<ModelItemTemplate> GetBags => _bags;

        public List<ModelSpell> GetNormalMounts => _mounts.FindAll(m => m.effectBasePoints_2 == 59);
        public List<ModelSpell> GetEpicMounts => _mounts.FindAll(m => m.effectBasePoints_2 == 99);
        public List<ModelSpell> GetFlyingMounts => _mounts.FindAll(m => m.effectBasePoints_2 == 149);
        public List<ModelSpell> GetEpicFlyingMounts => _mounts.FindAll(m => m.effectBasePoints_2 >= 279);
        public ModelSpell GetRidingSpellById(int id) => _ridingSpells.Find(rs => rs.Id == id);

        private readonly Dictionary<string, int> FoodTypeCode = new Dictionary<string, int>()
        {
            { "Meat", 1 },
            { "Fish", 2 },
            { "Cheese", 3 },
            { "Bread", 4 },
            { "Fungus", 5 },
            { "Fruit", 6 },
        };

        public ModelNpcVendor GetNearestItemVendor(ModelItemTemplate item)
        {
            if (item == null) return null;

            List<ModelNpcVendor> pot = item.VendorsSellingThisItem
                .Where(vendor => _blackListManager.IsVendorValid(vendor.CreatureTemplate)
                    && (ObjectManager.Me.Level > 10 || vendor.CreatureTemplate.Creature.GetSpawnPosition.DistanceTo(ObjectManager.Me.Position) < 500))
                .OrderBy(vendor => ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition))
                .ToList();

            return item.VendorsSellingThisItem
                .Where(vendor => _blackListManager.IsVendorValid(vendor.CreatureTemplate)
                    && (ObjectManager.Me.Level > 10 || vendor.CreatureTemplate.Creature.GetSpawnPosition.DistanceTo(ObjectManager.Me.Position) < 500))
                .OrderBy(vendor => ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition))
                .FirstOrDefault();
        }

        public ModelCreatureTemplate GetNearestSeller()
        {
            return _sellers
                .Where(vendor => _blackListManager.IsVendorValid(vendor))
                .OrderBy(seller => ObjectManager.Me.Position.DistanceTo(seller.Creature.GetSpawnPosition))
                .FirstOrDefault();
        }

        public ModelCreatureTemplate GetNearestRepairer()
        {
            return _repairers
                .Where(vendor => _blackListManager.IsVendorValid(vendor))
                .OrderBy(repairer => ObjectManager.Me.Position.DistanceTo(repairer.Creature.GetSpawnPosition))
                .FirstOrDefault();
        }

        public ModelGameObjectTemplate GetNearestMailBoxFrom(ModelCreatureTemplate npc)
        {
            return _mailboxes
                .Where(mailbox => _blackListManager.IsMailBoxValid(mailbox)
                    && mailbox.GameObject.GetSpawnPosition.DistanceTo(npc.Creature.GetSpawnPosition) < 300)
                .OrderBy(mailbox => ObjectManager.Me.Position.DistanceTo(mailbox.GameObject.GetSpawnPosition))
                .FirstOrDefault();
        }

        public ModelGameObjectTemplate GetNearestMailBoxFromMe(int range)
        {
            return _mailboxes
                .Where(mailbox => _blackListManager.IsMailBoxValid(mailbox)
                    && mailbox.GameObject.GetSpawnPosition.DistanceTo(ObjectManager.Me.Position) < range)
                .OrderBy(mailbox => ObjectManager.Me.Position.DistanceTo(mailbox.GameObject.GetSpawnPosition))
                .FirstOrDefault();
        }

        public ModelCreatureTemplate GetNearestTrainer()
        {
            return _trainers
                .Where(vendor => _blackListManager.IsVendorValid(vendor)
                    && (ObjectManager.Me.Level <= vendor.minLevel || vendor.minLevel > 15 || vendor.entry == 328)) // Allow Zaldimar Wefhellt (goldshire mage trainer)
                .OrderBy(vendor => ObjectManager.Me.Position.DistanceTo(vendor.Creature.GetSpawnPosition))
                .FirstOrDefault();
        }

        public ModelCreatureTemplate GetNearestWeaponsTrainer(int spellId)
        {
            ModelSpell spellToLearn = _weaponSpells
                .Find(ws => ws.Id == spellId);
            List<ModelCreatureTemplate> potentialVendors = new List<ModelCreatureTemplate>();
            foreach (ModelNpcTrainer mct in spellToLearn.NpcTrainers)
            {
                potentialVendors.AddRange(mct.VendorTemplates);
            }
            return potentialVendors
                .Where(vendor => _blackListManager.IsVendorValid(vendor))
                .Where(vendor => ObjectManager.Me.Position.DistanceTo(vendor.Creature.GetSpawnPosition) < 1000)
                .OrderBy(vendor => ObjectManager.Me.Position.DistanceTo(vendor.Creature.GetSpawnPosition))
                .FirstOrDefault();
        }

        public ModelSpell GetWeaponSpellById(int id) => _weaponSpells.Find(w => w.Id == id);
    }
}
