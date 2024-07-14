using System.Text;
using System.Net;
using System.Net.Sockets;



namespace Server
{
    class ServerManager : Singleton<ServerManager>
    {
        private const string m_ipv4Str = "0.0.0.0";//接收所有客户端的连接请求 
        private const int m_portInt = 2000;//为客户端打开的访问端口
        private const int m_maxClientNum = 100;

        private Socket? m_serverSocket;
        private HashSet<ClientItem>? m_clientItems;

        ~ServerManager()
        {
            CloseAllSocket();
        }

        public void Play()
        {
            m_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress iPAddress = IPAddress.Parse(m_ipv4Str);
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, m_portInt);

            m_serverSocket.Bind(iPEndPoint);

            m_serverSocket.Listen(m_maxClientNum);

            Console.WriteLine("服务器已启动！");

            m_clientItems = new HashSet<ClientItem>();

            m_serverSocket.BeginAccept(AcceptAsyncCallBack, m_serverSocket);
            Console.WriteLine("开始监听用户上线请求！");
        }

        private void AcceptAsyncCallBack(IAsyncResult asyncResult)
        {
            //(Socket)asyncResult.AsyncState;//就是之前传入的serverSocket

            ClientItem clientItem = new ClientItem(m_serverSocket.EndAccept(asyncResult));

            m_clientItems.Add(clientItem);

            Console.WriteLine("客户端" + clientItem.socket.RemoteEndPoint + "上线！");

            StartReceiveClientMessage(clientItem);

            //继续监听下一个用户的上线请求
            m_serverSocket.BeginAccept(AcceptAsyncCallBack, m_serverSocket);
            Console.WriteLine("继续监听下一个用户的上线请求！");
        }

        private void StartReceiveClientMessage(ClientItem clientItem)
        {
            clientItem.socket.BeginReceive(clientItem.messageByte, 0, clientItem.messageByte.Length, SocketFlags.None, ReceiveAsyncCallBack, clientItem);
        }

        private void ReceiveAsyncCallBack(IAsyncResult asyncResult)
        {
            ClientItem clientItem = (ClientItem)asyncResult.AsyncState;

            int length = clientItem.socket.EndReceive(asyncResult);

            if (length == 0)
            {
                Console.WriteLine(clientItem.socket.RemoteEndPoint + "客户端已下线！");
                clientItem.socket.Close();
                return;
            }

            string messageStr = Encoding.UTF8.GetString(clientItem.messageByte, 0, length);
            Console.WriteLine("从" + clientItem.socket.RemoteEndPoint + "客户端收到的数据：" + messageStr);

            StartReceiveClientMessage(clientItem);

            JudgeMessageKind(messageStr, clientItem);
        }

        private void JudgeMessageKind(string messageStr, ClientItem clientItem)
        {
            if (messageStr.Contains("|"))
            {
                string[] message = messageStr.Split("|");

                if (int.TryParse(message[0], out int id))
                {
                    if (m_clientItems.ElementAt(id) != null)
                    {
                        SendDataToClient(m_clientItems.ElementAt(id), Encoding.UTF8.GetBytes(message[1]));
                    }
                    else
                    {
                        SendDataToClient(clientItem, Encoding.UTF8.GetBytes("没有对应id"));
                    }
                }
                else
                {
                    switch (message[0])
                    {
                        case "message_info_1":
                            SendDataToClient(clientItem, Encoding.UTF8.GetBytes("第一种消息的数据"));
                            break;

                        case "message_info_2":
                            SendDataToClient(clientItem, Encoding.UTF8.GetBytes("第二种消息的数据"));
                            break;

                        default:
                            SendDataToClient(clientItem, Encoding.UTF8.GetBytes("没有该消息的处理函数"));
                            break;
                    }
                }
            }
            else
            {
                SendDataToClient(clientItem, Encoding.UTF8.GetBytes(messageStr));
            }
        }

        private void SendDataToClient(ClientItem clientItem, byte[] messageByte)
        {
            Socket clientSocket = clientItem.socket;

            StringBuilder stringBuilder = new StringBuilder();

            foreach (var item in m_clientItems)
            {
                stringBuilder.Append(item.socket.RemoteEndPoint + "\n");
            }

            byte[] commonMessage = Encoding.UTF8.GetBytes("客户端已上线个数为：" + m_clientItems.Count + "\n上线的客户端如以下所示：\n" + stringBuilder);

            foreach (var item in m_clientItems)
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
                clientSocket.BeginSend(messageByte, 0, messageByte.Length, SocketFlags.None, SendAsyncCallBack, clientItem);
                Console.WriteLine("发回" + clientSocket.RemoteEndPoint + "客户端的数据：" + Encoding.UTF8.GetString(messageByte));
            }
            catch
            {
                Console.WriteLine(clientSocket.RemoteEndPoint + "客户端已断开连接，无法发送数据到该客户端！");
            }
        }

        private void SendAsyncCallBack(IAsyncResult asyncResult)
        {
            ClientItem clientItem = (ClientItem)asyncResult.AsyncState;

            int lenth = clientItem.socket.EndSend(asyncResult);

            Console.WriteLine("数据发回" + clientItem.socket.RemoteEndPoint + "客户端成功！长度为:" + lenth);
        }

        private void CloseAllSocket()
        {
            if(m_clientItems != null)
            {
                foreach (var item in m_clientItems)
                {
                    item.socket.Close();
                }

                m_clientItems.Clear();
                m_clientItems = null;
            }
        }
    }
}