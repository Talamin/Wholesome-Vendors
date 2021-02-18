using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;
using PoisonMaster;

public class BuyAmmoState : State
{
    public override string DisplayName => "Buying Ammunition";

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

            if (ammoVendor == null)
            {
                Main.Logger("Couldn't find ammo vendor");
                return false;
            }

            if (AmmoToBuy != 0 && ItemsManager.GetItemCountById((uint)AmmoToBuy) <= 50)
            {
                Main.Logger("We have to Buy ammo");
                return true;
            }

            return false;
        }
    }

    public override void Run()
    {
        if (Me.Position.DistanceTo(ammoVendor.Position) >= 6)
        {
            Main.Logger("Running to Buy Arrows");
            Main.Logger("Nearest AmmunitionVendor from player:\n" + "Name: " + ammoVendor.Name + "[" + ammoVendor.Id + "]\nPosition: " + ammoVendor.Position.ToStringXml() + "\nDistance: " + ammoVendor.Position.DistanceTo(ObjectManager.Me.Position) + " yrds");
            GoToTask.ToPositionAndIntecractWithNpc(ammoVendor.Position, ammoVendor.Id);
        }
        else
        {
            if (Helpers.NpcIsAbsentOrDead(ammoVendor))
                return;
          
            Main.Logger("No Arrows found, time to buy some! " + AmmoToBuy);
            GoToTask.ToPositionAndIntecractWithNpc(ammoVendor.Position, ammoVendor.Id);
            Vendor.BuyItem(ItemsManager.GetNameById(AmmoToBuy), 2000 / 200);
            Helpers.AddItemToDoNotSellList(ItemsManager.GetNameById(AmmoToBuy));
            Main.Logger("We bought " + 2000 + " of  Arrows with id " + AmmoToBuy);
        }
        ammoVendor = null;
    }

    private DatabaseNPC SelectBestAmmoAndVendor()
    {
        AmmoToBuy = 0;
        foreach (int ammo in GetListUsableAmmo())
        {
            DatabaseNPC vendorWithThisAmmo = Database.GetAmmoVendor(new HashSet<int>() { ammo });
            if (vendorWithThisAmmo != null)
            {
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
