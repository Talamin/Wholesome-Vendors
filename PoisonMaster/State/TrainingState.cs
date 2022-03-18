using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using Wholesome_Vendors.Database;
using Wholesome_Vendors.Database.Models;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class TrainingState : State
{
    public override string DisplayName => "WV Training";

    private ModelCreatureTemplate TrainerNpc;
    private Timer stateTimer = new Timer();

    private List<int> levelstoTrain => PluginSettings.CurrentSetting.TrainLevels.Count > 0 ? PluginSettings.CurrentSetting.TrainLevels : new List<int>
        {2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28,
        30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56,
        58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 80 };

    private int LevelToTrain => levelstoTrain.Find(l => (int)ObjectManager.Me.Level >= l && PluginSettings.CurrentSetting.LastLevelTrained < l);

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !stateTimer.IsReady
                || LevelToTrain <= 0
                || !PluginSettings.CurrentSetting.AllowTrain
                || ObjectManager.Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);

            if ((ContinentId)Usefuls.ContinentId == ContinentId.Northrend
                || Helpers.PlayerIsInOutland()
                || (ContinentId)Usefuls.ContinentId == ContinentId.Azeroth && ObjectManager.Me.WowClass == WoWClass.Druid)
                return false;

            TrainerNpc = MemoryDB.GetNearestTrainer();

            return TrainerNpc != null;
        }
    }

    public override void Run()
    {
        Main.Logger($"Going to {TrainerNpc.subname} {TrainerNpc.name}");

        if (ObjectManager.Me.Position.DistanceTo(TrainerNpc.Creature.GetSpawnPosition) >= 10)
            GoToTask.ToPosition(TrainerNpc.Creature.GetSpawnPosition);

        if (ObjectManager.Me.Position.DistanceTo(TrainerNpc.Creature.GetSpawnPosition) < 30)
        {
            if (Helpers.NpcIsAbsentOrDead(TrainerNpc))
                return;

            GoToTask.ToPositionAndIntecractWithNpc(TrainerNpc.Creature.GetSpawnPosition, TrainerNpc.entry);
            Trainer.TrainingSpell();
            Thread.Sleep(800 + Usefuls.Latency);
            SpellManager.UpdateSpellBook();
            PluginSettings.CurrentSetting.LastLevelTrained = (int)ObjectManager.Me.Level;
            PluginSettings.CurrentSetting.Save();
        }
    }
}
