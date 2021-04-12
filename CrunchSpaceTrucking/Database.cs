﻿using System;
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
      private static string connStr = "server="+TruckingPlugin.config.DatabaseIP+";user=" + TruckingPlugin.config.DatabaseUser+";port="+TruckingPlugin.config.DatabasePort+";password="+ TruckingPlugin.config.DatabasePassword;
        public static void testConnection()
        {
          
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                TruckingPlugin.Log.Info("Connecting to MySQL...");
                conn.Open();


                string sql = "CREATE DATABASE IF NOT EXISTS spacetrucking";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();

                sql = "CREATE TABLE IF NOT EXISTS spacetrucking.players(playerid BIGINT UNSIGNED PRIMARY KEY, reputation INT NOT NULL, completed INT NOT NULL, failed INT NOT NULL, currentContract CHAR(36))";
                cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                sql = "CREATE TABLE IF NOT EXISTS spacetrucking.contracts(contractId CHAR(36) PRIMARY KEY, x DOUBLE NOT NULL, y DOUBLE NOT NULL, z DOUBLE NOT NULL, reputation INT NOT NULL, completed BOOLEAN NOT NULL)";
                cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                sql = "CREATE TABLE IF NOT EXISTS spacetrucking.contractItems(id INT AUTO_INCREMENT PRIMARY KEY, contractId CHAR(36) NOT NULL, itemIdFromConfig VARCHAR(100) NOT NULL, difficulty VARCHAR(15) NOT NULL,  amountToDeliver INT NOT NULL)";
                cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                conn.Close();
                TruckingPlugin.Log.Info(ex.ToString());
            }
            conn.Close();
            TruckingPlugin.Log.Info("Created the tables if it needed to");
        }

        public static void addNewContract(ulong steamId, Contract contract)
        {
        

            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                string sql = "INSERT INTO spacetrucking.players(playerid, reputation, completed, failed, currentContract) VALUES ROW (" + steamId + ",0,0,0,'" + contract.GetContractId() + "') ON DUPLICATE KEY UPDATE currentContract='" + contract.GetContractId() + "'";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();

                sql = "INSERT INTO spacetrucking.contracts(contractId, x, y, z, reputation, completed) VALUES ROW ('" + contract.GetContractId() + "','" + contract.GetDeliveryLocation().Coords.X + "'," + contract.GetDeliveryLocation().Coords.Y + "," + contract.GetDeliveryLocation().Coords.Z + ",50,false)";
                cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                foreach (ContractItems item in contract.getItemsInContract())
                {
                    sql = "INSERT INTO spacetrucking.contractitems(contractId, itemIdFromConfig, difficulty, amountToDeliver) VALUES ROW ('" + contract.GetContractId() + "','" + item.ContractItemId + "','" + item.difficulty + "'," + item.AmountToDeliver + ")";
                    cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                conn.Close();
                TruckingPlugin.Log.Info(ex.ToString());
            }
            TruckingPlugin.activeContracts.Add(steamId, contract);
            conn.Close();
            TruckingPlugin.Log.Info("Inserted a new contract for whoever this is " + steamId);
        }

        public static Contract TryGetContract(ulong steamid)
        {
         
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                //this runs more queries than i would like, but the current version of mysql errors if any field in a query is null, when thats fixed it can go back to select * from players where playerId = steamid
                string sql = "select reputation from spacetrucking.players where playerid=" + steamid + "";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    TruckingPlugin.reputation.Remove(steamid);
                    TruckingPlugin.reputation.Add(steamid, reader.GetInt32(0));
                }
                reader.Close();
                //this runs more queries than i would like, but the current version of mysql errors if any field in a query is null, when thats fixed it can go back to select * from players where playerId = steamid
                sql = "select * from spacetrucking.players where playerid=" + steamid;
                cmd = new MySqlCommand(sql, conn);
                reader = cmd.ExecuteReader();
                Guid contractId = new Guid();

                while (reader.Read())
                {
                    if (reader[4] != null)
                    {
                        contractId = reader.GetGuid(4);
                    }
                    else
                    {
                        conn.Close();
                        TruckingPlugin.Log.Info("No contract to load " + steamid);
                        return null;
                    }
                }
                        
                reader.Close();
                double x = 0, y = 0, z = 0;
                int reputation = 0;
                sql = "select * from spacetrucking.contracts where contractId='" + contractId + "'";
                cmd = new MySqlCommand(sql, conn);
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
      

                    x = reader.GetDouble(1);
                    y = reader.GetDouble(2);
                    z = reader.GetDouble(3);
                    reputation = reader.GetInt32(4);

                }

                List<ContractItems> items = new List<ContractItems>();
                reader.Close();

                sql = "select * from spacetrucking.contractItems where contractId='" + contractId + "'";
                cmd = new MySqlCommand(sql, conn);
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {

                    ContractItems item = new ContractItems();
                    if (TruckingPlugin.getItemFromLists(reader.GetString(2), reader.GetString(3)) != null)
                    {
                        item = (TruckingPlugin.getItemFromLists(reader.GetString(2), reader.GetString(3)));
                        item.SetAmountToDeliver(reader.GetInt32(4));
                        items.Add(item);
                    }

                }
                Contract contract = new Contract(contractId, steamid, items, x, y, z, reputation);
                reader.Close();
                conn.Close();
                TruckingPlugin.Log.Info("Loading data for whoever this is " + steamid);
                return contract;
            }
            catch (Exception ex)
            {
                conn.Close();
                TruckingPlugin.Log.Info("This error with the mysql isnt fixed, its safe to ignore it " + steamid);
              
            }
            conn.Close();
            return null;
        }
        public static void RemoveContract(ulong steamid, Boolean completed, Contract contract, long identityid)
        {
          
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                string sql = "select * from spacetrucking.players where playerid=" + steamid;
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                Guid contractId = new Guid();
                int read = 0;
                while (reader.Read())
                {
                    read++;
                    if (!reader.IsDBNull(4))
                    {
                        contractId = Guid.Parse(reader.GetString(4));
                    }
                }
                if (read == 0)
                {
                    TruckingPlugin.Log.Info("No contract to load from database " + steamid);
                    return;
                }
                reader.Close();
                if (completed)
                {
                    sql = "UPDATE spacetrucking.players SET currentContract = NULL, completed = completed + 1, reputation = reputation + " + contract.GetReputation() + " where playerId =" + steamid;
                    if (TruckingPlugin.reputation.TryGetValue(steamid, out int rep))
                    {
                        TruckingPlugin.reputation.Remove(steamid);
                        TruckingPlugin.reputation.Add(steamid, contract.GetReputation() + rep);
                    }
                    TruckingPlugin.reputation.Add(steamid, contract.GetReputation());


                }
                else
                {
                    sql = "UPDATE spacetrucking.players SET currentContract = NULL, failed = failed + 1, reputation = reputation - " + (contract.GetReputation() * 2) + " where playerId =" + steamid;
                    if (TruckingPlugin.reputation.TryGetValue(steamid, out int rep))
                    {
                        TruckingPlugin.reputation.Remove(steamid);
                        TruckingPlugin.reputation.Add(steamid, rep - contract.GetReputation());
                    }
                    else
                    {
                        TruckingPlugin.reputation.Add(steamid, contract.GetReputation() * -1);
                    }
                }
                cmd = new MySqlCommand(sql, conn);
                TruckingPlugin.RemoveContract(steamid, identityid);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                conn.Close();
                TruckingPlugin.Log.Info(ex.ToString());
            }
            conn.Close();
            return;
        }

    }


}
