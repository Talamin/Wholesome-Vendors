using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class BuyPoisonState : State
{
    public override string DisplayName => "Buying Poison";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private int InstantPoison;
    private int DeadlyPoison;
    private Timer stateTimer = new Timer();
    private DatabaseNPC poisonVendor;
    private int PoisonToBuy = 0;


    private readonly Dictionary<int, int> InstantPoisonDictionary = new Dictionary<int, int>
    {
        { 79, 43231 },
        { 73, 43230 },
        { 68, 21927 },
        { 60, 8928 },
        { 52, 8927 },
        { 44, 8926 },
        { 36, 6950 },
        { 28, 6949 },
        { 20, 6947 }
    };

    private readonly Dictionary<int, int> DeadlyPoisonDictionary = new Dictionary<int, int>
    {
        { 80, 43233 },
        { 76, 43232 },
        { 70, 22054 },
        { 62, 22053 },
        { 60, 20844 },
        { 54, 8985 },
        { 46, 8984 },
        { 38, 2893 },
        { 30, 2892 }
    };

    public override bool NeedToRun
    {
        get
        {
            if (!stateTimer.IsReady
                || !PluginSettings.CurrentSetting.AllowAutobuyPoison
                || ObjectManager.Me.WowClass != WoWClass.Rogue
                || Helpers.GetMoney < 1000
                || ObjectManager.Me.Level < 20)
                return false;

                stateTimer = new Timer(5000);

                poisonVendor = SelectBestPoisonVendor();

            if (ItemsManager.GetItemCountById((uint)InstantPoison) <= 0
                || ObjectManager.Me.Level >= 30 && ItemsManager.GetItemCountById((uint)DeadlyPoison) <= 0)
                {


                if (poisonVendor == null)
                {
                    Main.Logger("Couldn't find poison vendor");
                    return false;
                }
                return true;
            }
            return false;
        }
    }

    public override void Run()
    {
        Main.Logger("Nearest Vendor from player:\n" + "Name: " + poisonVendor.Name + "[" + poisonVendor.Id + "]\nPosition: " + poisonVendor.Position.ToStringXml() + "\nDistance: " + poisonVendor.Position.DistanceTo(Me.Position) + " yrds");
        int nbInstantPoisonToBuy = 20 - ItemsManager.GetItemCountById((uint)InstantPoison);
        int nbDeadlyPoisonToBuy = 20 - ItemsManager.GetItemCountById((uint)DeadlyPoison);

        if (Me.Position.DistanceTo(poisonVendor.Position) >= 6)
            GoToTask.ToPosition(poisonVendor.Position);

        if (Helpers.NpcIsAbsentOrDead(poisonVendor))
            return;

        // INSTANT POISON
        if (nbInstantPoisonToBuy > 0)
        {
            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(poisonVendor.Position, poisonVendor.Id, i);
                Helpers.BuyItem(ItemsManager.GetNameById(InstantPoison), nbInstantPoisonToBuy);
                Helpers.AddItemToDoNotSellList(ItemsManager.GetNameById(InstantPoison));
                Helpers.CloseWindow();
                Thread.Sleep(1000);
                if (ItemsManager.GetItemCountById((uint)InstantPoison) >= 20)
                    break;
            }
            Main.Logger($"Failed to buy {InstantPoison}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(poisonVendor.Id);
        }

        // DEADLY POISON
        if (Me.Level >= 30 && nbDeadlyPoisonToBuy > 0)
        {
            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(poisonVendor.Position, poisonVendor.Id, i);
                Helpers.BuyItem(ItemsManager.GetNameById(DeadlyPoison), 20);
                Helpers.AddItemToDoNotSellList(ItemsManager.GetNameById(DeadlyPoison));
                Helpers.CloseWindow();
                Thread.Sleep(1000);
                if (ItemsManager.GetItemCountById((uint)DeadlyPoison) >= 20)
                    break;
            }
            Main.Logger($"Failed to buy {DeadlyPoison}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(poisonVendor.Id);
        }
    }

    private DatabaseNPC SelectBestPoisonVendor()
    {
        poisonVendor = null;
        PoisonToBuy = 0;
        foreach(int poison in GetListUsablePoison())
        {
            DatabaseNPC vendorWithThisPoison = Database.GetPoisonVendor(new HashSet<int> { poison });
            if(vendorWithThisPoison != null)
            {
                //Main.Logger($"Found vendor {vendorWithThisPoison.Name} for item {ammo}");
                PoisonToBuy = poison;
                return vendorWithThisPoison;
            }
        }
    }
    private HashSet<int> GetListUsablePoison()
    {
        HashSet<int> listPoison = new HashSet<int>();
        foreach (KeyValuePair<int, int> instantPoison in InstantPoisonDictionary)
        {
            if (instantPoison.Key <= Me.Level)
                listPoison.Add(instantPoison.Value);  
        }
        foreach (KeyValuePair<int, int> deadlyPoison in DeadlyPoisonDictionary)
        {
            if (deadlyPoison.Key <= Me.Level)
                listPoison.Add(deadlyPoison.Value);
        }
        return listPoison;
    }


    private void SetPoisonToBuy()
    {
        foreach (KeyValuePair<int, int> instantPoison in InstantPoisonDictionary)
        {
            if (instantPoison.Key <= Me.Level)
            {
                InstantPoison = instantPoison.Value;
                Helpers.AddItemToDoNotSellList(ItemsManager.GetNameById(instantPoison.Value));
                break;
            }
        }

        foreach (KeyValuePair<int, int> deadlyPoison in DeadlyPoisonDictionary)
        {
            if (deadlyPoison.Key <= Me.Level)
            {
                DeadlyPoison = deadlyPoison.Value;
                Helpers.AddItemToDoNotSellList(ItemsManager.GetNameById(deadlyPoison.Value));
                break;
            }
        }
    }
}