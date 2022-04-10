using MarsSettingsGUI;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.WVSettings
{
    [Serializable]
    public class PluginSettings : Settings
    {
        [Setting]
        [DropdownList(new string[] { "Any", "Meat", "Fish", "Cheese", "Bread", "Fungus", "Fruit" })]
        [Category("Buy")]
        [DisplayName("Food type")]
        [Description("Food type to buy")]
        public string FoodType { get; set; }

        [Setting]
        [DefaultValue(20)]
        [Category("Buy")]
        [DisplayName("Food Amount")]
        [Description("Food amount to buy")]
        public int FoodNbToBuy { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Buy")]
        [DisplayName("Best food")]
        [Description("Will only buy the best possible food")]
        public bool BestFood { get; set; }

        [Setting]
        [DefaultValue(0)]
        [Category("Buy")]
        [DisplayName("Drink Amount")]
        [Description("Drink amount to buy")]
        public int DrinkNbToBuy { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Buy")]
        [DisplayName("Best drink")]
        [Description("Will only buy the best possible drinks")]
        public bool BestDrink { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Buy")]
        [DisplayName("Poison")]
        [Description("Allow buying poison")]
        public bool BuyPoison { get; set; }

        [Setting]
        [DefaultValue(0)]
        [Category("Buy")]
        [DisplayName("Ammo Amount")]
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
        [DefaultValue(false)]
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

        [Setting]
        [DefaultValue(false)]
        [Category("Bags")]
        [DisplayName("Buy bags")]
        [Description("Buy bags of specified capacity. ONLY works if you have empty bag slots.")]
        public bool BuyBags { get; set; }

        [Setting]
        [DropdownList(new string[] { "6", "8", "10", "12" })]
        [DefaultValue("6")]
        [Category("Bags")]
        [DisplayName("Capacity")]
        [Description("Capacity of bags to purchase")]
        public string BagsCapacity { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Mount")]
        [DisplayName("Ground Mount")]
        [Description("Buys a random normal mount (+60% speed) if you don't already have one. Only works if you are on the right continent.")]
        public bool BuyGroundMount { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Mount")]
        [DisplayName("Epic Mount")]
        [Description("Buys a random epic mount (+100% speed) if you don't already have one. Only works if you are on the right continent.")]
        public bool BuyEpicMount { get; set; }

        public string Databasetype { get; set; }
        public int DriveByDistance { get; set; }
        public double LastUpdateDate { get; set; }
        public int LastLevelTrained { get; set; }

        public PluginSettings()
        {
            Databasetype = "external";
            DriveByDistance = 100;
            FoodNbToBuy = 20;
            BestFood = false;
            DrinkNbToBuy = 0;
            BestDrink = false;
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

            BuyBags = false;
            BagsCapacity = "6";

            BuyGroundMount = false;
            BuyEpicMount = false;

            FoodType = "Any";

            TrainLevels = new List<int> { };
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
}