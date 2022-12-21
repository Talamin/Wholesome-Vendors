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
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

namespace WholesomeVendors.WVState
{
    public class BuyAmmoState : State
    {
        public override string DisplayName { get; set; } = "WV Buying Ammunition";

        private WoWLocalPlayer _me = ObjectManager.Me;
        private Timer _stateTimer = new Timer();
        private ModelNpcVendor _ammoVendor;
        private ModelItemTemplate _ammoToBuy;
        private int _nbAmmoInBags;
        private bool _usingDungeonProduct;

        private int AmmoAmountSetting => PluginSettings.CurrentSetting.AmmoAmount;
        private int AmountToBuy => AmmoAmountSetting - GetNbAmmosInBags();

        public BuyAmmoState()
        {
            _usingDungeonProduct = Helpers.UsingDungeonProduct();
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !Main.IsLaunched
                    || !MemoryDB.IsPopulated
                    || !PluginCache.Initialized
                    || Fight.InFight
                    || PluginCache.RangedWeaponType == null
                    || PluginSettings.CurrentSetting.AmmoAmount <= 0
                    || !_stateTimer.IsReady
                    || _me.IsOnTaxi)
                    return false;

                _ammoVendor = null;
                _ammoToBuy = null;

                _nbAmmoInBags = GetNbAmmosInBags();

                if (PluginCache.IsInInstance)
                {
                    return false;
                }

                _stateTimer = new Timer(5000);

                if (_nbAmmoInBags <= AmmoAmountSetting / 2 || _usingDungeonProduct)
                {
                    int amountToBuy = AmountToBuy;
                    foreach (ModelItemTemplate ammo in MemoryDB.GetUsableAmmos())
                    {
                        if (Helpers.HaveEnoughMoneyFor(amountToBuy, ammo))
                        {
                            ModelNpcVendor vendor = MemoryDB.GetNearestItemVendor(ammo);
                            if (vendor != null)
                            {
                                _ammoToBuy = ammo;
                                _ammoVendor = vendor;
                                // Normal
                                if (_nbAmmoInBags <= AmmoAmountSetting / 10
                                    || _usingDungeonProduct && _nbAmmoInBags < AmmoAmountSetting)
                                {
                                    DisplayName = $"Buying {amountToBuy} x {_ammoToBuy.Name} at vendor {_ammoVendor.CreatureTemplate.name}";
                                    return true;
                                }
                                // Drive-by
                                if (_nbAmmoInBags <= AmmoAmountSetting / 2
                                    && ObjectManager.Me.Position.DistanceTo(vendor.CreatureTemplate.Creature.GetSpawnPosition) < PluginSettings.CurrentSetting.DriveByDistance)
                                {
                                    DisplayName = $"Drive-by buying {amountToBuy} x {_ammoToBuy.Name} at vendor {_ammoVendor.CreatureTemplate.name}";
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

            Helpers.CheckMailboxNearby(_ammoVendor.CreatureTemplate);
            Vector3 vendorPos = _ammoVendor.CreatureTemplate.Creature.GetSpawnPosition;

            if (_me.Position.DistanceTo(vendorPos) >= 10)
                GoToTask.ToPositionAndIntecractWithNpc(vendorPos, _ammoVendor.entry);

            if (_me.Position.DistanceTo(vendorPos) < 10)
            {
                if (Helpers.NpcIsAbsentOrDead(_ammoVendor.CreatureTemplate))
                    return;

                Helpers.RemoveItemFromDoNotSellAndMailList(MemoryDB.GetUsableAmmos());
                Helpers.AddItemToDoNotSellAndMailList(_ammoToBuy.Name);

                for (int i = 0; i <= 5; i++)
                {
                    Main.Logger($"Attempt {i + 1}");
                    GoToTask.ToPositionAndIntecractWithNpc(vendorPos, _ammoVendor.entry, i);
                    Thread.Sleep(1000);
                    WTGossip.ClickOnFrameButton("StaticPopup1Button2"); // discard hearthstone popup
                    if (WTGossip.IsVendorGossipOpen)
                    {
                        Helpers.SellItems();
                        Thread.Sleep(1000);
                        WTGossip.BuyItem(_ammoToBuy.Name, AmountToBuy, _ammoToBuy.BuyCount);
                        Thread.Sleep(1000);

                        if (GetNbAmmosInBags() >= AmmoAmountSetting)
                        {
                            Helpers.CloseWindow();
                            return;
                        }
                    }
                    Helpers.CloseWindow();
                }

                Main.Logger($"Failed to buy {_ammoToBuy.Name}, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(_ammoVendor.CreatureTemplate.entry);
            }
        }

        private int GetNbAmmosInBags()
        {
            int nbAmmosInBags = 0;
            List<WoWItem> bagItems = PluginCache.BagItems;
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
}
