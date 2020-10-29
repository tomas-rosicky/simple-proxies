using System.Linq;
using System.Net.NetworkInformation;

namespace tproxy
{
    public class ZNic
    {
        public ZNic(NetworkInterface ni)
        {
            Name = ni.Name;
            NIType = ni.NetworkInterfaceType;

            UnicastIPAddressInformation ip = ni.GetIPProperties().UnicastAddresses
                                                .Where(ipa => ipa.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                                .FirstOrDefault();
            IpAddress = ip?.Address.ToString();

            GatewayIPAddressInformation gate = ni.GetIPProperties().GatewayAddresses
                                                    .Where(ipa => ipa.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                                    .FirstOrDefault();
            GatewayAddress = gate?.Address.ToString();

            MACAddress = ni.GetPhysicalAddress().ToString();
        }
        public NetworkInterfaceType NIType { get; set; }
        public string NITypeName { get; set; }
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string GatewayAddress { get; set; }
        public string MACAddress { get; set; }
    }
}
