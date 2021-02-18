using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Net;
using System.Text;
using System.Threading;

public static class AutoUpdater
{
    public static bool CheckUpdate(string MyCurrentVersion)
    {
        if (wManager.Information.Version.Contains("1.7.2"))
        {
            Main.Logger($"Plugin couldn't load (v {wManager.Information.Version})");
            Products.ProductStop();
            return false;
        }

        DateTime dateBegin = new DateTime(2020, 1, 1);
        DateTime currentDate = DateTime.Now;

        long elapsedTicks = currentDate.Ticks - dateBegin.Ticks;
        elapsedTicks /= 10000000;

        double timeSinceLastUpdate = elapsedTicks - PluginSettings.CurrentSetting.LastUpdateDate;

        // If last update try was < 30 seconds ago, we exit to avoid looping
        if (timeSinceLastUpdate < 30)
        {
            Main.Logger($"Last update attempts was {timeSinceLastUpdate} seconds ago. Exiting updater.");
            return false;
        }

        try
        {
            PluginSettings.CurrentSetting.LastUpdateDate = elapsedTicks;
            PluginSettings.CurrentSetting.Save();

            string onlineFile = "https://github.com/Talamin/PoisonMaster/raw/master/PoisonMaster/Compiled/Wholesome_Vendors.dll";

            // Version check
            string onlineVersion = "https://raw.githubusercontent.com/Talamin/PoisonMaster/master/PoisonMaster/Compiled/Version.txt";
            var onlineVersionContent = new WebClient { Encoding = Encoding.UTF8 }.DownloadString(onlineVersion);
            if (onlineVersionContent == null || onlineVersionContent.Length > 10 || onlineVersionContent == MyCurrentVersion)
            {
                Main.Logger($"Your version is up to date ({MyCurrentVersion})");
                return false;
            }

            // File check
            string currentFile = Others.GetCurrentDirectory + @"\Plugins\Wholesome_Vendors.dll";
            var onlineFileContent = new WebClient { Encoding = Encoding.UTF8 }.DownloadData(onlineFile);
            if (onlineFileContent != null && onlineFileContent.Length > 0)
            {
                Main.Logger($"Your version : {MyCurrentVersion} - Online Version : {onlineVersionContent}");
                Main.Logger("Updating");
                System.IO.File.WriteAllBytes(currentFile, onlineFileContent); // replace user file by online file
                Thread.Sleep(5000);
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
