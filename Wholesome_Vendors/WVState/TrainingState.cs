using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using WholesomeToolbox;
using WholesomeVendors.Database;
using WholesomeVendors.Database.Models;
using WholesomeVendors.WVSettings;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

namespace WholesomeVendors.WVState
{
    public class TrainingState : State
    {
        public override string DisplayName => "WV Training";

        private ModelCreatureTemplate _trainerNpc;
        private Timer _stateTimer = new Timer();

        private List<int> _levelstoTrain => PluginSettings.CurrentSetting.TrainLevels.Count > 0 ? PluginSettings.CurrentSetting.TrainLevels : new List<int>
        {2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28,
        30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56,
        58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 80 };

        private int LevelToTrain => _levelstoTrain.Find(l => (int)ObjectManager.Me.Level >= l && PluginSettings.CurrentSetting.LastLevelTrained < l);

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !Main.IsLaunched
                    || !_stateTimer.IsReady
                    || !MemoryDB.IsPopulated
                    || !PluginCache.Initialized
                    || PluginCache.IsInInstance
                    || LevelToTrain <= 0
                    || !PluginSettings.CurrentSetting.AllowTrain
                    || ObjectManager.Me.IsOnTaxi)
                    return false;

                _stateTimer = new Timer(5000);

                if ((ContinentId)Usefuls.ContinentId == ContinentId.Northrend
                    || WTLocation.PlayerInOutlands()
                    || (ContinentId)Usefuls.ContinentId == ContinentId.Azeroth && ObjectManager.Me.WowClass == WoWClass.Druid)
                    return false;

                _trainerNpc = MemoryDB.GetNearestTrainer();

                return _trainerNpc != null;
            }
        }

        public override void Run()
        {
            Main.Logger($"Going to {_trainerNpc.subname} {_trainerNpc.name}");

            if (ObjectManager.Me.Position.DistanceTo(_trainerNpc.Creature.GetSpawnPosition) >= 10)
                GoToTask.ToPosition(_trainerNpc.Creature.GetSpawnPosition);

            if (ObjectManager.Me.Position.DistanceTo(_trainerNpc.Creature.GetSpawnPosition) < 30)
            {
                if (Helpers.NpcIsAbsentOrDead(_trainerNpc))
                    return;

                GoToTask.ToPositionAndIntecractWithNpc(_trainerNpc.Creature.GetSpawnPosition, _trainerNpc.entry);
                Trainer.TrainingSpell();
                Thread.Sleep(800 + Usefuls.Latency);
                SpellManager.UpdateSpellBook();
                PluginSettings.CurrentSetting.LastLevelTrained = (int)ObjectManager.Me.Level;
                PluginSettings.CurrentSetting.Save();
                Helpers.CloseWindow();
            }
        }
    }
}