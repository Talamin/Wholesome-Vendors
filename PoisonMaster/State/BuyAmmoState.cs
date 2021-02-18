using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;
using PoisonMaster;

public class BuyAmmoState : State
{
    public override string DisplayName => "Buying Arrows and Bullets";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private uint ArrowId = 0;
    private uint BulletId = 0;
    private Timer stateTimer = new Timer();
    private DatabaseNPC ammoVendor;
    public static HashSet<int> BuyingAmmuniton = new HashSet<int>();

    private readonly Dictionary<int, uint> ArrowDictionary = new Dictionary<int, uint>
    {
        { 75, 41586 },
        { 65, 28056 },
        { 55, 28053 },
        { 40, 11285 },
        { 25, 3030 },
        { 10, 2515 },
        { 1, 2512 },
    };

    private readonly Dictionary<int, uint> BulletsDictionary = new Dictionary<int, uint>
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
            SetAmmoToBuy();

            if (ArrowId == 0 && BulletId == 0)
                return false;

            ammoVendor = Database.GetAmmoVendor();
            if (ammoVendor == null)
            {
                Main.Logger("Couldn't find ammo vendor");
                return false;
            }

            if (ArrowId != 0 && ItemsManager.GetItemCountById(ArrowId) <= 50)
            {
                Main.Logger("We have to Buy Arrows");
                return true;
            }

            if (BulletId != 0 && ItemsManager.GetItemCountById(BulletId) <= 50)
            {
                Main.Logger("We have to Buy Bullets");
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
          
            Main.Logger("No Arrows found, time to buy some! " + ArrowId);
            GoToTask.ToPositionAndIntecractWithNpc(ammoVendor.Position, ammoVendor.Id);
            Vendor.BuyItem(ItemsManager.GetNameById(ArrowId), 2000 / 200);
            Main.Logger("We bought " + 2000 + " of  Arrows with id " + ArrowId);
        }
        ammoVendor = null;
    }

    private void SetAmmoToBuy()
    {
        if (Helpers.GetRangedWeaponType() == "Bows")
        {
            foreach (KeyValuePair<int, uint> arrow in ArrowDictionary)
            {
                if (arrow.Key <= Me.Level)
                {
                    //Main.Logger($"Selected Arrow Level {level}");
                    Helpers.AddItemToDoNotSellList(ItemsManager.GetNameById(arrow.Value));
                    ArrowId = arrow.Value;
                    BulletId = 0;
                    BuyingAmmuniton.Clear();
                    if(!BuyingAmmuniton.Contains((int)ArrowId))
                    {
                        BuyingAmmuniton.Add((int)ArrowId);
                    }
                    break;
                }
            }
        }
        else if (Helpers.GetRangedWeaponType() == "Guns")
        {
            foreach (KeyValuePair<int, uint> bullet in BulletsDictionary)
            {
                if (bullet.Key <= Me.Level)
                {
                    //Main.Logger($"Selected Arrow Level {level}");
                    Helpers.AddItemToDoNotSellList(ItemsManager.GetNameById(bullet.Value));
                    BulletId = bullet.Value;
                    ArrowId = 0;
                    break;
                }
            }
        }
    }
}
