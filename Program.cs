﻿using System;



namespace Server
{
    class Program
    {
        private static string ipv4Str = "192.168.1.6";//ipv4地址
        private static int portInt = 2000;//端口
        private static int MaxClientConnectNum = 100;

        static void Main(string[] args)
        {
            StartServer startServer = new StartServer(ipv4Str, portInt, MaxClientConnectNum);

            startServer.Play();

            int index = 10;
            int count = 0;

            SetDataBase(ref index, ref count);

            SetDataBase(ref index, ref count);

            SetDataBase(ref index, ref count);

            SetDataBase(ref index, ref count);

            SetDataBase(ref index, ref count);

            Console.ReadKey();
        }

        private static void SetDataBase(ref int index, ref int count)
        {
            string value = index.ToString();

            string operation = Console.ReadLine();

            if (operation == "insert")
            {
                DataBaseManager.InsertData("databaseName", "tableName", "columnName", "varchar(100)", value);
                count++;
            }
            else if (operation == "delete")
            {
                DataBaseManager.DeleteData("databaseName", "tableName", "columnName", count);
                count--;
            }
            else if (operation == "update")
            {
                DataBaseManager.UpdateData("databaseName", "tableName", "columnName", count, value);
            }
            else if (operation == "select")
            {
                DataBaseManager.SelectData("databaseName", "tableName", out Dictionary<int, string> rowsData, out Dictionary<string, string> columnsData, out string singleData, "columnName", count);
                Console.WriteLine($"查找到的数据是：{singleData}");
            }
            else if (operation == "close")
            {
                Environment.Exit(0);
            }

            index++;
        }
    }
}