using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace CrunchSpaceTrucking
{
    public static class Database
    {
        public static void testConnection()
        {
            string connStr = "server=localhost;user=root;database=world;port=3306;password=******";
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                TruckingPlugin.Log.Info("Connecting to MySQL...");
                conn.Open();

                string sql = "CREATE TABLE IF NOT EXISTS players(playerid BIGINT UNSIGNED PRIMARY KEY, reputation INT NOT NULL, completed INT NOT NULL, failed INT NOT NULL, currentContract CHAR(36)";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                rdr.Close();
                sql = "CREATE TABLE IF NOT EXISTS contracts(currentContract CHAR(36) PRIMARY KEY, x DOUBLE NOT NULL, y DOUBLE NOT NULL, z DOUBLE NOT NULL, reputation INT NOT NULL, completed BOOLEAN NOT NULL)";
                cmd = new MySqlCommand(sql, conn);
                rdr = cmd.ExecuteReader();
                rdr.Close();
                sql = "CREATE TABLE IF NOT EXISTS contractItems(id INT AUTO_INCREMENT PRIMARY KEY, contractId CHAR(36) NOT NULL, itemtype VARCHAR(50) NOT NULL, subtype VARCHAR(100) NOT NULL, amountToDeliver INT NOT NULL)";
                cmd = new MySqlCommand(sql, conn);
                rdr = cmd.ExecuteReader();
                rdr.Close();
            }
            catch (Exception ex)
            {
                TruckingPlugin.Log.Info(ex.ToString());
            }
            conn.Close();
            TruckingPlugin.Log.Info("Done.");
        }
    }

}
