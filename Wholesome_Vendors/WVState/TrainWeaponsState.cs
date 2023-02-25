using robotManager.FiniteStateMachine;
using robotManager.Helpful;
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
using Timer = robotManager.Helpful.Timer;

namespace WholesomeVendors.WVState
{
    public class TrainWeaponsState : State
    {
        public override string DisplayName { get; set; } = "WV Training Weapons";

        private readonly IPluginCacheManager _pluginCacheManager;
        private readonly IMemoryDBManager _memoryDBManager;
        private readonly IVendorTimerManager _vendorTimerManager;
        private readonly IBlackListManager _blackListManager;

        private ModelCreatureTemplate _trainerNpc;
        private ModelSpell _weaponSpell;
        private Timer _stateTimer = new Timer();
        private bool _enabledInSetting;

        public TrainWeaponsState(
            IMemoryDBManager memoryDBManager,
            IPluginCacheManager pluginCacheManager,
            IVendorTimerManager vendorTimerManager,
            IBlackListManager blackListManager)
        {
            _memoryDBManager = memoryDBManager;
            _pluginCacheManager = pluginCacheManager;
            _vendorTimerManager = vendorTimerManager;
            _blackListManager = blackListManager;
            _enabledInSetting = PluginSettings.CurrentSetting.AllowWeaponTrain;
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Main.IsLaunched
                    || !_enabledInSetting
                    || !_stateTimer.IsReady
                    || _pluginCacheManager.WeaponsSpellsToLearn.Count <= 0
                    || _pluginCacheManager.InLoadingScreen
                    || ObjectManager.Me.Level < 20
                    || _pluginCacheManager.Money < 1000
                    || Fight.InFight
                    || ObjectManager.Me.IsOnTaxi
                    || _pluginCacheManager.IsInInstance
                    || _pluginCacheManager.IsInOutlands
                    || (ContinentId)Usefuls.ContinentId == ContinentId.Northrend
                    || !Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                {
                    return false;
                }

                foreach ((SkillLine skill, int spell) skillToLearn in _pluginCacheManager.WeaponsSpellsToLearn)
                {
                    _weaponSpell = _memoryDBManager.GetWeaponSpellById(skillToLearn.spell);
                    // OH bug
                    if (_pluginCacheManager.KnownSkills.Contains("Swords")
                        && _weaponSpell.name_lang_1 == "One-Handed Swords")
                    {
                        continue;
                    }
                    // OH bug
                    if (_pluginCacheManager.KnownSkills.Contains("Maces")
                        && _weaponSpell.name_lang_1 == "One-Handed Maces")
                    {
                        continue;
                    }
                    // OH bug
                    if (_pluginCacheManager.KnownSkills.Contains("Axes")
                        && _weaponSpell.name_lang_1 == "One-Handed Axes")
                    {
                        continue;
                    }
                    _trainerNpc = _memoryDBManager.GetNearestWeaponsTrainer(skillToLearn.spell);
                    if (_trainerNpc != null)
                    {
                        DisplayName = $"Learning {_weaponSpell.name_lang_1} at {_trainerNpc.subname} {_trainerNpc.name}";
                        return true;
                    }
                }

                return false;
            }
        }

        public override void Run()
        {
            Vector3 trainerPosition = _trainerNpc.Creature.GetSpawnPosition;

            if (!Helpers.TravelToVendorRange(_vendorTimerManager, _trainerNpc, DisplayName)
                || Helpers.NpcIsAbsentOrDead(_blackListManager, _trainerNpc))
            {
                return;
            }

            for (int i = 0; i <= 5; i++)
            {
                Logger.Log($"Attempt {i + 1}");
                GoToTask.ToPositionAndIntecractWithNpc(trainerPosition, _trainerNpc.entry, i);
                Thread.Sleep(1000);
                WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                if (Lua.LuaDoString<int>($"return ClassTrainerFrame:IsVisible();") > 0)
                {
                    WTGossip.ShowAndExpandAvailableTrainerSpells();
                    bool success = Lua.LuaDoString<bool>($@"
                        SetTrainerServiceTypeFilter(""available"", 1, 1);
                        ExpandTrainerSkillLine(0);
                        for i = 1, GetNumTrainerServices() do
                            local serviceName, serviceSubText, serviceType, isExpanded = GetTrainerServiceInfo(i);
                            if (serviceType ~= 'header' and serviceName == '{_weaponSpell.name_lang_1}') then
                                BuyTrainerService(i);
                                return true;
                            end
                        end
                        return false;
                    ");

                    if (success)
                    {
                        Thread.Sleep(1000);
                        Logger.Log($"Successfully learned {_weaponSpell.name_lang_1}");
                        Helpers.CloseWindow();
                        return;
                    }
                }
                Helpers.CloseWindow();
            }

            Logger.Log($"Failed to train {_weaponSpell.name_lang_1}, blacklisting {_trainerNpc.name}");
            _blackListManager.AddNPCToBlacklist(_trainerNpc.entry);
        }
    }
}