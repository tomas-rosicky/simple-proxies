﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace tproxy
{
    public class Sample
    {
        public void Main(string[] args)
        {
            var proxyServer = new ProxyServer();

            // locally trust root certificate used by this proxy 
            //proxyServer.CertificateManager.TrustRootCertificate(true);

            // optionally set the Certificate Engine
            // Under Mono only BouncyCastle will be supported
            //proxyServer.CertificateManager.CertificateEngine = Network.CertificateEngine.BouncyCastle;

            proxyServer.BeforeRequest += OnRequest;
            proxyServer.BeforeResponse += OnResponse;
            proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;

            proxyServer.OnClientConnectionCreate += ProxyServer_OnClientConnectionCreate;
            proxyServer.OnServerConnectionCreate += ProxyServer_OnServerConnectionCreate;


            var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, true)
            {
                // Use self-issued generic certificate on all https requests
                // Optimizes performance by not creating a certificate for each https-enabled domain
                // Useful when certificate trust is not required by proxy clients
                //GenericCertificate = new X509Certificate2(Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "genericcert.pfx"), "password")
            };

            // Fired when a CONNECT request is received
            explicitEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectRequest;

            // An explicit endpoint is where the client knows about the existence of a proxy
            // So client sends request in a proxy friendly manner
            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();

            // Transparent endpoint is useful for reverse proxy (client is not aware of the existence of proxy)
            // A transparent endpoint usually requires a network router port forwarding HTTP(S) packets or DNS
            // to send data to this endPoint
            var transparentEndPoint = new TransparentProxyEndPoint(IPAddress.Any, 8001, true)
            {
                // Generic Certificate hostname to use
                // when SNI is disabled by client
                GenericCertificateName = "google.com"
            };

            proxyServer.AddEndPoint(transparentEndPoint);

            //proxyServer.UpStreamHttpProxy = new ExternalProxy() { HostName = "localhost", Port = 8888 };
            //proxyServer.UpStreamHttpsProxy = new ExternalProxy() { HostName = "localhost", Port = 8888 };

            foreach (var endPoint in proxyServer.ProxyEndPoints)
                Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ",
                    endPoint.GetType().Name, endPoint.IpAddress, endPoint.Port);

            // Only explicit proxies can be set as system proxy!
            proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
            proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

            // wait here (You can use something else as a wait function, I am using this as a demo)
            Console.Read();

            // Unsubscribe & Quit
            explicitEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectRequest;
            proxyServer.BeforeRequest -= OnRequest;
            proxyServer.BeforeResponse -= OnResponse;
            proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;

            proxyServer.Stop();
        }

        private Task ProxyServer_OnServerConnectionCreate(object sender, System.Net.Sockets.Socket e)
        {
            Console.WriteLine("Server " + e.LocalEndPoint.ToString());
            return Task.CompletedTask;
        }

        private Task ProxyServer_OnClientConnectionCreate(object sender, System.Net.Sockets.Socket e)
        {
            Console.WriteLine("Client " + e.LocalEndPoint.ToString());
            return Task.CompletedTask;
        }

        private async Task OnBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            //e.HttpClient.UpStreamEndPoint = new IPEndPoint(IPAddress.Parse("192.168.31.221"), 0);
            //var clientLocalIp = e.ClientLocalEndPoint.Address;
            //if (!clientLocalIp.Equals(IPAddress.Loopback) && !clientLocalIp.Equals(IPAddress.IPv6Loopback))
            //{
            //    e.HttpClient.UpStreamEndPoint = new IPEndPoint(clientLocalIp, 0);
            //}
            string hostname = e.HttpClient.Request.RequestUri.Host;
            e.DecryptSsl = false;

            if (hostname.Contains("dropbox.com"))
            {
                // Exclude Https addresses you don't want to proxy
                // Useful for clients that use certificate pinning
                // for example dropbox.com
                e.DecryptSsl = false;
            }
        }

        public async Task OnRequest(object sender, SessionEventArgs e)
        {
            Console.WriteLine("ddddd: " + e.HttpClient.Request.Url);

            // read request headers
            var requestHeaders = e.HttpClient.Request.Headers;

            var method = e.HttpClient.Request.Method.ToUpper();
            if ((method == "POST" || method == "PUT" || method == "PATCH"))
            {
                // Get/Set request body bytes
                byte[] bodyBytes = await e.GetRequestBody();
                e.SetRequestBody(bodyBytes);

                // Get/Set request body as string
                string bodyString = await e.GetRequestBodyAsString();
                e.SetRequestBodyString(bodyString);

                // store request 
                // so that you can find it from response handler 
                e.UserData = e.HttpClient.Request;
            }

            // To cancel a request with a custom HTML content
            // Filter URL
            if (e.HttpClient.Request.RequestUri.AbsoluteUri.Contains("google.com"))
            {
                e.Ok("<!DOCTYPE html>" +
                    "<html><body><h1>" +
                    "Website Blocked" +
                    "</h1>" +
                    "<p>Blocked by titanium web proxy.</p>" +
                    "</body>" +
                    "</html>");
            }

            // Redirect example
            if (e.HttpClient.Request.RequestUri.AbsoluteUri.Contains("wikipedia.org"))
            {
                e.Redirect("https://www.paypal.com");
            }
        }

        // Modify response
        public async Task OnResponse(object sender, SessionEventArgs e)
        {
            // read response headers
            var responseHeaders = e.HttpClient.Response.Headers;

            //if (!e.ProxySession.Request.Host.Equals("medeczane.sgk.gov.tr")) return;
            if (e.HttpClient.Request.Method == "GET" || e.HttpClient.Request.Method == "POST")
            {
                if ((HttpStatusCode)e.HttpClient.Response.StatusCode == HttpStatusCode.OK)
                {
                    if (e.HttpClient.Response.ContentType != null && e.HttpClient.Response.ContentType.Trim().ToLower().Contains("text/html"))
                    {
                        byte[] bodyBytes = await e.GetResponseBody();
                        e.SetResponseBody(bodyBytes);

                        string body = await e.GetResponseBodyAsString();
                        e.SetResponseBodyString(body);
                    }
                }
            }

            if (e.UserData != null)
            {
                // access request from UserData property where we stored it in RequestHandler
                var request = (Request)e.UserData;
            }
        }

        // Allows overriding default certificate validation logic
        public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            // set IsValid to true/false based on Certificate Errors
            if (e.SslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                e.IsValid = true;

            return Task.CompletedTask;
        }

        // Allows overriding default client certificate selection logic during mutual authentication
        public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
        {
            // set e.clientCertificate to override
            return Task.CompletedTask;
        }
    }
}
