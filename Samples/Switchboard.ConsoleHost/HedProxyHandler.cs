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
using Switchboard.Server;

namespace Switchboard.ConsoleHost
{
	public class HedProxyHandler : ISwitchboardRequestHandler
	{
		private readonly string config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "topology.json");
		private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());
		private readonly Regex _pathMatch = new Regex(@"^/(\d+)(/)(.+)");

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



			var urlToDest = match.Groups[3].Value;
			//request.RequestUri = path.To.AbsolutePath + urlToDest;
		    request.RequestUri = urlToDest;
			switch (path.Behavior)
			{
				case ProxyBehavior.Optimal:
					return await OptimalResponse(context, request, path);
				case ProxyBehavior.Slow:
			        return await SlowResponse(context, request, path);
			    case ProxyBehavior.Normal:
			        return await NormalResponse(context, request, path);
				case ProxyBehavior.Hiccups:
                    return await HiccupResponse(context, request, path);
				case ProxyBehavior.Dropping:
                    return await DroppingResponse(context, request, path);
				case ProxyBehavior.Down:
                    return await DownResponse(context, request, path);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

	    private async Task<SwitchboardResponse> DownResponse(SwitchboardContext context, SwitchboardRequest request, ProxyPath path)
	    {
            var behavior = _random.Value.Next(1, 101);
	        if (behavior <= 50)
	        {
	            DropConnection();
	        }
	        if (behavior <= 80)
	        {
	            CloseTcpClient(context);
	        }
	        return new SwitchboardResponse
            {
                StatusCode = 503,
                StatusDescription = "ServiceUnavailable",
                ResponseBody =
                    new MemoryStream(Encoding.UTF8.GetBytes(string.Format("The requested url :{0} seems to be unavailable.",path)))
            };
	    }

	    private static void DropConnection()
	    {
	        throw new SwitchboardServer.AbandonConnectionException();
	    }

	    private static void CloseTcpClient(SwitchboardContext context)
	    {
	        context.Dispose();
	        throw new SwitchboardServer.AbandonConnectionException();
	    }

	    private async Task<SwitchboardResponse> NormalResponse(SwitchboardContext context, SwitchboardRequest request, ProxyPath path)
        {
            var behavior = _random.Value.Next(1, 101);
            if (behavior <= 95)
            {
                var result = await OptimalResponse(context, request, path);
            }
            else if (behavior <= 98)
            {
                return await SlowResponse(context, request, path);
            }
            else if (behavior == 99)
            {
                return await HiccupResponse(context, request, path);
            }
            return await DroppingResponse(context, request, path);

        }
        // 30% chance dropped connection, 30% chance 503 error, 30% chance close TCP, 10% normal
        private async Task<SwitchboardResponse> DroppingResponse(SwitchboardContext context, SwitchboardRequest request, ProxyPath path)
	    {
	        var behavior = _random.Value.Next(1, 101);
            if (behavior <= 30)
            {
            }
            else if (behavior <= 60)
            {
                return new SwitchboardResponse
                {
                    StatusCode = 503,
                    StatusDescription = "ServiceUnavailable",
                    ResponseBody =
                        new MemoryStream(Encoding.UTF8.GetBytes(string.Format("The requested url :{0} seems to be unavailable.", path)))
                };
            }
            else if (behavior <= 90)
            {
                CloseTcpClient(context);
            }
            return await NormalResponse(context, request, path);
	    }

        private async Task<SwitchboardResponse> HiccupResponse(SwitchboardContext context, SwitchboardRequest request, ProxyPath path)
	    {
            var behavior = _random.Value.Next(1, 101);
            if (behavior <= 15)
            {
                DropConnection();
            }
            else if (behavior <= 30)
            {
                return await SlowResponse(context, request, path);
            }
            else if (behavior <= 45)
            {
                var response = await OptimalResponse(context, request, path);
                return await OptimalResponse(context, request, path);
            } else if (behavior <= 60)
            {
                await CutRequestBodyByHalf(request);
                return await OptimalResponse(context, request, path);
            }
            return await NormalResponse(context, request, path);
	    }

	    private async Task CutRequestBodyByHalf(SwitchboardRequest request, int bufferSize = 4096)
	    {
	        if (null == request.RequestBody) return;
            byte[] buffer = new byte[bufferSize];
	        var requestSize = request.RequestBody.Length/2;
	        var stream = new MemoryStream();
	        int count;
	        long length = 0;
            while ((count = await request.RequestBody.ReadAsync(buffer, 0, bufferSize)) != 0 || length < requestSize)
            {
                var bytesToWrite = Math.Min(bufferSize, requestSize - length);
                await stream.WriteAsync(buffer, 0, (int)bytesToWrite);
                length += bytesToWrite;
            }
	        request.RequestBody = stream;
	    }
	    private async Task<SwitchboardResponse> SlowResponse(SwitchboardContext context, SwitchboardRequest request, ProxyPath path)
	    {
	        var wait = _random.Value.Next(500, 5000);
	        Debug.WriteLine("Waiting for {0:#,#;;0}ms under slow behavior", wait);
	        await Task.Delay(wait/2);
	        var result = await OptimalResponse(context, request, path);
	        await Task.Delay(wait/2);
	        return result;
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