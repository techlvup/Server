using System;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;

namespace Server
{
    class StartServer
    {
        private string ipv4Str;
        private int portInt;
        private int MaxClientConnectNum;

        private Socket serverSocket;
        private HashSet<ConnectSocket> connectSockets;

        public StartServer(string ipv4Str, int portInt, int MaxClientConnectNum)
        {
            this.ipv4Str = ipv4Str;
            this.portInt = portInt;
            this.MaxClientConnectNum = MaxClientConnectNum;
        }

        ~StartServer()
        {
            CloseAllSocket();
        }

        public void Play()
        {
            InitServerSocket();

            StartAcceptClient();
        }

        private void InitServerSocket()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress iPAddress = IPAddress.Parse(ipv4Str);
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, portInt);

            serverSocket.Bind(iPEndPoint);

            serverSocket.Listen(MaxClientConnectNum);

            Console.WriteLine("服务器" + serverSocket.LocalEndPoint + "已启动！");

            connectSockets = new HashSet<ConnectSocket>();
        }

        private void StartAcceptClient()
        {
            serverSocket.BeginAccept(AcceptAsyncCallBack, serverSocket);
        }

        private void AcceptAsyncCallBack(IAsyncResult asyncResult)
        {
            //(Socket)asyncResult.AsyncState;//就是之前传入的serverSocket

            ConnectSocket connectSocket = new ConnectSocket(serverSocket.EndAccept(asyncResult));

            connectSockets.Add(connectSocket);

            Console.WriteLine(connectSocket.socket.RemoteEndPoint + "客户端上线");

            StartReceiveClientMessage(connectSocket);

            //继续监听下一个用户的上线请求
            StartAcceptClient();
        }

        private void StartReceiveClientMessage(ConnectSocket connectSocket)
        {
            connectSocket.socket.BeginReceive(connectSocket.messageByte, 0, connectSocket.messageByte.Length, SocketFlags.None, ReceiveAsyncCallBack, connectSocket);
        }

        private void ReceiveAsyncCallBack(IAsyncResult asyncResult)
        {
            ConnectSocket connectSocket = (ConnectSocket)asyncResult.AsyncState;

            int length = connectSocket.socket.EndReceive(asyncResult);

            if (length == 0)
            {
                Console.WriteLine(connectSocket.socket.RemoteEndPoint + "客户端已下线！");
                connectSocket.socket.Close();
                return;
            }

            string messageStr = Encoding.UTF8.GetString(connectSocket.messageByte, 0, length);
            Console.WriteLine("从" + connectSocket.socket.RemoteEndPoint + "客户端收到的数据：" + messageStr);

            StartReceiveClientMessage(connectSocket);

            JudgeMessageKind(messageStr, connectSocket);
        }

        private void JudgeMessageKind(string messageStr, ConnectSocket connectSocket)
        {
            if (messageStr.Contains("|"))
            {
                string[] message = messageStr.Split("|");

                if (int.TryParse(message[0], out int id))
                {
                    if (connectSockets.ElementAt(id) != null)
                    {
                        SendDataToClient(connectSockets.ElementAt(id), Encoding.UTF8.GetBytes(message[1]));
                    }
                    else
                    {
                        SendDataToClient(connectSocket, Encoding.UTF8.GetBytes("没有对应id"));
                    }
                }
                else
                {
                    switch (message[0])
                    {
                        case "message_info_1":
                            SendDataToClient(connectSocket, Encoding.UTF8.GetBytes("第一种消息的数据"));
                            break;

                        case "message_info_2":
                            SendDataToClient(connectSocket, Encoding.UTF8.GetBytes("第二种消息的数据"));
                            break;

                        default:
                            SendDataToClient(connectSocket, Encoding.UTF8.GetBytes("没有该消息的处理函数"));
                            break;
                    }
                }
            }
            else
            {
                SendDataToClient(connectSocket, Encoding.UTF8.GetBytes(messageStr));
            }
        }

        private void SendDataToClient(ConnectSocket connectSoc, byte[] messageByte)
        {
            Socket connectSocket = connectSoc.socket;

            StringBuilder stringBuilder = new StringBuilder();

            foreach (var item in connectSockets)
            {
                stringBuilder.Append(item.socket.RemoteEndPoint + "\n");
            }

            byte[] commonMessage = Encoding.UTF8.GetBytes("客户端已上线个数为：" + connectSockets.Count + "\n上线的客户端如以下所示：\n" + stringBuilder);

            foreach (var item in connectSockets)
            {
                try
                {
                    item.socket.BeginSend(commonMessage, 0, commonMessage.Length, SocketFlags.None, SendAsyncCallBack, item);
                }
                catch
                {
                    Console.WriteLine(item.socket.RemoteEndPoint + "客户端已断开连接，无法发送数据到该客户端！");
                }
            }

            try
            {
                connectSocket.BeginSend(messageByte, 0, messageByte.Length, SocketFlags.None, SendAsyncCallBack, connectSoc);
                Console.WriteLine("发回" + connectSocket.RemoteEndPoint + "客户端的数据：" + Encoding.UTF8.GetString(messageByte));
            }
            catch
            {
                Console.WriteLine(connectSocket.RemoteEndPoint + "客户端已断开连接，无法发送数据到该客户端！");
            }
        }

        private void SendAsyncCallBack(IAsyncResult asyncResult)
        {
            ConnectSocket connectSocket = (ConnectSocket)asyncResult.AsyncState;

            int lenth = connectSocket.socket.EndSend(asyncResult);

            Console.WriteLine("数据发回" + connectSocket.socket.RemoteEndPoint + "客户端成功！长度为:" + lenth);
        }

        private void CloseAllSocket()
        {
            foreach (var item in connectSockets)
            {
                item.socket.Close();
            }

            connectSockets.Clear();
        }
    }
}