using System;
using System.Data.SQLite;
using System.Net;
using System.Threading.Tasks;
using robotManager.Helpful;

namespace WoWDBUpdater
{
    public class DBUpdater
    {
        private readonly DB _db;

        public DBUpdater(DB database)
        {
            _db = database;
        }

        public bool CheckUpdate()
        {
            int totalCount = int.Parse(_db.GetQuery("SELECT COUNT(*) FROM creature"));
            int zeroCount = int.Parse(_db.GetQuery("SELECT COUNT(*) FROM creature WHERE zoneId=0"));
            float ratio = (float)zeroCount / totalCount * 100;
            return ratio >= 50;
        }

        public bool Update()
        {
            Logging.Write("Updating database.");
            Logging.WriteDebug("Downloading wholesome database update.");
            string updateQueries;
            using (var client = new WebClient())
            {
                try
                {
                    updateQueries =
                        client.DownloadString("https://s3-eu-west-1.amazonaws.com/wholesome.team/update_db.txt");
                }
                catch (WebException e)
                {
                    Logging.WriteError("Failed to download wholesome database update.\n" + e.Message);
                    return false;
                }
            }

            Logging.WriteDebug("Executing queries.");

            foreach (string line in updateQueries.Replace("\r\n", "\n").Split('\n'))
                if (line.Length > 0)
                    _db.ExecuteQuery(line);

            Logging.Write("Updated database.");
            return true;
        }
    }
}