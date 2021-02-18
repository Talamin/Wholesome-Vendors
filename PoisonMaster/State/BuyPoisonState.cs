using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
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

    private bool NeedInstantPoison => ItemsManager.GetItemCountById((uint)InstantPoison) <= 0;
    private bool NeedDeadlyPoison => ObjectManager.Me.Level >= 30 && ItemsManager.GetItemCountById((uint)DeadlyPoison) <= 0;

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

            if (InstantPoison != 0 && NeedInstantPoison || DeadlyPoison != 0 && NeedDeadlyPoison)
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

        if (Me.Position.DistanceTo(poisonVendor.Position) < 6)
        {
            if (Helpers.NpcIsAbsentOrDead(poisonVendor))
                return;

            // INSTANT POISON
            if (nbInstantPoisonToBuy > 0)
            {
                for (int i = 0; i <= 5; i++)
                {
                    GoToTask.ToPositionAndIntecractWithNpc(poisonVendor.Position, poisonVendor.Id, i);
                    Helpers.BuyItem(ItemsManager.GetNameById(InstantPoison), nbInstantPoisonToBuy, 1);
                    Helpers.AddItemToDoNotSellList(ItemsManager.GetNameById(InstantPoison));
                    Helpers.CloseWindow();
                    Thread.Sleep(1000);
                    if (!NeedInstantPoison)
                        break;
                }

                if (NeedInstantPoison)
                {
                    Main.Logger($"Failed to buy {InstantPoison}, blacklisting vendor");
                    NPCBlackList.AddNPCToBlacklist(poisonVendor.Id);
                }
            }

            // DEADLY POISON
            if (Me.Level >= 30 && nbDeadlyPoisonToBuy > 0)
            {
                for (int i = 0; i <= 5; i++)
                {
                    GoToTask.ToPositionAndIntecractWithNpc(poisonVendor.Position, poisonVendor.Id, i);
                    Helpers.BuyItem(ItemsManager.GetNameById(DeadlyPoison), nbDeadlyPoisonToBuy, 1);
                    Helpers.AddItemToDoNotSellList(ItemsManager.GetNameById(DeadlyPoison));
                    Helpers.CloseWindow();
                    Thread.Sleep(1000);
                    if (!NeedDeadlyPoison)
                        break;
                }

                if (NeedDeadlyPoison)
                {
                    Main.Logger($"Failed to buy {DeadlyPoison}, blacklisting vendor");
                    NPCBlackList.AddNPCToBlacklist(poisonVendor.Id);
                }
            }
        }
    }

    private DatabaseNPC SelectBestPoisonVendor()
    {
        poisonVendor = null;
        InstantPoison = 0;
        DeadlyPoison = 0;
        DatabaseNPC vendor = null;

        if (NeedDeadlyPoison)
        {
            foreach (int deadly in GetListUsableDeadlyPoison())
            {
                DatabaseNPC vendorWithThisPoison = Database.GetPoisonVendor(new HashSet<int> { deadly });
                if (vendorWithThisPoison != null)
                {
                    //Main.Logger($"Found vendor {vendorWithThisPoison.Name} for item {deadly}");
                    DeadlyPoison = deadly;
                    vendor = vendorWithThisPoison;
                    break;
                }
            }
        }

        if (NeedInstantPoison)
        {
            foreach (int instant in GetListUsableInstantPoison())
            {
                DatabaseNPC vendorWithThisPoison = Database.GetPoisonVendor(new HashSet<int> { instant });
                if (vendorWithThisPoison != null)
                {
                    //Main.Logger($"Found vendor {vendorWithThisPoison.Name} for item {instant}");
                    InstantPoison = instant;
                    vendor = vendorWithThisPoison;
                    break;
                }
            }
        }
        return vendor;
    }

    private HashSet<int> GetListUsableInstantPoison()
    {
        HashSet<int> listInstants = new HashSet<int>();
        foreach (KeyValuePair<int, int> instant in InstantPoisonDictionary)
        {
            if (instant.Key <= Me.Level)
                listInstants.Add(instant.Value);
        }
        return listInstants;
    }

    private HashSet<int> GetListUsableDeadlyPoison()
    {
        HashSet<int> listDeadlys = new HashSet<int>();
        foreach (KeyValuePair<int, int> deadly in DeadlyPoisonDictionary)
        {
            if (deadly.Key <= Me.Level)
                listDeadlys.Add(deadly.Value);
        }
        return listDeadlys;
    }
}