// -----------------------------------------------------------------------
//  <copyright file="HedProxyHandler.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hed.ConsoleHost.Common;
using Hed.Server.Context;
using Hed.Server.Handlers;
using Hed.Server.Request;
using Hed.Server.Response;
using Hed.Server.Server;
using Newtonsoft.Json;

namespace Hed.ConsoleHost
{
	public class HedProxyHandler : IHedRequestHandler
	{
        public static HedProxyHandler Instance
        {
            get; private set;
        }

	    static HedProxyHandler()
	    {
	        Instance = new HedProxyHandler();
	    }

	    private HedProxyHandler()
	    {
            Requests = new ConcurrentDictionary<string, ConcurrentDictionary<string, Ref>>();
	    }
        public ConcurrentDictionary<String, ConcurrentDictionary<string,Ref>> Requests { get; private set; }

	    private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());
		private readonly Regex _pathMatch = new Regex(@"^/(\d+)(/)(.+)");

		public async Task<HedResponse> GetResponseAsync(HedContext context, HedRequest request)
		{
			var match = _pathMatch.Match(request.RequestUri);
			if (match.Success == false)
			{
				return new HedResponse
				{
					StatusCode = 500,
					StatusDescription = "InternalServerError",
					ResponseBody =
						new MemoryStream(Encoding.UTF8.GetBytes("Cannot understand request without a proper topology path id"))
				};
			}
			ProxyPath path;
			if (HedConfiguration.Instance.TryGetPath(match.Groups[1].Value, out path) == false)
			{
				return new HedResponse
				{
					StatusCode = 500,
					StatusDescription = "InternalServerError",
					ResponseBody =
						new MemoryStream(Encoding.UTF8.GetBytes("Topology id " + match.Groups[1].Value + " does not exists"))
				};
			}



            var urlToDest = path.To.ToString() +"/"+ match.Groups[3].Value;
			//request.RequestUri = path.To.AbsolutePath + urlToDest;
		    request.RequestUri = urlToDest;
		    HedResponse response;
		    switch (path.Behavior)
		    {
		        case ProxyBehavior.Optimal:
		            response = await OptimalResponse(context, request, path);
		            break;
		        case ProxyBehavior.Slow:
                    response = await SlowResponse(context, request, path);
                    break;
		        case ProxyBehavior.Normal:
                    response = await NormalResponse(context, request, path);
                    break;
		        case ProxyBehavior.Hiccups:
                    response = await HiccupResponse(context, request, path);
                    break;
		        case ProxyBehavior.Dropping:
                    response = await DroppingResponse(context, request, path);
                    break;
		        case ProxyBehavior.Down:
                    response = await DownResponse(context, request, path);
                    break;
		        default:
		            throw new ArgumentOutOfRangeException();
		    }
		    var pathKey = string.Format("{0}=>{1}",path.From,path.To);
            if (!Requests.ContainsKey(pathKey)) Requests.AddOrUpdate(pathKey, path.Operations, (key, val) => val);		                
		    return response;
		}

	    private async Task<HedResponse> DownResponse(HedContext context, HedRequest request, ProxyPath path)
	    {
            var behavior = _random.Value.Next(1, 101);
	        if (behavior <= 50)
	        {
                path.Operation("Down");
	            DropConnection();
	        }
	        if (behavior <= 80)
	        {
                path.Operation("CloseTcp");
	            CloseTcpClient(context);
	        }
	        return new HedResponse
            {
                StatusCode = 503,
                StatusDescription = "ServiceUnavailable",
                ResponseBody =
                    new MemoryStream(Encoding.UTF8.GetBytes(string.Format("The requested url :{0} seems to be unavailable.",path)))
            };
	    }

	    private static void DropConnection()
	    {
	        throw new HedServer.AbandonConnectionException();
	    }

	    private static void CloseTcpClient(HedContext context)
	    {
	        context.Dispose();
	        throw new HedServer.AbandonConnectionException();
	    }

	    private async Task<HedResponse> NormalResponse(HedContext context, HedRequest request, ProxyPath path)
        {
            var behavior = _random.Value.Next(1, 101);
            if (behavior <= 95)
            {
                return await OptimalResponse(context, request, path);
            }
	        if (behavior <= 98)
	        {
	            return await SlowResponse(context, request, path);
	        }
	        if (behavior == 99)
	        {
	            return await HiccupResponse(context, request, path);
	        }
	        return await DroppingResponse(context, request, path);

        }
        // 30% chance dropped connection, 30% chance 503 error, 30% chance close TCP, 10% normal
        private async Task<HedResponse> DroppingResponse(HedContext context, HedRequest request, ProxyPath path)
	    {
	        var behavior = _random.Value.Next(1, 101);
            if (behavior <= 60)
            {
                path.Operation("503");
                return new HedResponse
                {
                    StatusCode = 503,
                    StatusDescription = "ServiceUnavailable",
                    ResponseBody =
                        new MemoryStream(Encoding.UTF8.GetBytes(string.Format("The requested url :{0} seems to be unavailable.", path)))
                };
            }
            if (behavior <= 90)
            {
                path.Operation("CloseTcp");
                CloseTcpClient(context);
            }
            return await NormalResponse(context, request, path);
	    }

        private async Task<HedResponse> HiccupResponse(HedContext context, HedRequest request, ProxyPath path)
	    {
            var behavior = _random.Value.Next(1, 101);
            if (behavior <= 15)
            {
                path.Operation("Drop");
                DropConnection();
            }
            else if (behavior <= 30)
            {
                return await SlowResponse(context, request, path);
            }
            else if (behavior <= 45)
            {
                path.Operation("Repeated");
                var response = await OptimalResponse(context, request, path,false);
                return await OptimalResponse(context, request, path,false);
            } else if (behavior <= 60)
            {
                path.Operation("HalfSend");
                await CutRequestBodyByHalf(request);
                return await OptimalResponse(context, request, path,false);
            }
            return await NormalResponse(context, request, path);
	    }

	    private async Task CutRequestBodyByHalf(HedRequest request, int bufferSize = 4096)
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
	    private async Task<HedResponse> SlowResponse(HedContext context, HedRequest request, ProxyPath path)
	    {
            path.Operation("Slow");
	        var wait = _random.Value.Next(500, 5000);
	        Debug.WriteLine("Waiting for {0:#,#;;0}ms under slow behavior", wait);
	        await Task.Delay(wait/2);
	        var result = await OptimalResponse(context, request, path);
	        await Task.Delay(wait/2);
	        return result;
	    }

	    private static async Task<HedResponse> OptimalResponse(HedContext context, HedRequest request, ProxyPath path,bool setOpertation = true)
		{
			await OpenOutboundConnection(context, path);
			await context.OutboundConnection.WriteRequestAsync(request);
			var response = await context.OutboundConnection.ReadResponseAsync();
            if(setOpertation) path.Operation("Optimal");
			return response;
		}

		private static async Task OpenOutboundConnection(HedContext context, ProxyPath path)
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