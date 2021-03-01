using System;
using System.Data;
using System.Data.SQLite;
using robotManager.Helpful;

namespace WoWDBUpdater
{
    public class DB : IDisposable
    {
        private readonly SQLiteConnection _con;
        private readonly SQLiteCommand _cmd;

        public DB()
        {
            _con = new SQLiteConnection("Data Source=Data/WoWDb335");
            _con.Open();
            _cmd = _con.CreateCommand();
        }

        public void Dispose()
        {
            _con?.Close();
        }

        public DataTable SelectQuery(string query)
        {
            var dt = new DataTable();

            try
            {
                _cmd.CommandText = query;
                var ad = new SQLiteDataAdapter(_cmd);
                ad.Fill(dt);
            }
            catch (SQLiteException ex)
            {
                Logging.WriteError("Failed to execute query. " + ex.Message);
            }

            return dt;
        }

        public void ExecuteQuery(string query)
        {
            _cmd.CommandText = query;
            _cmd.ExecuteNonQuery();
        }

        public string GetQuery(string query)
        {
            _cmd.CommandText = query;
            return _cmd.ExecuteScalar().ToString();
        }
    }
}