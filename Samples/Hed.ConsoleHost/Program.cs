using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Hed.ConsoleHost.Logging;
using Hed.Server.Server;
using Microsoft.Owin.Hosting;

namespace Hed.ConsoleHost
{
	class Program
	{
		static void Main(string[] args)
		{
            Trace.Listeners.Add(new ConsoleLogger());
            var endPoint = new IPEndPoint(IPAddress.Loopback, 9090);
            var hedProxyHandler = HedProxyHandler.Instance;
            var server = new HedServer(endPoint, hedProxyHandler);
            server.Start();

            //using (var simpleProxy = new SimpleProxy(9090))
            using (WebApp.Start<Startup>("http://localhost:9091/"))
			{
				Console.ReadLine();
			}

		}
	}
}

