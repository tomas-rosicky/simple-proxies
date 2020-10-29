using System;
using System.Net;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;

namespace tproxy
{
    public class ZProxy
    {
        public ZProxy(ZNic nic, int port)
        {
            Nic = nic;
            LocalAddress = nic.IpAddress;
            Port = port;
        }

        public void StartProxyServer(string localAddress = null, int port = 5000)
        {
            int serverPort = Port == 0 ? port : Port;
            ProxyServer = new ProxyServer();

            ExplicitProxyEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, serverPort, false);

            ProxyServer.AddEndPoint(ExplicitProxyEndPoint);
            string upStreamEndPointAddress = localAddress ?? LocalAddress;
            if (!string.IsNullOrEmpty(upStreamEndPointAddress))
            {
                ProxyServer.UpStreamEndPoint = new IPEndPoint(IPAddress.Parse(upStreamEndPointAddress), 0);
            }

            ProxyServer.Start();

            foreach (var endPoint in ProxyServer.ProxyEndPoints)
                Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} , LocalAddress: {3}",
                    endPoint.GetType().Name, endPoint.IpAddress, endPoint.Port, upStreamEndPointAddress);
        }

        public void StopProxyServer()
        {
            ProxyServer.Stop();
            Console.WriteLine("Stopping on '{0}' endpoint at Ip {1} and port: {2} , LocalAddress: {3}",
                    ExplicitProxyEndPoint.GetType().Name, ExplicitProxyEndPoint.IpAddress, ExplicitProxyEndPoint.Port, ProxyServer.UpStreamEndPoint.Address);
        }

        public ProxyServer ProxyServer { get; set; }
        public ExplicitProxyEndPoint ExplicitProxyEndPoint { get; set; }
        public int Port { get; set; }
        public string LocalAddress { get; set; }
        public ZNic Nic { get; set; }
    }
}
