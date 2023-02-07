using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
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
    public class TrainingState : State
    {
        public override string DisplayName { get; set; } = "WV Training";

        private readonly IPluginCacheManager _pluginCacheManager;
        private readonly IMemoryDBManager _memoryDBManager;
        private readonly IVendorTimerManager _vendorTimerManager;
        private readonly IBlackListManager _blackListManager;

        private ModelCreatureTemplate _trainerNpc;

        private List<int> _levelstoTrain => PluginSettings.CurrentSetting.TrainLevels.Count > 0 ? PluginSettings.CurrentSetting.TrainLevels : new List<int>
        {2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28,
        30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56,
        58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 80 };

        private int LevelToTrain => _levelstoTrain.Find(l => (int)ObjectManager.Me.Level >= l && PluginSettings.CurrentSetting.LastLevelTrained < l);

        public TrainingState(
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
                    || LevelToTrain <= 0
                    || _pluginCacheManager.InLoadingScreen
                    || Fight.InFight
                    || !PluginSettings.CurrentSetting.AllowTrain
                    || ObjectManager.Me.IsOnTaxi
                    || _pluginCacheManager.IsInInstance
                    || _pluginCacheManager.IsInOutlands
                    || (ContinentId)Usefuls.ContinentId == ContinentId.Northrend
                    || (ContinentId)Usefuls.ContinentId == ContinentId.Azeroth && ObjectManager.Me.WowClass == WoWClass.Druid
                    || !Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                {
                    return false;
                }

                _trainerNpc = _memoryDBManager.GetNearestTrainer();

                if (_trainerNpc != null)
                {
                    DisplayName = $"Training at {_trainerNpc.subname} {_trainerNpc.name}";
                    return true;
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
                if (Lua.LuaDoString<int>($"return ClassTrainerFrame:IsVisible()") > 0)
                {
                    Trainer.TrainingSpell();
                    Thread.Sleep(800 + Usefuls.Latency);
                    SpellManager.UpdateSpellBook();
                    PluginSettings.CurrentSetting.LastLevelTrained = (int)ObjectManager.Me.Level;
                    PluginSettings.CurrentSetting.Save();
                    Helpers.CloseWindow();
                    return;
                }
                Helpers.CloseWindow();
            }
        }
    }
}