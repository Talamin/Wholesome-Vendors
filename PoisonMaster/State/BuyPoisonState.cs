using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static PluginSettings;
using Timer = robotManager.Helpful.Timer;

public class BuyPoisonState : State
{
    public override string DisplayName => "WV Buying Poison";

    private WoWLocalPlayer Me = ObjectManager.Me;

    private int InstantPoisonIdToBuy;
    private int NbInstandPoisonToBuy => 20 - ItemsManager.GetItemCountById((uint)InstantPoisonIdToBuy);
    private string InstantPoisonNameToBuy;

    private int DeadlyPoisonIdToBuy;
    private int NbDeadlyPoisonToBuy => 20 - ItemsManager.GetItemCountById((uint)DeadlyPoisonIdToBuy);
    private string DeadlyPoisonNameToBuy;

    private Timer stateTimer = new Timer();
    private DatabaseNPC PoisonVendor;

    private bool NeedInstantPoison => ItemsManager.GetItemCountById((uint)InstantPoisonIdToBuy) <= 0;
    private bool NeedDeadlyPoison => ObjectManager.Me.Level >= 30 && ItemsManager.GetItemCountById((uint)DeadlyPoisonIdToBuy) <= 0;

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
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !stateTimer.IsReady
                || !CurrentSetting.AllowAutobuyPoison
                || ObjectManager.Me.WowClass != WoWClass.Rogue
                || ObjectManager.Me.Level < 20
                || Me.IsOnTaxi)
                return false;

            if (Usefuls.ContinentId != 0 || Usefuls.ContinentId != 1 || Usefuls.ContinentId != 530 || Usefuls.ContinentId != 571)
                return false;

            stateTimer = new Timer(5000);

            SetPoisonAndVendor();

            if (InstantPoisonIdToBuy != 0 && NeedInstantPoison || DeadlyPoisonIdToBuy != 0 && NeedDeadlyPoison)
                return PoisonVendor != null;

            return false;
        }
    }

    public override void Run()
    {
        Main.Logger($"Buying poisons at vendor {PoisonVendor.Name}");

        Helpers.CheckMailboxNearby(PoisonVendor);

        if (Me.Position.DistanceTo(PoisonVendor.Position) >= 10)
            GoToTask.ToPosition(PoisonVendor.Position);

        if (Me.Position.DistanceTo(PoisonVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(PoisonVendor))
                return;

            List<string> allPoisonsNames = GetPotentialPoisonsNames();

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(PoisonVendor.Position, PoisonVendor.Id, i);
                Thread.Sleep(500);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.OpenRecordVendorItems(allPoisonsNames)) // also checks if vendor window is open
                {
                    // Sell first
                    Helpers.SellItems(PoisonVendor);

                    if (NbInstandPoisonToBuy > 0 && InstantPoisonIdToBuy > 0)
                    {
                        if (!Helpers.HaveEnoughMoneyFor(NbInstandPoisonToBuy, InstantPoisonNameToBuy))
                        {
                            Main.Logger("Not enough money. Item prices sold by this vendor are now recorded.");
                            Helpers.CloseWindow();
                            break;
                        }
                        VendorItem vendorItem = CurrentSetting.VendorItems.Find(item => item.Name == InstantPoisonNameToBuy);
                        Helpers.BuyItem(InstantPoisonNameToBuy, NbInstandPoisonToBuy, vendorItem.Stack);
                        Helpers.CloseWindow();
                        Thread.Sleep(1000);
                        if (!NeedInstantPoison)
                            return;
                    }

                    if (Me.Level >= 30 && NbDeadlyPoisonToBuy > 0 && DeadlyPoisonIdToBuy > 0)
                    {
                        if (!Helpers.HaveEnoughMoneyFor(NbDeadlyPoisonToBuy, DeadlyPoisonNameToBuy))
                        {
                            Main.Logger("Not enough money. Item prices for this vendor are now recorded.");
                            Helpers.CloseWindow();
                            break;
                        }
                        VendorItem vendorItem = CurrentSetting.VendorItems.Find(item => item.Name == DeadlyPoisonNameToBuy);
                        Helpers.BuyItem(DeadlyPoisonNameToBuy, NbDeadlyPoisonToBuy, vendorItem.Stack);
                        Helpers.CloseWindow();
                        Thread.Sleep(1000);
                        if (!NeedDeadlyPoison)
                            return;
                    }
                }
            }

            if (NeedDeadlyPoison || NeedInstantPoison)
            {
                Main.Logger($"Failed to buy poisons, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(PoisonVendor.Id);
            }
        }
    }

    private List<string> GetPotentialPoisonsNames()
    {
        List<string> allPoisons = new List<string>();

        foreach (KeyValuePair<int, int> instant in InstantPoisonDictionary)
            allPoisons.Add(Database.GetItemName(instant.Value));
        foreach (KeyValuePair<int, int> deadly in DeadlyPoisonDictionary)
            allPoisons.Add(Database.GetItemName(deadly.Value));

        return allPoisons;
    }

    private void ClearDoNotSellListFromInstants()
    {
        foreach (KeyValuePair<int, int> instant in InstantPoisonDictionary)
            Helpers.RemoveItemFromDoNotSellList(Database.GetItemName(instant.Value));
    }

    private void ClearDoNotSellListFromDeadlies()
    {
        foreach (KeyValuePair<int, int> deadly in DeadlyPoisonDictionary)
            Helpers.RemoveItemFromDoNotSellList(Database.GetItemName(deadly.Value));
    }

    private void SetPoisonAndVendor()
    {
        PoisonVendor = null;
        InstantPoisonIdToBuy = 0;
        DeadlyPoisonIdToBuy = 0;

        if (NeedDeadlyPoison)
        {
            foreach (int deadly in GetListUsableDeadlyPoison())
            {
                DatabaseNPC vendorWithThisPoison = Database.GetPoisonVendor(new HashSet<int> { deadly });
                if (vendorWithThisPoison != null && Helpers.HaveEnoughMoneyFor(NbDeadlyPoisonToBuy, Database.GetItemName(deadly)))
                {
                    DeadlyPoisonIdToBuy = deadly;
                    DeadlyPoisonNameToBuy = Database.GetItemName(deadly);
                    PoisonVendor = vendorWithThisPoison;
                    ClearDoNotSellListFromDeadlies();
                    Helpers.AddItemToDoNotSellList(DeadlyPoisonNameToBuy);
                    Helpers.AddItemToDoNotMailList(DeadlyPoisonNameToBuy);
                    break;
                }
            }
        }

        if (NeedInstantPoison)
        {
            foreach (int instant in GetListUsableInstantPoison())
            {
                DatabaseNPC vendorWithThisPoison = Database.GetPoisonVendor(new HashSet<int> { instant });
                if (vendorWithThisPoison != null && Helpers.HaveEnoughMoneyFor(NbInstandPoisonToBuy, Database.GetItemName(instant)))
                {
                    InstantPoisonIdToBuy = instant;
                    InstantPoisonNameToBuy = Database.GetItemName(instant);
                    PoisonVendor = vendorWithThisPoison;
                    ClearDoNotSellListFromInstants();
                    Helpers.AddItemToDoNotSellList(InstantPoisonNameToBuy);
                    Helpers.AddItemToDoNotMailList(InstantPoisonNameToBuy);
                    break;
                }
            }
        }
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