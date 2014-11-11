// -----------------------------------------------------------------------
//  <copyright file="HedProxyHandler.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Hed.ConsoleHost
{
	public class HedProxyHandler : ISwitchboardRequestHandler
	{
		private readonly string config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "topology.json");
		private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());
		private readonly Regex _pathMatch = new Regex(@"^/(\d+)(/.+)");

		private readonly ProxyTopology _topology = new ProxyTopology
		{
			Paths =
			{
				{"1", new ProxyPath
				{
					From = "http://localhost:8080/databases/abc",
					To = new Uri("http://localhost:8080/databases/def"),
					Behavior = ProxyBehavior.Slow
				}}
			}
		};

		public HedProxyHandler()
		{
			if (File.Exists(config))
			{
				_topology = JsonConvert.DeserializeObject<ProxyTopology>(File.ReadAllText(config));
			}
		}


		public async Task<SwitchboardResponse> GetResponseAsync(SwitchboardContext context, SwitchboardRequest request)
		{
			var match = _pathMatch.Match(request.RequestUri);
			if (match.Success == false)
			{
				return new SwitchboardResponse
				{
					StatusCode = 500,
					StatusDescription = "InternalServerError",
					ResponseBody =
						new MemoryStream(Encoding.UTF8.GetBytes("Cannot understand request without a proper topology path id"))
				};
			}
			ProxyPath path;
			if (_topology.Paths.TryGetValue(match.Groups[1].Value, out path) == false)
			{
				return new SwitchboardResponse
				{
					StatusCode = 500,
					StatusDescription = "InternalServerError",
					ResponseBody =
						new MemoryStream(Encoding.UTF8.GetBytes("Topology id " + match.Groups[1].Value + " does not exists"))
				};
			}



			var urlToDest = match.Groups[2].Value;
			request.RequestUri = path.To.AbsolutePath + urlToDest;
			switch (path.Behavior)
			{
				case ProxyBehavior.Optimal:
					return await OptimalResponse(context, request, path);
				case ProxyBehavior.Slow:
					var wait = _random.Value.Next(500, 5000);
					Debug.WriteLine("Waiting for {0:#,#;;0}ms under slow behavior", wait);
					await Task.Delay(wait/2);
					var result = await OptimalResponse(context, request, path);
					await Task.Delay(wait / 2);
					return result;
				case ProxyBehavior.Normal:
				case ProxyBehavior.Hiccups:
				case ProxyBehavior.Dropping:
				case ProxyBehavior.Down:
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static async Task<SwitchboardResponse> OptimalResponse(SwitchboardContext context, SwitchboardRequest request, ProxyPath path)
		{
			await OpenOutboundConnection(context, path);
			await context.OutboundConnection.WriteRequestAsync(request);
			var response = await context.OutboundConnection.ReadResponseAsync();
			return response;
		}

		private static async Task OpenOutboundConnection(SwitchboardContext context, ProxyPath path)
		{
			IPAddress ip;
			if (path.To.HostNameType == UriHostNameType.IPv4)
			{
				ip = IPAddress.Parse(path.To.Host);
			}
			else
			{
				var ipAddresses = await Dns.GetHostAddressesAsync(path.To.Host);
				ip = ipAddresses.First(x => x.AddressFamily == AddressFamily.InterNetwork);

			}

			var backendEp = new IPEndPoint(ip, path.To.Port);

			if (path.To.Scheme != "https")
				await context.OpenOutboundConnectionAsync(backendEp);
			else
				await context.OpenSecureOutboundConnectionAsync(backendEp, path.To.Host);
		}
	}
}