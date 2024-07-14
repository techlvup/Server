using System.Net.Sockets;



namespace Server
{
    class ClientItem
    {
        public Socket socket;
        public byte[] messageByte;
        public ClientItem(Socket socket)
        {
            this.socket = socket;
            messageByte = new byte[socket.ReceiveBufferSize];
        }
    }
}