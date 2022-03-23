using PoisonMaster;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Threading;
using Wholesome_Vendors.Database;
using Wholesome_Vendors.Database.Models;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class BuyPoisonState : State
{
    public override string DisplayName { get; set; } = "WV Buying Poison";

    private WoWLocalPlayer Me = ObjectManager.Me;

    private ModelItemTemplate PoisonToBuy;
    private ModelNpcVendor PoisonVendor;
    private int NbInstantsInBags;
    private int NbDeadlysInBags;
    private int AmountToBuy;
    private Timer stateTimer = new Timer();

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !stateTimer.IsReady
                || !MemoryDB.IsPopulated
                || !PluginCache.Initialized
                || !PluginSettings.CurrentSetting.BuyPoison
                || ObjectManager.Me.WowClass != WoWClass.Rogue
                || ObjectManager.Me.Level < 20
                || Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);
            PoisonToBuy = null;
            PoisonVendor = null;
            AmountToBuy = 0;

            RecordNbPoisonsInBags();

            // Deadly Poison
            if (NbDeadlysInBags <= 15)
            {
                AmountToBuy = 20 - NbDeadlysInBags;
                ModelItemTemplate deadlyP = MemoryDB.GetDeadlyPoisons.Find(p => p.RequiredLevel <= ObjectManager.Me.Level);
                if (deadlyP != null && Helpers.HaveEnoughMoneyFor(AmountToBuy, deadlyP))
                {
                    ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(deadlyP);
                    if (vendor != null)
                    {
                        PoisonToBuy = deadlyP;
                        PoisonVendor = vendor;
                        // Normal
                        if (NbDeadlysInBags <= 1)
                        {
                            DisplayName = $"Buying {AmountToBuy} x {PoisonToBuy.Name} at vendor {PoisonVendor.CreatureTemplate.name}";
                            return true;
                        }
                        // Drive-by
                        if (NbDeadlysInBags <= 15
                            && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                        {
                            DisplayName = $"Drive-by buying {AmountToBuy} x {PoisonToBuy.Name} at vendor {PoisonVendor.CreatureTemplate.name}";
                            return true;
                        }
                    }
                }
            }

            // Instant Poison
            if (NbInstantsInBags <= 10)
            {
                AmountToBuy = 20 - NbInstantsInBags;
                ModelItemTemplate instantP = MemoryDB.GetInstantPoisons.Find(p => p.RequiredLevel <= ObjectManager.Me.Level);
                if (instantP != null && Helpers.HaveEnoughMoneyFor(AmountToBuy, instantP))
                {
                    ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(instantP);
                    if (vendor != null)
                    {
                        // Normal
                        if (NbInstantsInBags <= 1)
                        {
                            PoisonToBuy = instantP;
                            PoisonVendor = vendor;
                            DisplayName = $"Buying {AmountToBuy} x {PoisonToBuy.Name} at vendor {PoisonVendor.CreatureTemplate.name}";
                            return true;
                        }
                        // Drive-by
                        if (NbInstantsInBags <= 15
                            && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                        {
                            PoisonToBuy = instantP;
                            PoisonVendor = vendor;
                            DisplayName = $"Drive-by buying {AmountToBuy} x {PoisonToBuy.Name} at vendor {PoisonVendor.CreatureTemplate.name}";
                            return true;
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
        Vector3 vendorPos = PoisonVendor.CreatureTemplate.Creature.GetSpawnPosition;

        Helpers.CheckMailboxNearby(PoisonVendor.CreatureTemplate);

        if (Me.Position.DistanceTo(vendorPos) >= 10)
            GoToTask.ToPosition(vendorPos);

        if (Me.Position.DistanceTo(vendorPos) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(PoisonVendor.CreatureTemplate))
                return;

            ClearObsoletePoison(PoisonToBuy.displayid);
            Helpers.AddItemToDoNotSellList(PoisonToBuy.Name);
            Helpers.AddItemToDoNotMailList(PoisonToBuy.Name);

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(vendorPos, PoisonVendor.entry, i);
                Thread.Sleep(500);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.IsVendorGossipOpen())
                {
                    Helpers.SellItems(PoisonVendor.CreatureTemplate);

                    Helpers.BuyItem(PoisonToBuy.Name, AmountToBuy, PoisonToBuy.BuyCount);
                    Helpers.CloseWindow();
                    Thread.Sleep(500);
                    RecordNbPoisonsInBags();
                    Thread.Sleep(500);

                    if (PoisonToBuy.displayid == 13710 && NbInstantsInBags >= 20) // Instant
                        return;
                    if (PoisonToBuy.displayid == 13707 && NbDeadlysInBags >= 20) // Deadly
                        return;
                }
            }

            Main.Logger($"Failed to buy poisons, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(PoisonVendor.entry);
        }
    }

    private void ClearObsoletePoison(int displayId)
    {
        foreach (ModelItemTemplate poison in MemoryDB.GetAllPoisons)
        {
            if (poison.displayid == displayId)
                Helpers.RemoveItemFromDoNotSellList(poison.Name);
        }
    }

    private void RecordNbPoisonsInBags()
    {
        NbDeadlysInBags = 0;
        NbInstantsInBags = 0;
        List<WoWItem> bagItems = PluginCache.BagItems;
        foreach (WoWItem item in bagItems)
        {
            if (MemoryDB.GetDeadlyPoisons.Exists(p => p.Entry == item.Entry))
            {
                NbDeadlysInBags += ItemsManager.GetItemCountById((uint)item.Entry);
            }
            if (MemoryDB.GetInstantPoisons.Exists(p => p.Entry == item.Entry))
            {
                NbInstantsInBags += ItemsManager.GetItemCountById((uint)item.Entry);
            }
        }
    }
}