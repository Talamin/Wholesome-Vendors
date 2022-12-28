using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Threading;
using WholesomeToolbox;
using WholesomeVendors.Blacklist;
using WholesomeVendors.Database;
using WholesomeVendors.Database.Models;
using WholesomeVendors.WVSettings;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

namespace WholesomeVendors.WVState
{
    public class BuyPoisonState : State
    {
        public override string DisplayName { get; set; } = "WV Buying Poison";

        private WoWLocalPlayer _me = ObjectManager.Me;

        private ModelItemTemplate _poisonToBuy;
        private ModelNpcVendor _poisonVendor;
        private int _nbInstantsInBags;
        private int _nbDeadlysInBags;
        private int _amountToBuy;
        private Timer _stateTimer = new Timer();
        private bool _usingDungeonProduct;

        public BuyPoisonState()
        {
            _usingDungeonProduct = Helpers.UsingDungeonProduct();
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !Main.IsLaunched
                    || PluginCache.InLoadingScreen
                    || !_stateTimer.IsReady
                    || !MemoryDB.IsPopulated
                    || !PluginCache.Initialized
                    || Fight.InFight
                    || !PluginSettings.CurrentSetting.BuyPoison
                    || ObjectManager.Me.WowClass != WoWClass.Rogue
                    || ObjectManager.Me.Level < 20
                    || _me.IsOnTaxi)
                    return false;

                _poisonToBuy = null;
                _poisonVendor = null;
                _amountToBuy = 0;

                RecordNbPoisonsInBags();

                if (PluginCache.IsInInstance)
                {
                    return false;
                }

                _stateTimer = new Timer(5000);

                // Deadly Poison
                if (_nbDeadlysInBags <= 15)
                {
                    _amountToBuy = 20 - _nbDeadlysInBags;
                    ModelItemTemplate deadlyP = MemoryDB.GetDeadlyPoisons.Find(p => p.RequiredLevel <= ObjectManager.Me.Level);
                    if (deadlyP != null && Helpers.HaveEnoughMoneyFor(_amountToBuy, deadlyP))
                    {
                        ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(deadlyP);
                        if (vendor != null)
                        {
                            _poisonToBuy = deadlyP;
                            _poisonVendor = vendor;
                            // Normal
                            if (_nbDeadlysInBags <= 1
                                || _usingDungeonProduct && _nbDeadlysInBags <= 15)
                            {
                                DisplayName = $"Buying {_amountToBuy} x {_poisonToBuy.Name} at vendor {_poisonVendor.CreatureTemplate.name}";
                                return true;
                            }
                            // Drive-by
                            if (_nbDeadlysInBags <= 15
                                && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                            {
                                DisplayName = $"Drive-by buying {_amountToBuy} x {_poisonToBuy.Name} at vendor {_poisonVendor.CreatureTemplate.name}";
                                return true;
                            }
                        }
                    }
                }

                // Instant Poison
                if (_nbInstantsInBags <= 10)
                {
                    _amountToBuy = 20 - _nbInstantsInBags;
                    ModelItemTemplate instantP = MemoryDB.GetInstantPoisons.Find(p => p.RequiredLevel <= ObjectManager.Me.Level);
                    if (instantP != null && Helpers.HaveEnoughMoneyFor(_amountToBuy, instantP))
                    {
                        ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(instantP);
                        if (vendor != null)
                        {
                            // Normal
                            if (_nbInstantsInBags <= 1
                                || _usingDungeonProduct && _nbInstantsInBags <= 15)
                            {
                                _poisonToBuy = instantP;
                                _poisonVendor = vendor;
                                DisplayName = $"Buying {_amountToBuy} x {_poisonToBuy.Name} at vendor {_poisonVendor.CreatureTemplate.name}";
                                return true;
                            }
                            // Drive-by
                            if (_nbInstantsInBags <= 15
                                && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                            {
                                _poisonToBuy = instantP;
                                _poisonVendor = vendor;
                                DisplayName = $"Drive-by buying {_amountToBuy} x {_poisonToBuy.Name} at vendor {_poisonVendor.CreatureTemplate.name}";
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
            Vector3 vendorPos = _poisonVendor.CreatureTemplate.Creature.GetSpawnPosition;

            Helpers.CheckMailboxNearby(_poisonVendor.CreatureTemplate);

            if (_me.Position.DistanceTo(vendorPos) >= 10)
                GoToTask.ToPosition(vendorPos);

            if (_me.Position.DistanceTo(vendorPos) < 10)
            {
                if (Helpers.NpcIsAbsentOrDead(_poisonVendor.CreatureTemplate))
                    return;

                ClearObsoletePoison(_poisonToBuy.displayid);
                WTSettings.AddItemToDoNotSellAndMailList(new List<string>() { _poisonToBuy.Name });

                for (int i = 0; i <= 5; i++)
                {
                    Main.Logger($"Attempt {i + 1}");
                    GoToTask.ToPositionAndIntecractWithNpc(vendorPos, _poisonVendor.entry, i);
                    Thread.Sleep(1000);
                    WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                    if (WTGossip.IsVendorGossipOpen)
                    {
                        Helpers.SellItems();
                        Thread.Sleep(1000);
                        WTGossip.BuyItem(_poisonToBuy.Name, _amountToBuy, _poisonToBuy.BuyCount);
                        Thread.Sleep(1000);
                        RecordNbPoisonsInBags();
                        Thread.Sleep(1000);

                        if (_poisonToBuy.displayid == 13710 && _nbInstantsInBags >= 20) // Instant
                        {
                            Helpers.CloseWindow();
                            return;
                        }
                        if (_poisonToBuy.displayid == 13707 && _nbDeadlysInBags >= 20) // Deadly
                        {
                            Helpers.CloseWindow();
                            return;
                        }
                    }
                    Helpers.CloseWindow();
                }

                Main.Logger($"Failed to buy poisons, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(_poisonVendor.entry);
            }
        }

        private void ClearObsoletePoison(int displayId)
        {
            foreach (ModelItemTemplate poison in MemoryDB.GetAllPoisons)
            {
                if (poison.displayid == displayId)
                {
                    WTSettings.RemoveItemFromDoNotSellAndMailList(new List<string>() { poison.Name });
                }
            }
        }

        private void RecordNbPoisonsInBags()
        {
            _nbDeadlysInBags = 0;
            _nbInstantsInBags = 0;
            List<WoWItem> bagItems = PluginCache.BagItems;
            foreach (WoWItem item in bagItems)
            {
                if (MemoryDB.GetDeadlyPoisons.Exists(p => p.Entry == item.Entry))
                {
                    _nbDeadlysInBags += ItemsManager.GetItemCountById((uint)item.Entry);
                }
                if (MemoryDB.GetInstantPoisons.Exists(p => p.Entry == item.Entry))
                {
                    _nbInstantsInBags += ItemsManager.GetItemCountById((uint)item.Entry);
                }
            }
        }
    }
}