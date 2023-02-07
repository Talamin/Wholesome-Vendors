using robotManager.Helpful;
using robotManager.Products;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using WholesomeVendors.Utils;
using WholesomeVendors.WVSettings;

namespace WholesomeVendors
{
    public static class AutoUpdater
    {
        public static bool CheckUpdate(string mainVersion)
        {
            if (wManager.Information.Version.Contains("1.7.2"))
            {
                Logger.Log($"Plugin couldn't load (v {wManager.Information.Version})");
                Products.ProductStop();
                return false;
            }

            Version currentVersion = new Version(mainVersion);

            DateTime dateBegin = new DateTime(2020, 1, 1);
            DateTime currentDate = DateTime.Now;

            long elapsedTicks = currentDate.Ticks - dateBegin.Ticks;
            elapsedTicks /= 10000000;

            double timeSinceLastUpdate = elapsedTicks - PluginSettings.CurrentSetting.LastUpdateDate;

            // If last update try was < 30 seconds ago, we exit to avoid looping
            if (timeSinceLastUpdate < 30)
            {
                Logger.Log($"Last update attempts was {timeSinceLastUpdate} seconds ago. Exiting updater.");
                return false;
            }

            try
            {
                PluginSettings.CurrentSetting.LastUpdateDate = elapsedTicks;
                PluginSettings.CurrentSetting.Save();
                string onlineDllLink = "https://github.com/Talamin/Wholesome-Vendors/raw/master/Wholesome_Vendors/Compiled/Wholesome_Vendors.dll";
                string onlineVersionLink = "https://raw.githubusercontent.com/Talamin/PoisonMaster/master/Wholesome_Vendors/Compiled/Auto_Version.txt";

                var onlineVersionTxt = new WebClient { Encoding = Encoding.UTF8 }.DownloadString(onlineVersionLink);
                Version onlineVersion = new Version(onlineVersionTxt);

                if (onlineVersion.CompareTo(currentVersion) <= 0)
                {
                    Logger.Log($"Your version is up to date ({currentVersion} / {onlineVersion})");
                    return false;
                }

                // File check
                string currentFile = Others.GetCurrentDirectory + @"\Plugins\Wholesome_Vendors.dll";
                var onlineFileContent = new WebClient { Encoding = Encoding.UTF8 }.DownloadData(onlineDllLink);
                if (onlineFileContent != null && onlineFileContent.Length > 0)
                {
                    Logger.Log($"Updating your version {currentVersion} to online Version {onlineVersion}");
                    File.WriteAllBytes(currentFile, onlineFileContent); // Replace user file by online file
                    File.Delete(Others.GetCurrentDirectory + @"Data\WVM.json"); // Delete json to retrigger an extraction
                    Thread.Sleep(1000);
                    return true;
                }
            }
            catch (Exception e)
            {
                Logging.WriteError("Auto update: " + e);
            }
            return false;
        }
    }
}