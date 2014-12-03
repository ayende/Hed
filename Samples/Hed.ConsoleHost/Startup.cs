using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;
using Newtonsoft.Json;
using Owin;

namespace Hed.ConsoleHost
{
    using WebSocketAccept = Action<IDictionary<string, object>, // options
        Func<IDictionary<string, object>, Task>>; // callback
    using WebSocketCloseAsync =
        Func<int /* closeStatus */,
            string /* closeDescription */,
            CancellationToken /* cancel */,
            Task>;
    using WebSocketReceiveAsync =
        Func<ArraySegment<byte> /* data */,
            CancellationToken /* cancel */,
            Task<Tuple<int /* messageType */,
                bool /* endOfMessage */,
                int /* count */>>>;
    using WebSocketSendAsync =
        Func<ArraySegment<byte> /* data */,
            int /* messageType */,
            bool /* endOfMessage */,
            CancellationToken /* cancel */,
            Task>;
    using WebSocketReceiveResult = Tuple<int, // type
        bool, // end of message?
        int>; // count
	public class Startup
	{
		public void Configuration(IAppBuilder appBuilder)
		{
			var config = new HttpConfiguration();
			config.MapHttpAttributeRoutes();
			config.Formatters.Remove(config.Formatters.XmlFormatter);
			config.Formatters.JsonFormatter.SerializerSettings.Converters
				.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
			appBuilder.UseWebApi(config);
		    appBuilder.Use(UpgradeToWebSockets);           
		}
        private Task UpgradeToWebSockets(IOwinContext context, Func<Task> next)
        {
            WebSocketAccept accept = context.Get<WebSocketAccept>("websocket.Accept");
            if (accept == null)
            {
                // Not a websocket request
                return next();
            }

            accept(null, WebSocketSendRequestsStats);

            return Task.FromResult<object>(null);
        }

        private async Task WebSocketSendRequestsStats(IDictionary<string, object> websocketContext)
        {
            var sendAsync = (WebSocketSendAsync)websocketContext["websocket.SendAsync"];
            var closeAsync = (WebSocketCloseAsync)websocketContext["websocket.CloseAsync"];
            var callCancelled = (CancellationToken)websocketContext["websocket.CallCancelled"];

            byte[] buffer = new byte[1024];

            CreateWaitForClientCloseTask(websocketContext, callCancelled);
            object status;
            while ((!websocketContext.TryGetValue("websocket.ClientCloseStatus", out status) || (int)status == 0) && callCancelled.IsCancellationRequested == false)
            { 
                await Task.Delay(3000);
                var requestJson = JsonConvert.SerializeObject(HedProxyHandler.Instance.Requests);
                var requestAsArray = System.Text.Encoding.UTF8.GetBytes(requestJson);
                await sendAsync(new ArraySegment<byte>(requestAsArray, 0, requestAsArray.Length), 1, true, callCancelled);
                callCancelled = (CancellationToken)websocketContext["websocket.CallCancelled"];
            }

            await closeAsync((int)websocketContext["websocket.ClientCloseStatus"], (string)websocketContext["websocket.ClientCloseDescription"], callCancelled);
        }

        private void CreateWaitForClientCloseTask(IDictionary<string, object> websocketContext, CancellationToken callCancelled)
        {
            new Task(async () =>
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);
                var receiveAsync = (WebSocketReceiveAsync)websocketContext["websocket.ReceiveAsync"];
                var closeAsync = (WebSocketCloseAsync)websocketContext["websocket.CloseAsync"];

                while (callCancelled.IsCancellationRequested == false)
                {
                    try
                    {
                        WebSocketReceiveResult receiveResult = await receiveAsync(buffer, callCancelled);

                        if (receiveResult.Item1 == WebSocketCloseMessageType)
                        {
                            var clientCloseStatus = (int)websocketContext["websocket.ClientCloseStatus"];
                            var clientCloseDescription = (string)websocketContext["websocket.ClientCloseDescription"];

                            if (clientCloseStatus == NormalClosureCode || clientCloseStatus == NormalGoingAwayClosureCode)
                            {
                                await closeAsync(clientCloseStatus, clientCloseDescription, callCancelled);
                            }

                            //At this point the WebSocket is in a 'CloseReceived' state, so there is no need to continue waiting for messages
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        return;
                    }
                }

            }).Start();
        }
        private const int NormalClosureCode = 1000;
	    private const int NormalGoingAwayClosureCode = 1001;
        private const int WebSocketCloseMessageType = 8;
        private const string NormalClosureMessage = "CLOSE_NORMAL";
	}
}