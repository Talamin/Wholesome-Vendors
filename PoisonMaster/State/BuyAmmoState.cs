using PoisonMaster;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Wholesome_Vendors.Database;
using Wholesome_Vendors.Database.Models;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class BuyAmmoState : State
{
    public override string DisplayName { get; set; } = "WV Buying Ammunition";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private Timer stateTimer = new Timer();
    private ModelNpcVendor AmmoVendor;
    private ModelItemTemplate AmmoToBuy;
    private int AmmoAmountSetting => PluginSettings.CurrentSetting.AmmoAmount;
    private int NbAmmoInBags;
    private int AmountToBuy => AmmoAmountSetting - GetNbAmmosInBags();

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !MemoryDB.IsPopulated
                || PluginSettings.CurrentSetting.AmmoAmount <= 0
                || !stateTimer.IsReady
                || Helpers.GetRangedWeaponType() == null
                || Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);
            AmmoVendor = null;
            AmmoToBuy = null;

            NbAmmoInBags = GetNbAmmosInBags();

            if (NbAmmoInBags <= AmmoAmountSetting / 2)
            {
                int myMoney = (int)ObjectManager.Me.GetMoneyCopper;
                int amountToBuy = AmountToBuy;
                foreach (ModelItemTemplate ammo in MemoryDB.GetUsableAmmos())
                {
                    if (Helpers.HaveEnoughMoneyFor(amountToBuy, ammo, myMoney))
                    {
                        ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(ammo);
                        if (vendor != null)
                        {
                            AmmoToBuy = ammo;
                            AmmoVendor = vendor;
                            // Normal
                            if (NbAmmoInBags <= AmmoAmountSetting / 10)
                            {
                                DisplayName = $"Buying {amountToBuy} x {AmmoToBuy.Name} at vendor {AmmoVendor.CreatureTemplate.name}";
                                return true;
                            }
                            // Drive-by
                            if (NbAmmoInBags <= AmmoAmountSetting / 2
                                && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                            {
                                DisplayName = $"Drive-by buying {amountToBuy} x {AmmoToBuy.Name} at vendor {AmmoVendor.CreatureTemplate.name}";
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }

    public override void Run()
    {
        Main.Logger(DisplayName);

        Helpers.CheckMailboxNearby(AmmoVendor.CreatureTemplate);
        Vector3 vendorPos = AmmoVendor.CreatureTemplate.Creature.GetSpawnPosition;

        if (Me.Position.DistanceTo(vendorPos) >= 10)
            GoToTask.ToPositionAndIntecractWithNpc(vendorPos, AmmoVendor.entry);

        if (Me.Position.DistanceTo(vendorPos) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(AmmoVendor.CreatureTemplate))
                return;

            ClearObsoleteAmmo();
            Helpers.AddItemToDoNotSellList(AmmoToBuy.Name);
            Helpers.AddItemToDoNotMailList(AmmoToBuy.Name);

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(vendorPos, AmmoVendor.entry, i);
                Thread.Sleep(500);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.IsVendorGossipOpen())
                {
                    Helpers.SellItems(AmmoVendor.CreatureTemplate);
                    Helpers.BuyItem(AmmoToBuy.Name, AmountToBuy, AmmoToBuy.BuyCount);
                    Helpers.CloseWindow();
                    Thread.Sleep(1000);
                    if (GetNbAmmosInBags() >= AmmoAmountSetting)
                        return;
                }
            }
            Main.Logger($"Failed to buy {AmmoToBuy.Name}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(AmmoVendor.CreatureTemplate.entry);
        }
    }

    private void ClearObsoleteAmmo()
    {
        foreach (ModelItemTemplate ammo in MemoryDB.GetUsableAmmos())
        {
            Helpers.RemoveItemFromDoNotSellList(ammo.Name);
        }
    }

    private int GetNbAmmosInBags()
    {
        int nbAmmosInBags = 0;
        List<WoWItem> bagItems = Bag.GetBagItem();
        List<ModelItemTemplate> allAmmos = MemoryDB.GetUsableAmmos();
        foreach (WoWItem bagItem in bagItems)
        {
            if (allAmmos.Exists(ua => ua.Entry == bagItem.Entry))
            {
                nbAmmosInBags += ItemsManager.GetItemCountById((uint)bagItem.Entry);
            }
        }
        return nbAmmosInBags;
    }
}
