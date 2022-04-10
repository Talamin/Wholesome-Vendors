using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Threading;
using WholesomeToolbox;
using WholesomeVendors;
using WholesomeVendors.Blacklist;
using WholesomeVendors.Database;
using WholesomeVendors.Database.Models;
using WholesomeVendors.WVSettings;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class BuyBagsState : State
{
    public override string DisplayName { get; set; } = "WV Buying Bags";

    private WoWLocalPlayer _me = ObjectManager.Me;
    private Timer _stateTimer = new Timer();
    private ModelNpcVendor _bagVendor;
    private ModelItemTemplate _bagToBuy;

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !PluginSettings.CurrentSetting.BuyBags
                || !MemoryDB.IsPopulated
                || !PluginCache.Initialized
                || PluginCache.IsInInstance
                || !_stateTimer.IsReady
                || _me.IsOnTaxi)
                return false;

            _stateTimer = new Timer(5000);

            if (PluginCache.EmptyContainerSlots <= 0)
            {
                Helpers.RemoveItemFromDoNotSellAndMailList(MemoryDB.GetBags);
                return false;
            }

            if (BagInBags() != null)
            {
                Main.Logger($"Equipping {BagInBags().Name}");
                WTItem.EquipBag(BagInBags().Name);
                Thread.Sleep(500);
                return false;
            }

            _bagVendor = null;
            _bagToBuy = null;

            foreach (ModelItemTemplate bag in MemoryDB.GetBags)
            {
                if (Helpers.HaveEnoughMoneyFor(1, bag))
                {
                    ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(bag);
                    if (vendor != null)
                    {
                        _bagToBuy = bag;
                        _bagVendor = vendor;
                        DisplayName = $"Buying {_bagToBuy.Name} at vendor {_bagVendor.CreatureTemplate.name}";
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
        Vector3 vendorPos = _bagVendor.CreatureTemplate.Creature.GetSpawnPosition;

        Helpers.CheckMailboxNearby(_bagVendor.CreatureTemplate);

        if (_me.Position.DistanceTo(vendorPos) >= 10)
            GoToTask.ToPosition(vendorPos);

        if (_me.Position.DistanceTo(vendorPos) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(_bagVendor.CreatureTemplate))
                return;

            Helpers.AddItemToDoNotSellAndMailList(_bagToBuy.Name);

            for (int i = 0; i <= 5; i++)
            {
                Main.Logger($"Attempt {i + 1}");
                GoToTask.ToPositionAndIntecractWithNpc(vendorPos, _bagVendor.entry, i);
                Thread.Sleep(1000);
                WTLua.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                if (WTGossip.IsVendorGossipOpen)
                {
                    WTGossip.BuyItem(_bagToBuy.Name, 1, _bagToBuy.BuyCount);
                    Thread.Sleep(1000);
                    if (ItemsManager.GetItemCountByNameLUA(_bagToBuy.Name) > 0)
                    {
                        Main.Logger($"Equipping {BagInBags().Name}");
                        WTItem.EquipBag(BagInBags().Name);
                        Thread.Sleep(1000);
                        Helpers.CloseWindow();
                        return;
                    }
                }
                Helpers.CloseWindow();
            }

            Main.Logger($"Failed to buy {_bagToBuy.Name}, blacklisting vendor");
            NPCBlackList.AddNPCToBlacklist(_bagVendor.entry);
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
