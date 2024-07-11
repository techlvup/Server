using System.Net.Sockets;

namespace Server
{
    class ConnectSocket
    {
        public Socket socket;
        public byte[] messageByte;
        public ConnectSocket(Socket socket)
        {
            this.socket = socket;
            messageByte = new byte[socket.ReceiveBufferSize];
        }
    }
}