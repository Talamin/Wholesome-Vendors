using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;
using PoisonMaster;
using System.Threading;
using wManager;
using wManager.Wow.Enums;

public class TrainingState : State
{
    public override string DisplayName => "WV Training";

    private DatabaseNPC TrainerVendor;
    private Timer stateTimer = new Timer();
    private bool needToTrain => leveltoTrain.Exists(l => (int)ObjectManager.Me.Level >= l && PluginSettings.CurrentSetting.LastLevelTrained < l);

    private List<int> leveltoTrain = new List<int>
    {2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 
        30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56, 
        58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 80 };
    
    public override bool NeedToRun
    {
        get
        {
            if (!stateTimer.IsReady 
                || !needToTrain 
                || !PluginSettings.CurrentSetting.AutoTrain
                || ObjectManager.Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);

            if ((ContinentId) Usefuls.ContinentId == ContinentId.Northrend
                || Helpers.PlayerIsInOutland())
                return false;

            TrainerVendor = Database.GetTrainer();

            if (TrainerVendor == null)
            {
                Main.Logger("Couldn´t find Trainer NPC");
                return false;
            }

            return true;
        }
    }

    public override void Run()
    {
        Main.Logger($"Going to trainer {TrainerVendor.Name}");

        if (ObjectManager.Me.Position.DistanceTo(TrainerVendor.Position) >= 10)
            GoToTask.ToPosition(TrainerVendor.Position);

        if (ObjectManager.Me.Position.DistanceTo(TrainerVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(TrainerVendor))
                return;

            GoToTask.ToPositionAndIntecractWithNpc(TrainerVendor.Position, TrainerVendor.Id);
            Trainer.TrainingSpell();
            Thread.Sleep(800 + Usefuls.Latency);
            SpellManager.UpdateSpellBook();
            PluginSettings.CurrentSetting.LastLevelTrained = (int)ObjectManager.Me.Level;
            PluginSettings.CurrentSetting.Save();
        }
    }
}
