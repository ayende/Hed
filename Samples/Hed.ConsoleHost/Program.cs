using System;
using System.Diagnostics;
using System.Net;
using Hed.ConsoleHost.Logging;
using Switchboard.Server;

namespace Hed.ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            // Dump all debug data to the console, coloring it if possible
            Trace.Listeners.Add(new ConsoleLogger());

            var endPoint = new IPEndPoint(IPAddress.Loopback, 9090);
			//var handler = new SimpleReverseProxyHandler("http://www.nytimes.com");
            var server = new SwitchboardServer(endPoint, new HedProxyHandler());

            server.Start();

            Console.WriteLine("Point your browser at http://{0}", endPoint);

            Console.ReadLine();
        }
    }


}
