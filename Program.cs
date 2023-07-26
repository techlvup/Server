using System;

namespace Server
{
    class Program
    {
        private static string ipv4Str = "192.168.1.4";//ipv4地址
        private static int portInt = 2000;//端口
        private static int MaxClientConnectNum = 100;

        static void Main(string[] args)
        {
            StartServer startServer = new StartServer(ipv4Str, portInt, MaxClientConnectNum);

            startServer.Play();

            Console.ReadKey();
        }
    }
}