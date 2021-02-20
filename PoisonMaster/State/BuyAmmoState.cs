using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;
using PoisonMaster;
using System.Threading;
using wManager;

public class BuyAmmoState : State
{
    public override string DisplayName => "WV Buying Ammunition";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private Timer stateTimer = new Timer();
    private DatabaseNPC ammoVendor;
    private int AmmoToBuy = 0;

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
            if (!PluginSettings.CurrentSetting.AutobuyAmmunition
                || !stateTimer.IsReady
                || Helpers.GetRangedWeaponType() == null)
                return false;

            stateTimer = new Timer(5000);

            ammoVendor = SelectBestAmmoAndVendor();

            if (AmmoToBuy == 0)
                return false;

            if (ammoVendor == null)
            {
                Main.Logger("Couldn't find ammo vendor");
                return false;
            }

            if (ItemsManager.GetItemCountById((uint)AmmoToBuy) <= 50)
                return true;

            return false;
        }
    }

    public override void Run()
    {
        if (Me.Position.DistanceTo(ammoVendor.Position) >= 10)
        {
            Main.Logger($"Buying {ItemsManager.GetNameById(AmmoToBuy)} from {ammoVendor.Name} [{ammoVendor.Id}]\nPosition: {ammoVendor.Position.ToStringXml()}\nDistance: {ammoVendor.Position.DistanceTo(ObjectManager.Me.Position)} yrds");
            GoToTask.ToPositionAndIntecractWithNpc(ammoVendor.Position, ammoVendor.Id);
        }

        if (Me.Position.DistanceTo(ammoVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(ammoVendor))
                return;

            // Sell first
            Helpers.SellItems(ammoVendor);

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(ammoVendor.Position, ammoVendor.Id, i);
                Vendor.BuyItem(ItemsManager.GetNameById(AmmoToBuy), 2000 / 200);
                ClearDoNotSellListFromAmmos();
                Helpers.AddItemToDoNotSellList(ItemsManager.GetNameById(AmmoToBuy));
                Helpers.CloseWindow();
                Thread.Sleep(1000);
                if (ItemsManager.GetItemCountById((uint)AmmoToBuy) >= wManagerSetting.CurrentSetting.DrinkAmount)
                    return;
            }
            Main.Logger($"Failed to buy {AmmoToBuy}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(ammoVendor.Id);
        }        
    }

    private void ClearDoNotSellListFromAmmos()
    {
        foreach (KeyValuePair<int, int> arrow in ArrowDictionary)
            Helpers.RemoveItemFromDoNotSellList(ItemsManager.GetNameById(arrow.Value));

        foreach (KeyValuePair<int, int> bullet in BulletsDictionary)
            Helpers.RemoveItemFromDoNotSellList(ItemsManager.GetNameById(bullet.Value));
    }

    private DatabaseNPC SelectBestAmmoAndVendor()
    {
        ammoVendor = null;
        AmmoToBuy = 0;
        foreach (int ammo in GetListUsableAmmo())
        {
            DatabaseNPC vendorWithThisAmmo = Database.GetAmmoVendor(new HashSet<int>() { ammo });
            if (vendorWithThisAmmo != null)
            {
                //Main.Logger($"Found vendor {vendorWithThisAmmo.Name} for item {ammo}");
                AmmoToBuy = ammo;
                return vendorWithThisAmmo;
            }
        }
        return null;
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
}
