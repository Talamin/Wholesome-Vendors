using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System;
using System.Configuration;
using System.ComponentModel;
using System.IO;
using MarsSettingsGUI;
using System.Collections.Generic;
using wManager.Wow.Enums;

[Serializable]
public class PluginSettings : Settings
{
    [Setting]
    [DefaultValue(0)]
    [Category("Buy")]
    [DisplayName("Food")]
    [Description("Food amount to buy")]
    public int FoodNbToBuy { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("Buy")]
    [DisplayName("Drink")]
    [Description("Drink amount to buy")]
    public int DrinkNbToBuy { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Buy")]
    [DisplayName("Poison")]
    [Description("Allow buying poison")]
    public bool BuyPoison { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("Buy")]
    [DisplayName("Ammo")]
    [Description("Ammunition amount to Buy")]
    public int AmmoAmount { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Repair")]
    [DisplayName("Repair")]
    [Description("Allow repair")]
    public bool AllowRepair { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Sell")]
    [DisplayName("Sell")]
    [Description("Allow selling")]
    public bool AllowSell { get; set; }

    [Setting]
    [DefaultValue(2)]
    [Category("Sell")]
    [DisplayName("Min Free Bags Slots")]
    [Description("Minimum Free Bags Slots")]
    public int MinFreeSlots { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Sell")]
    [DisplayName("Sell Gray")]
    [Description("Allow selling of gray items")]
    public bool SellGrayItems { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Sell")]
    [DisplayName("Sell White")]
    [Description("Allow selling of white items")]
    public bool SellWhiteItems { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Sell")]
    [DisplayName("Sell Green")]
    [Description("Allow selling of green items")]
    public bool SellGreenItems { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Sell")]
    [DisplayName("Sell Blue")]
    [Description("Allow selling of blue items")]
    public bool SellBlueItems { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Sell")]
    [DisplayName("Sell Purple")]
    [Description("Allow selling of purple items")]
    public bool SellPurpleItems { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Training")]
    [DisplayName("Train")]
    [Description("Allow training")]
    public bool AllowTrain { get; set; }

    [Setting]
    [Category("Training")]
    [DisplayName("Training levels")]
    [Description("Set at which levels you want to train. Leave empty if you want to train every 2 levels.")]
    public List<int> TrainLevels { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail")]
    [Description("Allow mailing")]
    public bool AllowMail { get; set; }

    [Setting]
    [DefaultValue("")]
    [Category("Mailing")]
    [DisplayName("Mail Recipient")]
    [Description("Recipient for mailing")]
    public string MailingRecipient { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail Gray")]
    [Description("Allow mailing of gray items")]
    public bool MailGrayItems { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail White")]
    [Description("Allow mailing of white items")]
    public bool MailWhiteItems { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail Green")]
    [Description("Allow mailing of green items")]
    public bool MailGreenItems { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail Blue")]
    [Description("Allow mailing of blue items")]
    public bool MailBlueItems { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail Purple")]
    [Description("Allow mailing of purple items")]
    public bool MailPurpleItems { get; set; }

    public string Databasetype { get; set; }
    public double LastUpdateDate { get; set; }
    public int LastLevelTrained { get; set; }
    public List<VendorItem> VendorItems { get; set; }

    public PluginSettings()
    {
        Databasetype = "external";
        FoodNbToBuy = 20;
        DrinkNbToBuy = ObjectManager.Me.WowClass == WoWClass.Paladin
            || ObjectManager.Me.WowClass == WoWClass.Hunter
            || ObjectManager.Me.WowClass == WoWClass.Priest
            || ObjectManager.Me.WowClass == WoWClass.Shaman
            || ObjectManager.Me.WowClass == WoWClass.Mage
            || ObjectManager.Me.WowClass == WoWClass.Warlock
            || ObjectManager.Me.WowClass == WoWClass.Druid ? 20 : 0;
        BuyPoison = ObjectManager.Me.WowClass == WoWClass.Rogue;
        AmmoAmount = ObjectManager.Me.WowClass == WoWClass.Hunter ? 2000 : 0;
        AllowRepair = true;
        AllowSell = true;
        AllowTrain = true;
        LastUpdateDate = 0;
        LastLevelTrained = (int)ObjectManager.Me.Level;

        MinFreeSlots = 2;
        SellGrayItems = true;
        SellWhiteItems = false;
        SellGreenItems = false;
        SellBlueItems = false;
        SellPurpleItems = false;

        AllowMail = false;
        MailGrayItems = false;
        MailWhiteItems = false;
        MailGreenItems = false;
        MailBlueItems = false;
        MailPurpleItems = false;

        MailingRecipient = "";

        TrainLevels = new List<int> {};

        VendorItems = new List<VendorItem>();
    }

    [Serializable]
    public struct VendorItem
    {
        public string Name;
        public int Stack;
        public int Price;

        public VendorItem(string name, int stack, int price)
        {
            Price = price;
            Name = name;
            Stack = stack;
        }
    }

    public static PluginSettings CurrentSetting { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("Wholesome-BuyingPlugin", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Main.Logger("Wholesome-BuyingPlugin > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("Wholesome-BuyingPlugin", ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting =
                    Load<PluginSettings>(AdviserFilePathAndName("Wholesome-BuyingPlugin", ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new PluginSettings();
        }
        catch (Exception e)
        {
            Main.Logger("Wholesome-BuyingPlugin > Load(): " + e);
        }
        return false;
    }

    public void ShowConfiguration()
    {
        var settingWindow = new SettingsWindow(this);
        settingWindow.ShowDialog();
        settingWindow.SaveWindowPosition = true;
        settingWindow.Title = $"{ObjectManager.Me.Name}";
    }
}