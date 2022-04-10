using robotManager.Helpful;
using System.Data.SQLite;
using System.Net;

namespace WholesomeVendors.Database
{
    public class DBUpdater
    {
        private static SQLiteConnection _con;
        private static SQLiteCommand _cmd;

        public DBUpdater()
        {
            string baseDirectory = Others.GetCurrentDirectory + @"Data\WoWDb335;Cache=Shared;";
            _con = new SQLiteConnection("Data Source=" + baseDirectory);
        }

        public bool CheckUpdate()
        {
            _con.Open();
            _cmd = _con.CreateCommand();
            _cmd.CommandText = "SELECT COUNT(*) FROM creature";
            int totalCount = int.Parse(_cmd.ExecuteScalar().ToString());
            _cmd.CommandText = "SELECT COUNT(*) FROM creature WHERE zoneId=0";
            int zeroCount = int.Parse(_cmd.ExecuteScalar().ToString());
            float ratio = (float)zeroCount / totalCount * 100;
            _con.Dispose();
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
                {
                    _con.Open();
                    _cmd = _con.CreateCommand();
                    _cmd.CommandText = line;
                    _cmd.ExecuteNonQuery();
                    _con.Dispose();
                }

            Logging.Write("Updated database.");
            return true;
        }
    }
}