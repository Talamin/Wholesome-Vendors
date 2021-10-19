using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;
using PoisonMaster;
using System.Threading;
using static PluginSettings;

public class BuyAmmoState : State
{
    public override string DisplayName => "WV Buying Ammunition";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private Timer stateTimer = new Timer();
    private DatabaseNPC AmmoVendor;
    private int AmmoIdToBuy;
    private string AmmoNameToBuy;
    private int AmmoAmountToBuy => CurrentSetting.AutobuyAmmunitionAmount;

    private readonly Dictionary<int, int> ArrowDictionary = new Dictionary<int, int>
    {
        { 75, 41586 },
        { 65, 28056 },
        { 55, 28053 },
        { 40, 11285 },
        { 25, 3030 },
        { 10, 2515 },
        { 1, 2512 },
    };

    private readonly Dictionary<int, int> BulletsDictionary = new Dictionary<int, int>
    {
        {75 ,41584},
        {65 ,28061},
        {55, 28060},
        {40, 11284},
        {25, 3033},
        {10, 2519 },
        {1, 2516 },
    };

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || CurrentSetting.AutobuyAmmunitionAmount <= 0
                || !stateTimer.IsReady
                || Helpers.GetRangedWeaponType() == null
                || Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);

            SetAmmoAndVendor();
            
            if (AmmoIdToBuy > 0
                && GetNbAmmosInBags() <= 50)
                return AmmoVendor != null;

            return false;
        }
    }

    public override void Run()
    {
        Main.Logger($"Buying {AmmoAmountToBuy} x {AmmoNameToBuy} [{AmmoIdToBuy}] at vendor {AmmoVendor.Name}");

        Helpers.CheckMailboxNearby(AmmoVendor);

        if (Me.Position.DistanceTo(AmmoVendor.Position) >= 10)
            GoToTask.ToPositionAndIntecractWithNpc(AmmoVendor.Position, AmmoVendor.Id);

        if (Me.Position.DistanceTo(AmmoVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(AmmoVendor))
                return;

            ClearDoNotSellListFromAmmos();
            Helpers.AddItemToDoNotSellList(AmmoNameToBuy);
            Helpers.AddItemToDoNotMailList(AmmoNameToBuy);
 
            List<string> allAmmoNames = GetPotentialAmmoNames();

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(AmmoVendor.Position, AmmoVendor.Id, i);
                Thread.Sleep(500);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.OpenRecordVendorItems(allAmmoNames)) // also checks if vendor window is open
                {
                    // Sell first
                    Helpers.SellItems(AmmoVendor);
                    if (!Helpers.HaveEnoughMoneyFor(AmmoAmountToBuy, AmmoNameToBuy))
                    {
                        Main.Logger("Not enough money. Item prices sold by this vendor are now recorded.");
                        Helpers.CloseWindow();
                        return;
                    }
                    VendorItem vendorItem = CurrentSetting.VendorItems.Find(item => item.Name == AmmoNameToBuy);
                    Helpers.BuyItem(AmmoNameToBuy, AmmoAmountToBuy, vendorItem.Stack);
                    Helpers.CloseWindow();
                    Thread.Sleep(1000);
                    if (ItemsManager.GetItemCountById((uint)AmmoIdToBuy) >= AmmoAmountToBuy)
                        return;
                }
            }
            Main.Logger($"Failed to buy {AmmoIdToBuy}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(AmmoVendor.Id);
        }
    }

    private List<string> GetPotentialAmmoNames()
    {
        List<string> allAmmos = new List<string>();

        foreach (KeyValuePair<int, int> arrow in ArrowDictionary)
            allAmmos.Add(Database.GetItemName(arrow.Value));
        foreach (KeyValuePair<int, int> bullet in BulletsDictionary)
            allAmmos.Add(Database.GetItemName(bullet.Value));

        return allAmmos;
    }

    private void ClearDoNotSellListFromAmmos()
    {
        foreach (KeyValuePair<int, int> arrow in ArrowDictionary)
            Helpers.RemoveItemFromDoNotSellList(Database.GetItemName(arrow.Value));

        foreach (KeyValuePair<int, int> bullet in BulletsDictionary)
            Helpers.RemoveItemFromDoNotSellList(Database.GetItemName(bullet.Value));
    }

    private void SetAmmoAndVendor()
    {
        AmmoVendor = null;
        AmmoIdToBuy = 0;
        foreach (int ammoId in GetListUsableAmmo())
        {
            DatabaseNPC vendorWithThisAmmo = Database.GetAmmoVendor(new HashSet<int>() { ammoId });
            if (vendorWithThisAmmo != null && Helpers.HaveEnoughMoneyFor(AmmoAmountToBuy, Database.GetItemName(ammoId)))
            {
                AmmoIdToBuy = ammoId;
                AmmoVendor = vendorWithThisAmmo;
                AmmoNameToBuy = Database.GetItemName(ammoId);
                return;
            }
        }

        if (AmmoVendor == null)
            Main.Logger($"Couldn't find any ammo vendor");
    }

    private HashSet<int> GetListUsableAmmo()
    {
        HashSet<int> listAmmo = new HashSet<int>();
        if (Helpers.GetRangedWeaponType() == "Bows")
        {
            foreach (KeyValuePair<int, int> arrow in ArrowDictionary)
            {
                if (arrow.Key <= Me.Level)
                    listAmmo.Add(arrow.Value);
            }
        }
        else if (Helpers.GetRangedWeaponType() == "Guns")
        {
            foreach (KeyValuePair<int, int> bullet in BulletsDictionary)
            {
                if (bullet.Key <= Me.Level)
                    listAmmo.Add(bullet.Value);
            }
        }
        return listAmmo;
    }

    private int GetNbAmmosInBags()
    {
        int nbAmmosInBags = 0;
        foreach (int arrow in GetListUsableAmmo())
            nbAmmosInBags += ItemsManager.GetItemCountById((uint)arrow);

        return nbAmmosInBags;
    }
}
