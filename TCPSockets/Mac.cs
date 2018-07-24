using System;
using System.Net;
using System.Net.NetworkInformation;

namespace TCPSockets
{
    public static class Mac
    {
        [System.Runtime.InteropServices.DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref int PhyAddrLen);

        public static string GetMacAddress(IPEndPoint ip)
        {
            IPAddress.TryParse(ip.Address.ToString(), out IPAddress clientmac);
            const int MacAddressLength = 6;
            int length = MacAddressLength;
            var macBytes = new byte[MacAddressLength];
            SendARP(BitConverter.ToInt32(clientmac.GetAddressBytes(), 0), 0, macBytes, ref length);
            string mac = new PhysicalAddress(macBytes).ToString();
            for (int i = 0; i < 5; i++)
            {
                mac = mac.Insert(2 + (i * 3), ":");
            }
            return mac;
        }
    }
}
