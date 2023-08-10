using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WholesomeToolbox;
using WholesomeVendors.Database.Models;
using WholesomeVendors.Managers;
using WholesomeVendors.Utils;
using WholesomeVendors.WVSettings;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.WVState
{
    public class BuyMountState : State
    {
        public override string DisplayName { get; set; } = "WV Buy Mount";

        private readonly IPluginCacheManager _pluginCacheManager;
        private readonly IMemoryDBManager _memoryDBManager;
        private readonly IVendorTimerManager _vendorTimerManager;
        private readonly IBlackListManager _blackListManager;

        private WoWLocalPlayer Me = ObjectManager.Me;
        ModelSpell _ridingSkillToLearn;
        ModelSpell _mountSpellToLearn;
        ModelCreatureTemplate _ridingTrainer;
        ModelCreatureTemplate _mountVendor;

        public BuyMountState(
            IMemoryDBManager memoryDBManager,
            IPluginCacheManager pluginCacheManager,
            IVendorTimerManager vendorTimerManager,
            IBlackListManager blackListManager)
        {
            _memoryDBManager = memoryDBManager;
            _pluginCacheManager = pluginCacheManager;
            _vendorTimerManager = vendorTimerManager;
            _blackListManager = blackListManager;
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Main.IsLaunched
                    || _pluginCacheManager.InLoadingScreen
                    || !_pluginCacheManager.BagsRecorded
                    || Me.Level < 20
                    || Fight.InFight
                    || Me.IsOnTaxi
                    || !Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                {
                    return false;
                }

                _ridingSkillToLearn = null;
                _mountSpellToLearn = null;
                _ridingTrainer = null;
                _mountVendor = null;

                if (_pluginCacheManager.IsInInstance)
                {
                    return false;
                }

                // Epic mount
                if (PluginSettings.CurrentSetting.BuyEpicMount
                    && Me.Level >= 40
                    && !Know150Mount)
                {
                    int neededMoney = PluginSettings.CurrentSetting.MountsAreFree ? 0 : 100000; // mount cost
                    if (_pluginCacheManager.RidingSkill < 75
                        && !PluginSettings.CurrentSetting.MountSkillsAreFree)
                    {
                        neededMoney += 40000; // training cost 75
                    }
                    if (_pluginCacheManager.RidingSkill < 150
                        && !PluginSettings.CurrentSetting.MountSkillsAreFree)
                    {
                        neededMoney += 500000; // training cost 150
                    }

                    if (_pluginCacheManager.Money >= neededMoney)
                    {
                        if (_pluginCacheManager.RidingSkill < 75)
                        {
                            if (SetRidingTraining(33388)) // Apprentice
                            {
                                return true;
                            }
                        }
                        else if (_pluginCacheManager.RidingSkill < 150)
                        {
                            if (SetRidingTraining(33391)) // Journeyman
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (SetMountToBuy(_memoryDBManager.GetEpicMounts, GroundMount150SpellsDictionary))
                            {
                                return true;
                            }
                        }
                    }
                }

                // Normal mount
                if (PluginSettings.CurrentSetting.BuyGroundMount
                    && !Know75Mount
                    && !Know150Mount)
                {
                    int neededMoney = PluginSettings.CurrentSetting.MountsAreFree ? 0 : 10000; // mount cost
                    if (_pluginCacheManager.RidingSkill < 75
                        && !PluginSettings.CurrentSetting.MountSkillsAreFree)
                    {
                        neededMoney += 40000; // training cost 75
                    }

                    if (_pluginCacheManager.Money >= neededMoney)
                    {
                        if (_pluginCacheManager.RidingSkill < 75)
                        {
                            if (SetRidingTraining(33388)) // Apprentice
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (SetMountToBuy(_memoryDBManager.GetNormalMounts, GroundMount75SpellsDictionary))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }

        public override void Run()
        {
            _pluginCacheManager.SanitizeDNSAndDNMLists();

            if (_ridingSkillToLearn != null)
            {
                Vector3 vendorPos = _ridingTrainer.Creature.GetSpawnPosition;

                if (!Helpers.TravelToVendorRange(_vendorTimerManager, _ridingTrainer, $"Learning {_ridingSkillToLearn.name_lang_1} at {_ridingTrainer.name}")
                    || Helpers.NpcIsAbsentOrDead(_blackListManager, _ridingTrainer))
                {
                    return;
                }

                for (int i = 0; i <= 5; i++)
                {
                    Logger.Log($"Attempt {i + 1}");
                    GoToTask.ToPositionAndIntecractWithNpc(_ridingTrainer.Creature.GetSpawnPosition, _ridingTrainer.entry, i);
                    Thread.Sleep(1000);
                    WTGossip.ShowAndExpandAvailableTrainerSpells();
                    Thread.Sleep(500);
                    if (WTGossip.IsTrainerGossipOpen)
                    {
                        WTGossip.LearnSpellByName(_ridingSkillToLearn.name_lang_1);
                        Thread.Sleep(2000);
                        Helpers.CloseWindow();
                        if (_pluginCacheManager.RidingSkill > _ridingSkillToLearn.NpcTrainer.ReqSkillRank)
                        {
                            Helpers.CloseWindow();
                            return;
                        }
                    }
                }

                Logger.Log($"Failed to learn {_ridingSkillToLearn.name_lang_1}, blacklisting vendor");
                _blackListManager.AddNPCToBlacklist(_ridingTrainer.entry);
            }

            if (_mountSpellToLearn != null)
            {
                Vector3 vendorPos = _mountVendor.Creature.GetSpawnPosition;

                if (!Helpers.TravelToVendorRange(_vendorTimerManager, _mountVendor, $"Buying {_mountSpellToLearn.name_lang_1} at {_mountVendor.name}")
                    || Helpers.NpcIsAbsentOrDead(_blackListManager, _mountVendor))
                {
                    return;
                }

                WTSettings.AddItemToDoNotSellAndMailList(new List<string>() { _mountSpellToLearn.AssociatedItem.Name });

                for (int i = 0; i <= 5; i++)
                {
                    Logger.Log($"Attempt {i + 1}");
                    GoToTask.ToPositionAndIntecractWithNpc(vendorPos, _mountVendor.entry, i);
                    Thread.Sleep(1000);
                    WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                    if (WTGossip.IsVendorGossipOpen)
                    {
                        Helpers.SellItems(_pluginCacheManager);
                        Thread.Sleep(1000);
                        WTGossip.BuyItem(_mountSpellToLearn.AssociatedItem.Name, 1, 1);
                        Thread.Sleep(3000);

                        if (_pluginCacheManager.BagItems.Exists(item => item.Entry == _mountSpellToLearn.AssociatedItem.Entry))
                        {
                            ItemsManager.UseItemByNameOrId(_mountSpellToLearn.AssociatedItem.Name);
                            Thread.Sleep(3000);
                            if (_pluginCacheManager.KnownMountSpells.Contains(_mountSpellToLearn.Id))
                            {
                                wManager.wManagerSetting.CurrentSetting.GroundMountName = _mountSpellToLearn.name_lang_1;
                                wManager.wManagerSetting.CurrentSetting.Save();
                                Helpers.CloseWindow();
                                return;
                            }
                        }
                    }
                    Helpers.CloseWindow();
                }

                Logger.Log($"Failed to buy {_mountSpellToLearn.AssociatedItem.Name}, blacklisting vendor");
                _blackListManager.AddNPCToBlacklist(_mountVendor.entry);
            }
        }

        private bool SetMountToBuy(List<ModelSpell> mountsList, Dictionary<int, List<uint>> mountDictionary)
        {
            // B11/Draenei exceptions
            if (mountsList[0].effectBasePoints_2 <= 100) // ground mounts
            {
                if (ObjectManager.Me.WowRace == WoWRace.Draenei && !_pluginCacheManager.IsInDraeneiStartingZone
                    || ObjectManager.Me.WowRace == WoWRace.BloodElf && !_pluginCacheManager.IsInBloodElfStartingZone)
                    return false;
            }
            else
            {
                if (_pluginCacheManager.IsInDraeneiStartingZone || _pluginCacheManager.IsInBloodElfStartingZone)
                    return false;
            }

            List<ModelSpell> availableMounts = mountsList
                .FindAll(m => m.AssociatedItem != null
                    && m.AssociatedItem.VendorsSellingThisItem.Count > 0
                    && m.AssociatedItem.VendorsSellingThisItem[0].CreatureTemplate.Creature?.map == Usefuls.ContinentId
                    && mountDictionary[(int)ObjectManager.Me.WowRace].Contains((uint)m.Id));

            if (availableMounts?.Count <= 0)
                return false;

            WVItem mountItemInBag = _pluginCacheManager.BagItems.Find(bi => availableMounts.Exists(am => am.AssociatedItem.Entry == bi.Entry));

            if (mountItemInBag != null)
            {
                ItemsManager.UseItemByNameOrId(mountItemInBag.Name);
                return false;
            }
            else
            {
                Random random = new Random();
                int index = random.Next(availableMounts.Count);
                ModelSpell mountSpell = availableMounts[index];
                ModelCreatureTemplate mountVendor = mountSpell.AssociatedItem.VendorsSellingThisItem[0].CreatureTemplate;
                if (mountVendor != null && mountSpell != null)
                {
                    _mountSpellToLearn = mountSpell;
                    _mountVendor = mountVendor;
                    return true;
                }
                return false;
            }
        }

        private bool SetRidingTraining(int ridingSpellId)
        {
            ModelSpell ridingSpell = _memoryDBManager.GetRidingSpellById(ridingSpellId);

            if (ridingSpell.effectBasePoints_2 <= 1 && _pluginCacheManager.IsInOutlands) // 75 / 150
                return false;

            if (ridingSpell.effectBasePoints_2 > 1 && !_pluginCacheManager.IsInOutlands) // 225 / 300
                return false;

            ModelCreatureTemplate ridingTrainer = GetNearestRidingTrainer(ridingSpell);
            if (ridingSpell != null && ridingTrainer != null)
            {
                _ridingSkillToLearn = ridingSpell;
                _ridingTrainer = ridingTrainer;
                return true;
            }
            return false;
        }

        private ModelCreatureTemplate GetNearestRidingTrainer(ModelSpell ridingSpell)
        {
            List<ModelCreatureTemplate> allVendors = ridingSpell.NpcTrainer.VendorTemplates;

            return allVendors
                .Where(vendor => _blackListManager.IsVendorValid(vendor))
                .Where(vendor => RidingTrainersDictionary[(int)ObjectManager.Me.WowRace].Contains((uint)vendor.entry))
                .OrderBy(vendor => ObjectManager.Me.Position.DistanceTo(vendor.Creature.GetSpawnPosition))
                .FirstOrDefault();
        }

        private readonly Dictionary<int, List<uint>> GroundMount75SpellsDictionary = new Dictionary<int, List<uint>>
        {
            { (int)WoWRace.Undead, new List<uint>{ 64977, 17463, 17464, 17462} },
            { (int)WoWRace.Orc, new List<uint>{ 64658, 6654, 6653, 580 } },
            { (int)WoWRace.Troll, new List<uint> { 8395, 10796, 10799} },
            { (int)WoWRace.Tauren, new List<uint> { 64657, 18990, 18989 } },
            { (int)WoWRace.BloodElf, new List<uint> { 35022, 35020, 35018, 34795 } },
            { (int)WoWRace.Human, new List<uint> { 458, 6648, 472} },
            { (int)WoWRace.Dwarf, new List<uint> { 6898, 6899, 6777 } },
            { (int)WoWRace.Gnome, new List<uint> { 10873, 10969, 17453, 17454 } },
            { (int)WoWRace.NightElf, new List<uint> { 8394, 10789, 10793 } },
            { (int)WoWRace.Draenei, new List<uint> { 34406, 35710, 35711 } },
        };

        private readonly Dictionary<int, List<uint>> GroundMount150SpellsDictionary = new Dictionary<int, List<uint>>
        {
            { (int)WoWRace.Undead, new List<uint>{ 17465, 66846, 23246} },
            { (int)WoWRace.Orc, new List<uint>{ 23250, 23252, 23251} },
            { (int)WoWRace.Troll, new List<uint> { 23241, 23242, 23243 } },
            { (int)WoWRace.Tauren, new List<uint> { 23249, 23248, 23247 } },
            { (int)WoWRace.BloodElf, new List<uint> { 35025, 33660, 35027 } },
            { (int)WoWRace.Human, new List<uint> { 23229, 23227, 23228 } },
            { (int)WoWRace.Dwarf, new List<uint> { 23238, 23239, 23240 } },
            { (int)WoWRace.Gnome, new List<uint> { 23222, 23223, 23225 } },
            { (int)WoWRace.NightElf, new List<uint> { 23219, 23221, 23338 } },
            { (int)WoWRace.Draenei, new List<uint> { 35712, 35713, 35714 } },
        };

        private readonly Dictionary<int, List<uint>> RidingTrainersDictionary = new Dictionary<int, List<uint>>
        {
            { (int)WoWRace.Undead, new List<uint>{ 20500, 28746, 31238, 31247, 35093, 35135, 4773 } },
            { (int)WoWRace.Orc, new List<uint>{ 20500, 28746, 31238, 31247, 35093, 35135, 4752 } },
            { (int)WoWRace.Troll, new List<uint> { 20500, 28746, 31238, 31247, 35093, 35135, 7953 } },
            { (int)WoWRace.Tauren, new List<uint> { 20500, 28746, 31238, 31247, 35093, 35135, 3690 } },
            { (int)WoWRace.BloodElf, new List<uint> { 20500, 28746, 31238, 31247, 35093, 35135, 16280 } },
            { (int)WoWRace.Human, new List<uint> { 20511, 28746, 31238, 31247, 35100, 35133, 4732 } },
            { (int)WoWRace.Dwarf, new List<uint> { 20511, 28746, 31238, 31247, 35100, 35133, 4772 } },
            { (int)WoWRace.Gnome, new List<uint> { 20511, 28746, 31238, 31247, 35100, 35133, 7954 } },
            { (int)WoWRace.NightElf, new List<uint> { 20511, 28746, 31238, 31247, 35100, 35133, 4753 } },
            { (int)WoWRace.Draenei, new List<uint> { 20511, 28746, 31238, 31247, 35100, 35133, 20914 } },
        };

        private bool Know75Mount => _pluginCacheManager.KnownMountSpells.Exists(ms => _memoryDBManager.GetNormalMounts.Exists(nm => nm.Id == ms));
        private bool Know150Mount => _pluginCacheManager.KnownMountSpells.Exists(ms => _memoryDBManager.GetEpicMounts.Exists(nm => nm.Id == ms));
        private bool Know225Mount => _pluginCacheManager.KnownMountSpells.Exists(ms => _memoryDBManager.GetFlyingMounts.Exists(nm => nm.Id == ms));
        private bool Know300Mount => _pluginCacheManager.KnownMountSpells.Exists(ms => _memoryDBManager.GetEpicFlyingMounts.Exists(nm => nm.Id == ms));
    }
}