using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;
using PoisonMaster;
using System.Threading;
using wManager;

public class TrainingState : State
{
    public override string DisplayName => "WV Training";

    private DatabaseNPC trainerNPC;
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
            if (!stateTimer.IsReady || !needToTrain)
                return false;

            stateTimer = new Timer(5000);
            trainerNPC = Database.GetTrainer();

            if (trainerNPC == null)
            {
                Main.Logger("Couldn´t find Trainer NPC");
                return false;
            }

            return true;
        }
    }

    public override void Run()
    {
        if (ObjectManager.Me.Position.DistanceTo(trainerNPC.Position) >= 6)
        {
            Main.Logger("Nearest Trainer from player:\n" + "Name: " + trainerNPC.Name + "[" + trainerNPC.Id + "]\nPosition: " + trainerNPC.Position.ToStringXml() + "\nDistance: " + trainerNPC.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
            GoToTask.ToPosition(trainerNPC.Position);
        }

        if (ObjectManager.Me.Position.DistanceTo(trainerNPC.Position) < 6)
        {
            if (Helpers.NpcIsAbsentOrDead(trainerNPC))
                return;

            GoToTask.ToPositionAndIntecractWithNpc(trainerNPC.Position, trainerNPC.Id);
            Trainer.TrainingSpell();
            Thread.Sleep(800 + Usefuls.Latency);
            SpellManager.UpdateSpellBook();
            PluginSettings.CurrentSetting.LastLevelTrained = (int)ObjectManager.Me.Level;
            PluginSettings.CurrentSetting.Save();
        }
    }
}
