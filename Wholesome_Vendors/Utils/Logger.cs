using robotManager.Helpful;
using System.Drawing;

namespace WholesomeVendors.Utils
{
    public class Logger
    {
        private static readonly string _pluginName = "Wholesome Vendors";

        public static void Log(string message)
        {
            Logging.Write($"[{_pluginName}]: {message}", Logging.LogType.Normal, Color.ForestGreen);
        }
        public static void LogError(string message)
        {
            Logging.Write($"[{_pluginName}]: {message}", Logging.LogType.Normal, Color.Red);
        }
    }
}
