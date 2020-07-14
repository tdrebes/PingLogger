using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace PingLogger
{
    public class DatabaseConnector
    {
        private string dataSource = "data.sqlite";
        private string version = "3";
        private SQLiteConnection conn;

        public DatabaseConnector()
        {
            OpenDatabase(dataSource, version);
        }

        public int CreateDatabase()
        {
            try
            {
                SQLiteConnection.CreateFile(dataSource);
            }
            catch (IOException)
            {
                Debug.WriteLine("DataSource file is already in use.");
                return 0;
            }
            conn = new SQLiteConnection($"Data Source={dataSource};Version={version}");
            conn.Open();

            //---
            conn.Close();
            return 1;
        }

        public int CreatePingLogTable()
        {
            string sql = "CREATE TABLE IF NOT EXISTS ping_log (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "ping INT, " +
                "date TEXT, " +
                "time TEXT" +
                ")";
            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            return cmd.ExecuteNonQuery();
        }

        public int CreateTimoutLogTable()
        {
            string sql2 = "CREATE TABLE IF NOT EXISTS timeout_log (" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "date TEXT, " +
                "time TEXT" +
                ")";
            SQLiteCommand cmd2 = new SQLiteCommand(sql2, conn);
            return cmd2.ExecuteNonQuery();
        }

        public void OpenDatabase(string dataSource, string version)
        {
            conn = new SQLiteConnection($"Data Source={dataSource};Version={version}");
            conn.Open();
        }

        public void CloseDatabase()
        {
            conn.Close();
        }


        public int InsertPing(long ping)
        {
            string sql = $"INSERT INTO ping_log (ping, date, time) VALUES ({ping}, date('now'), time('now'))";
            Debug.WriteLine(sql);
            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected;
        }

        public int InsertTimeout()
        {
            string sql = $"INSERT INTO timeout_log (date, time) VALUES (date('now'), time('now'))";
            Debug.WriteLine(sql);
            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected;
        }

        public int ClearPingTable()
        {
            string sql = $"DELETE FROM ping_log; VACUUM; DELETE FROM sqlite_sequence WHERE name='ping_log'";
            Debug.WriteLine(sql);
            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected;
        }

        public DataSet getPingDataSet()
        {
            String sql = "Select* FROM ping_log; ";
            DataSet dataSet = new DataSet();
            SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, conn);
            dataAdapter.Fill(dataSet);
            return dataSet;
        }

        public void exportToXml(string path)
        {
            String sql = "Select* FROM ping_log; ";
            DataSet dataSet = new DataSet();
            SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, conn);
            dataAdapter.Fill(dataSet);
            //dataSet.WriteXml(path);

            int i = 0;
            StreamWriter sw = new StreamWriter(path, false);
            DataTable dt = dataSet.Tables[0];
            for (i = 0; i < dt.Columns.Count - 1; i++)
            {
                sw.Write(dt.Columns[i].ColumnName + " ");
            }
            sw.Write(dt.Columns[i].ColumnName);
            sw.WriteLine();
            foreach (DataRow row in dt.Rows)
            {
                object[] array = row.ItemArray;
                for (i = 0; i < array.Length - 1; i++)
                {
                    sw.Write(array[i] + " ");
                }
                sw.Write(array[i].ToString());
                sw.WriteLine();
            }
            sw.Close();

        }


    }
}
