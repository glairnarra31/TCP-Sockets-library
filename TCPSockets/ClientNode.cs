using System.Net;
using System.Net.Sockets;

namespace TCPSockets
{
    public class ClientNode
    {
        public readonly TcpClient tcpClient;
        public byte[] TX, RX;
        public readonly string macAddress;

        public ClientNode(TcpClient tcpClient, int bufferSize = 512)
        {
            this.tcpClient = tcpClient;
            RX = TX = new byte[bufferSize];
            macAddress = Mac.GetMacAddress(tcpClient.Client.RemoteEndPoint as IPEndPoint);
        }
    }
}
