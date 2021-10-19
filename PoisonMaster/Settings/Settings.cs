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
    public int FoodAmount { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("Buy")]
    [DisplayName("Drink")]
    [Description("Drink amount to buy")]
    public int DrinkAmount { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Buy")]
    [DisplayName("Poison")]
    [Description("Allow buying poison")]
    public bool AllowAutobuyPoison { get; set; }

    [Setting]
    [DefaultValue(0)]
    [Category("Buy")]
    [DisplayName("Ammo")]
    [Description("Ammunition amount to Buy")]
    public int AutobuyAmmunitionAmount { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Repair")]
    [DisplayName("Repair")]
    [Description("Allow repair")]
    public bool AutoRepair { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Sell")]
    [DisplayName("Sell")]
    [Description("Allow selling")]
    public bool AllowAutoSell { get; set; }

    [Setting]
    [DefaultValue(2)]
    [Category("Sell")]
    [DisplayName("Min Free Bags Slots")]
    [Description("Minimum Free Bags Slots")]
    public int MinFreeBagSlots { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Sell")]
    [DisplayName("Sell Gray")]
    [Description("Allow selling of gray items")]
    public bool SellGray { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Sell")]
    [DisplayName("Sell White")]
    [Description("Allow selling of white items")]
    public bool SellWhite { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Sell")]
    [DisplayName("Sell Green")]
    [Description("Allow selling of green items")]
    public bool SellGreen { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Sell")]
    [DisplayName("Sell Blue")]
    [Description("Allow selling of blue items")]
    public bool SellBlue { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Sell")]
    [DisplayName("Sell Purple")]
    [Description("Allow selling of purple items")]
    public bool SellPurple { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Training")]
    [DisplayName("Train")]
    [Description("Allow training")]
    public bool AutoTrain { get; set; }

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
    public bool AllowMailing { get; set; }

    [Setting]
    [DefaultValue("")]
    [Category("Mailing")]
    [DisplayName("Mail Recipient")]
    [Description("Recipient for mailing")]
    public string MailRecipient { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail Gray")]
    [Description("Allow mailing of gray items")]
    public bool MailGray { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail White")]
    [Description("Allow mailing of white items")]
    public bool MailWhite { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail Green")]
    [Description("Allow mailing of green items")]
    public bool MailGreen { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail Blue")]
    [Description("Allow mailing of blue items")]
    public bool MailBlue { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Mailing")]
    [DisplayName("Mail Purple")]
    [Description("Allow mailing of purple items")]
    public bool MailPurple { get; set; }

    public string Databasetype { get; set; }
    public double LastUpdateDate { get; set; }
    public int LastLevelTrained { get; set; }
    public List<VendorItem> VendorItems { get; set; }

    public PluginSettings()
    {
        Databasetype = "external";
        FoodAmount = 0;
        DrinkAmount = 0;
        AllowAutobuyPoison = ObjectManager.Me.WowClass == WoWClass.Rogue;
        AutobuyAmmunitionAmount = 0;
        AutoRepair = true;
        AllowAutoSell = true;
        AutoTrain = true;
        LastUpdateDate = 0;
        LastLevelTrained = (int)ObjectManager.Me.Level;

        MinFreeBagSlots = 2;
        SellGray = true;
        SellWhite = false;
        SellGreen = false;
        SellBlue = false;
        SellPurple = false;

        AllowMailing = false;
        MailGray = false;
        MailWhite = false;
        MailGreen = false;
        MailBlue = false;
        MailPurple = false;

        MailRecipient = "";

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