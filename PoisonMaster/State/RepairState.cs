using PoisonMaster;
using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class RepairState : State
{
    public override string DisplayName => "WV Repair and Sell";

    private DatabaseNPC repairVendor;
    private Timer stateTimer = new Timer();
    private int minDurability = 35;
    private GameObjects BestMailbox;
    private int minFreeSlots => wManagerSetting.CurrentSetting.MinFreeBagSlotsToGoToTown;

    public override bool NeedToRun
    {
        get
        {
            if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                || !Main.IsLaunched
                || !stateTimer.IsReady
                || ObjectManager.Me.IsOnTaxi)
                return false;

            stateTimer = new Timer(5000);

            if (PluginSettings.CurrentSetting.AutoRepair && ObjectManager.Me.GetDurabilityPercent < minDurability)
            {
                repairVendor = Database.GetRepairVendor();
                SetMailbox(repairVendor);
                return repairVendor != null;
            }
            else if (PluginSettings.CurrentSetting.AllowAutoSell && Bag.GetContainerNumFreeSlotsByType(BagType.Unspecified) <= minFreeSlots)
            {
                repairVendor = Database.GetSellVendor();
                SetMailbox(repairVendor);
                return repairVendor != null;
            }

            return false;
        }
    }

    public override void Run()
    {
        Main.Logger($"Going to sell/repair vendor {repairVendor.Name}");
        
        //Mailing Start
        if (wManagerSetting.CurrentSetting.UseMail && BestMailbox != null)
        {
            Main.Logger($"Important, before Buying we need to Mail Items");
            if (ObjectManager.Me.Position.DistanceTo(BestMailbox.Position) >= 10)
                GoToTask.ToPositionAndIntecractWithGameObject(BestMailbox.Position, BestMailbox.Id);
            if (ObjectManager.Me.Position.DistanceTo(BestMailbox.Position) < 10)
                if (Helpers.MailboxIsAbsent(BestMailbox))
                    return;

            bool needRunAgain = true;
            for (int i = 7; i > 0 && needRunAgain; i--)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(BestMailbox.Position, BestMailbox.Id);
                Thread.Sleep(500);
                Mail.SendMessage(wManagerSetting.CurrentSetting.MailRecipient,
                    "Post",
                    "Message",
                    wManagerSetting.CurrentSetting.ForceMailList,
                    wManagerSetting.CurrentSetting.DoNotMailList,
                    Helpers.GetListQualityToMail(),
                    out needRunAgain);
            }
            if (!needRunAgain)
                Main.Logger($"Send Items to the Player {wManagerSetting.CurrentSetting.MailRecipient}");

            Mail.CloseMailFrame();
        }
        //Mailing End

        List<WoWItem> bagItems = Bag.GetBagItem();

        if (ObjectManager.Me.Position.DistanceTo(repairVendor.Position) >= 10)
            GoToTask.ToPosition(repairVendor.Position);

        if (ObjectManager.Me.Position.DistanceTo(repairVendor.Position) < 10)
        {
            if (Helpers.NpcIsAbsentOrDead(repairVendor))
                return;

            // Sell first
            Helpers.SellItems(repairVendor);

            // Then repair
            for (int i = 0; i <= 5; i++)
            {
                GoToTask.ToPositionAndIntecractWithNpc(repairVendor.Position, repairVendor.Id, i);
                Vendor.RepairAllItems();
                Lua.LuaDoString("MerchantRepairAllButton:Click();", false);
                Lua.LuaDoString("RepairAllItems();", false);
                Helpers.CloseWindow();
                Thread.Sleep(1000);

                if (ObjectManager.Me.GetDurabilityPercent >= minDurability)
                    break;
            }

            if (ObjectManager.Me.GetDurabilityPercent < minDurability || Bag.GetContainerNumFreeSlotsByType(BagType.Unspecified) <= minFreeSlots)
            {
                Main.Logger($"Failed to sell/repair, blacklisting vendor");
                NPCBlackList.AddNPCToBlacklist(repairVendor.Id);
            }
        }
    }
    private void SetMailbox(DatabaseNPC NearTo)
    {
        GameObjects nearestMailbox = Database.GetMailbox(NearTo);
        BestMailbox = nearestMailbox;
    }

}
