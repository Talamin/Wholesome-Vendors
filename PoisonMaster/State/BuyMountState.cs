using PoisonMaster;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Vendors.Database;
using Wholesome_Vendors.Database.Models;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class BuyMountState : State
{
    public override string DisplayName { get; set; } = "WV Buying Mount";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private Timer stateTimer = new Timer();
    ModelSpell _ridingSkillToLearn;
    ModelSpell _mountSpellToLearn;
    ModelCreatureTemplate _ridingTrainer;
    ModelCreatureTemplate _mountVendor;

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || Me.Level < 20
                || !MemoryDB.IsPopulated
                || !PluginCache.Initialized
                || PluginCache.IsInInstance
                || !stateTimer.IsReady
                || Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);
            _ridingSkillToLearn = null;
            _mountSpellToLearn = null;
            _ridingTrainer = null;
            _mountVendor = null;

            // Epic mount
            if (PluginSettings.CurrentSetting.BuyEpicMount
                && Me.Level >= 40
                && !PluginCache.Know150Mount)
            {
                int neededMoney = 100000; // mount cost
                if (PluginCache.RidingSkill < 75) neededMoney += 40000; // training cost 75
                if (PluginCache.RidingSkill < 150) neededMoney += 500000; // training cost 75

                if (PluginCache.Money >= neededMoney)
                {
                    if (PluginCache.RidingSkill < 75)
                    {
                        if (SetRidingTraining(33388)) // Apprentice
                            return true;
                    }
                    else if (PluginCache.RidingSkill < 150)
                    {
                        if (SetRidingTraining(33391)) // Journeyman
                            return true;
                    }
                    else
                    {
                        if (SetMountToBuy(MemoryDB.GetEpicMounts, GroundMount150SpellsDictionary))
                            return true;
                    }
                }
            }

            // Normal mount
            if (PluginSettings.CurrentSetting.BuyGroundMount
                && !PluginCache.Know75Mount
                && !PluginCache.Know150Mount)
            {
                int neededMoney = 10000; // mount cost
                if (PluginCache.RidingSkill < 75) neededMoney += 40000; // training cost

                if (PluginCache.Money >= neededMoney)
                {
                    if (PluginCache.RidingSkill < 75)
                    {
                        if (SetRidingTraining(33388)) // Apprentice
                            return true;
                    }
                    else
                    {
                        if (SetMountToBuy(MemoryDB.GetNormalMounts, GroundMount75SpellsDictionary))
                            return true;
                    }
                }
            }

            return false;
        }
    }

    public override void Run()
    {
        if (_ridingSkillToLearn != null)
        {
            Main.Logger($"Learning {_ridingSkillToLearn.name_lang_1} at {_ridingTrainer.name}");
            Vector3 vendorPos = _ridingTrainer.Creature.GetSpawnPosition;

            if (Me.Position.DistanceTo(vendorPos) >= 10)
                GoToTask.ToPosition(vendorPos);

            if (Me.Position.DistanceTo(vendorPos) < 10)
            {
                if (Helpers.NpcIsAbsentOrDead(_ridingTrainer))
                    return;

                for (int i = 0; i <= 5; i++)
                {
                    Main.Logger($"Attempt {i + 1}");
                    GoToTask.ToPositionAndIntecractWithNpc(_ridingTrainer.Creature.GetSpawnPosition, _ridingTrainer.entry, i);
                    Thread.Sleep(1000);
                    Lua.LuaDoString($"SetTrainerServiceTypeFilter('available', 1)");
                    Lua.LuaDoString($"ExpandTrainerSkillLine(0)");
                    Thread.Sleep(500);
                    if (Helpers.IsTrainerGossipOpen())
                    {
                        Main.Logger("OPEN");
                        Helpers.LearnSpellByName(_ridingSkillToLearn.name_lang_1);
                        Thread.Sleep(2000);
                        Helpers.CloseWindow();
                        if (PluginCache.RidingSkill > _ridingSkillToLearn.NpcTrainer.ReqSkillRank)
                        {
                            Helpers.CloseWindow();
                            return;
                        }
                    }
                }

                Main.Logger($"Failed to learn {_ridingSkillToLearn.name_lang_1}, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(_ridingTrainer.entry);
            }
        }

        if (_mountSpellToLearn != null)
        {
            Main.Logger($"Buying {_mountSpellToLearn.name_lang_1} at {_mountVendor.name}");
            Vector3 vendorPos = _mountVendor.Creature.GetSpawnPosition;

            if (Me.Position.DistanceTo(vendorPos) >= 10)
                GoToTask.ToPosition(vendorPos);

            if (Me.Position.DistanceTo(vendorPos) < 10)
            {
                if (Helpers.NpcIsAbsentOrDead(_mountVendor))
                    return;

                Helpers.AddItemToDoNotSellList(_mountSpellToLearn.AssociatedItem.Name);

                for (int i = 0; i <= 5; i++)
                {
                    Main.Logger($"Attempt {i + 1}");
                    GoToTask.ToPositionAndIntecractWithNpc(vendorPos, _mountVendor.entry, i);
                    Thread.Sleep(1000);
                    Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                    if (Helpers.IsVendorGossipOpen())
                    {
                        Helpers.SellItems(_mountVendor);
                        Thread.Sleep(1000);
                        Helpers.BuyItem(_mountSpellToLearn.AssociatedItem.Name, 1, 1);
                        Thread.Sleep(3000);

                        if (PluginCache.BagItems.Exists(item => item.Entry == _mountSpellToLearn.AssociatedItem.Entry))
                        {
                            ItemsManager.UseItemByNameOrId(_mountSpellToLearn.AssociatedItem.Name);
                            Thread.Sleep(3000);
                            if (PluginCache.KnownMountSpells.Contains(_mountSpellToLearn.Id))
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
                Main.Logger($"Failed to buy {_mountSpellToLearn.AssociatedItem.Name}, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(_mountVendor.entry);
            }
        }
    }

    private bool SetMountToBuy(List<ModelSpell> mountsList, Dictionary<int, List<uint>> mountDictionary)
    {
        // B11/Draenei exceptions
        if (mountsList[0].effectBasePoints_2 <= 100) // ground mounts
        {
            if ((ObjectManager.Me.WowRace == WoWRace.Draenei && !PluginCache.IsInDraeneiStartingZone)
                || (ObjectManager.Me.WowRace == WoWRace.BloodElf && !PluginCache.IsInBloodElfStartingZone))
                return false;
        }
        else
        {
            if (PluginCache.IsInDraeneiStartingZone || PluginCache.IsInBloodElfStartingZone)
                return false;
        }

        List<ModelSpell> availableMounts = mountsList
            .FindAll(m => m.AssociatedItem != null
                && m.AssociatedItem.VendorsSellingThisItem.Count > 0
                && m.AssociatedItem.VendorsSellingThisItem[0].CreatureTemplate.Creature?.map == Usefuls.ContinentId
                && mountDictionary[(int)ObjectManager.Me.WowRace].Contains((uint)m.Id));

        if (availableMounts?.Count <= 0)
            return false;

        WoWItem mountItemInBag = PluginCache.BagItems.Find(bi => availableMounts.Exists(am => am.AssociatedItem.Entry == bi.Entry));

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
        ModelSpell ridingSpell = MemoryDB.GetRidingSpellById(ridingSpellId);

        if (ridingSpell.effectBasePoints_2 <= 1 && PluginCache.IsInOutlands) // 75 / 150
            return false;

        if (ridingSpell.effectBasePoints_2 > 1 && !PluginCache.IsInOutlands) // 225 / 300
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
            .Where(vendor => NPCBlackList.IsVendorValid(vendor))
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
}
