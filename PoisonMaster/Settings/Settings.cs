using robotManager.Helpful;
using robotManager.Products;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.ComponentModel;
using System.IO;
using MarsSettingsGUI;

[Serializable]
public class PluginSettings : Settings
{
    [Setting]
    [DefaultValue(true)]
    [Category("Buying")]
    [DisplayName("Buy Food")]
    [Description("Allow Autobuy Food")]
    public bool AllowAutobuyFood { get; set; }

    [Setting]
    [DefaultValue(true)]
    [Category("Buying")]
    [DisplayName("Buy Water")]
    [Description("Allow Autobuy Water")]
    public bool AllowAutobuyWater { get; set; }

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
    public bool AllowAutobuyAmmunition { get; set; }

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
    public bool AllowAutoRepair { get; set; }

    [Setting]
    [DefaultValue(false)]
    [Category("Database")]
    [DisplayName("Database Intern/Extern")]
    [Description("You can choose between intern and external Database")]
    [DropdownList(new string[] {"internal","external" })]
    public string Databasetype { get; set; }

    public PluginSettings()
    {
        Databasetype = "external";
        AllowAutobuyFood = true;
        AllowAutobuyWater = true;
        AllowAutobuyPoison = true;
        AllowAutobuyAmmunition = true;
        AllowAutoRepair = true;
        AllowAutoSell = true;

        ConfigWinForm(new System.Drawing.Point(800, 600));
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
        settingWindow.Title = $"{ObjectManager.Me.Name}";
    }
}