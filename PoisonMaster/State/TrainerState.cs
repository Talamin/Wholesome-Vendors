using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;
using PoisonMaster;
using System.Threading;
using wManager;

public class Trainer : State
{
    public override string DisplayName => "Buying Ammunition";

    private WoWLocalPlayer Me = ObjectManager.Me;


    public override bool NeedToRun
    {
        get
        {

        }
    }

    public override void Run()
    {

    }


}
