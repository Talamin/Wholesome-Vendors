using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System;
using System.Configuration;
using System.ComponentModel;
using System.IO;
using MarsSettingsGUI;
using System.Collections.Generic;

[Serializable]
public class PluginSettings : Settings
{
    [Setting]
    [DefaultValue(true)]
    [Category("Buying")]
    [DisplayName("Buy Food")]
    [Description("Allow Autobuy Food")]
    public bool AutobuyFood { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Buying")]
    [DisplayName("Buy Water")]
    [Description("Allow Autobuy Water")]
    public bool AutoBuyWater { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Buying")]
    [DisplayName("Buy Poison")]
    [Description("Allow Autobuy Water")]
    public bool AllowAutobuyPoison { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Buying")]
    [DisplayName("Buy Ammunition")]
    [Description("Allow Autobuy Ammunition")]
    public bool AutobuyAmmunition { get; set; }

    [Setting]
    [DefaultValue(2000)]
    [Category("Buying")]
    [DisplayName("Buy Ammunition Ammount")]
    [Description("Set the Ammount to Buy")]
    public int AutobuyAmmunitionAmount { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("SellRepair")]
    [DisplayName("Sell")]
    [Description("Allow Autosell")]
    public bool AllowAutoSell { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("SellRepair")]
    [DisplayName("Repair")]
    [Description("Allow Autorepair")]
    public bool AutoRepair { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Train")]
    [DisplayName("Training")]
    [Description("Allow Autotraining")]
    public bool AutoTrain { get; set; }

    //[Setting]
    //[DefaultValue(false)]
    //[Category("Database")]
    //[DisplayName("Database Intern/Extern")]
    //[Description("You can choose between intern and external Database")]
    //[DropdownList(new string[] {"internal","external" })]
    public string Databasetype { get; set; }

    public double LastUpdateDate { get; set; }
    public int LastLevelTrained { get; set; }
    public List<VendorItem> VendorItems { get; set; }

    public PluginSettings()
    {
        Databasetype = "external";
        AutobuyFood = true;
        AutoBuyWater = true;
        AllowAutobuyPoison = true;
        AutobuyAmmunition = true;
        AutobuyAmmunitionAmount = 2000;
        AutoRepair = true;
        AllowAutoSell = true;
        AutoTrain = true;
        LastUpdateDate = 0;
        LastLevelTrained = (int)ObjectManager.Me.Level;

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