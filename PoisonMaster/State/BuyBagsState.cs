using PoisonMaster;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Threading;
using Wholesome_Vendors.Database;
using Wholesome_Vendors.Database.Models;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class BuyBagsState : State
{
    public override string DisplayName { get; set; } = "WV Buying Bags";

    private WoWLocalPlayer Me = ObjectManager.Me;
    private Timer stateTimer = new Timer();
    private ModelNpcVendor BagVendor;
    private ModelItemTemplate BagToBuy;

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !PluginSettings.CurrentSetting.BuyBags
                || !MemoryDB.IsPopulated
                || !PluginCache.Initialized
                || PluginCache.EmptyContainerSlots <= 0
                || !stateTimer.IsReady
                || Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);

            if (BagInBags() != null)
            {
                Main.Logger($"Equipping {BagInBags().Name}");
                Lua.RunMacroText($"/equip {BagInBags().Name}");
                Lua.LuaDoString($"EquipPendingItem(0);");
                Thread.Sleep(500);
                return false;
            }

            BagVendor = null;
            BagToBuy = null;

            foreach (ModelItemTemplate bag in MemoryDB.GetBags)
            {
                if (Helpers.HaveEnoughMoneyFor(1, bag))
                {
                    ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(bag);
                    if (vendor != null)
                    {
                        BagToBuy = bag;
                        BagVendor = vendor;
                        DisplayName = $"Buying {BagToBuy.Name} at vendor {BagVendor.CreatureTemplate.name}";
                        return true;
                    }
                }
            }

            return false;
        }
    }
    public override void Run()
    {
        Main.Logger(DisplayName);
        Vector3 vendorPos = BagVendor.CreatureTemplate.Creature.GetSpawnPosition;

        Helpers.CheckMailboxNearby(BagVendor.CreatureTemplate);

        if (Me.Position.DistanceTo(vendorPos) >= 10)
            GoToTask.ToPosition(vendorPos);

        if (Me.Position.DistanceTo(vendorPos) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(BagVendor.CreatureTemplate))
                return;

            Helpers.AddItemToDoNotSellList(BagToBuy.Name);
            Helpers.AddItemToDoNotMailList(BagToBuy.Name);

            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(vendorPos, BagVendor.entry, i);
                Thread.Sleep(500);
                Lua.LuaDoString($"StaticPopup1Button2:Click()"); // discard hearthstone popup
                if (Helpers.IsVendorGossipOpen())
                {
                    Helpers.BuyItem(BagToBuy.Name, 1, BagToBuy.BuyCount);
                    Helpers.CloseWindow();
                    Thread.Sleep(500);
                    if (ItemsManager.GetItemCountByNameLUA(BagToBuy.Name) > 0)
                    {
                        Main.Logger($"Equipping {BagInBags().Name}");
                        Lua.RunMacroText($"/equip {BagInBags().Name}");
                        Lua.LuaDoString($"EquipPendingItem(0);");
                        Thread.Sleep(500);
                        return;
                    }
                }
            }

            Main.Logger($"Failed to buy {BagToBuy.Name}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(BagVendor.entry);
        }
    }

    private ModelItemTemplate BagInBags()
    {
        List<WoWItem> items = PluginCache.BagItems;
        List<ModelItemTemplate> allBags = MemoryDB.GetBags;
        foreach (WoWItem item in items)
        {
            if (allBags.Exists(ua => ua.Entry == item.Entry))
            {
                return allBags.Find(ua => ua.Entry == item.Entry);
            }
        }
        return null;
    }
}
