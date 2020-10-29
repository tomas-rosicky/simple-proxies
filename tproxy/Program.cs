using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace tproxy
{
    class Program
    {
        static void Main(string[] args)
        {
            var nics = new List<ZNic>();
            // Get all network interfaces
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up
                    && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    nics.Add(new ZNic(ni));
                }
            }

            var proxyServers = new List<ZProxy>();
            var startPort = 8000;

            foreach (var nic in nics)
            {
                var proxyServer = new ZProxy(nic, ++startPort);
                proxyServer.StartProxyServer();

                proxyServers.Add(proxyServer);
            }
            Console.Read();


            // Stop proxy servers
            foreach (var ps in proxyServers)
            {
                ps.StopProxyServer();
            }
        }
    }
}
