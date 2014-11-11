using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Hed.Server.Connection;
using Hed.Server.Context;
using Hed.Server.Handlers;

namespace Hed.Server.Server
{
    public class HedServer
    {
        private IHedRequestHandler handler;
        private TcpListener server;
        private Task workTask;
        private bool stopping;
        private Timer connectivityTimer;

        public HedServer(IPEndPoint listenEp, IHedRequestHandler handler)
        {
            this.server = new TcpListener(listenEp);
            this.handler = handler;
        }

        public void Start()
        {
            this.server.Start();
            this.workTask = Run(CancellationToken.None);
        }

        private async Task Run(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await server.AcceptTcpClientAsync();

                var inbound = await CreateInboundConnection(client);
                await inbound.OpenAsync(ct);

                Debug.WriteLine("{0}: Connected", inbound.RemoteEndPoint);

                var context = new HedContext(inbound);

                var ignored = HandleSession(context);
            }
        }

        protected virtual Task<InboundConnection> CreateInboundConnection(TcpClient client)
        {
            return Task.FromResult<InboundConnection>(new InboundConnection(client));
        }

        public class AbandonConnectionException : Exception
        {
        }

        private async Task HandleSession(HedContext context)
        {
            bool abandon = false;
            try
            {
                Debug.WriteLine("{0}: Starting session", context.InboundConnection.RemoteEndPoint);

                do
                {
                    var request = await context.InboundConnection.ReadRequestAsync().ConfigureAwait(false);

                    if (request == null)
                        return;

                    Debug.WriteLine("{0}: Got {1} request for {2}", context.InboundConnection.RemoteEndPoint,
                        request.Method, request.RequestUri);

                    var response = await handler.GetResponseAsync(context, request).ConfigureAwait(false);
                    Debug.WriteLine("{0}: Got response from handler ({1})", context.InboundConnection.RemoteEndPoint,
                        response.StatusCode);

                    await context.InboundConnection.WriteResponseAsync(response).ConfigureAwait(false);
                    Debug.WriteLine("{0}: Wrote response to client", context.InboundConnection.RemoteEndPoint);

                    if (context.OutboundConnection != null && !context.OutboundConnection.IsConnected)
                        context.Close();

                } while (context.InboundConnection.IsConnected);
            }
            catch (AbandonConnectionException)
            {
                Debug.WriteLine("{0}: Connection abandoned", context.InboundConnection.RemoteEndPoint);
                abandon = true;
            }
            catch (Exception exc)
            {
                Debug.WriteLine("{0}: Error: {1}", context.InboundConnection.RemoteEndPoint, exc.Message);
                context.Close();
                Debug.WriteLine("{0}: Closed context", context.InboundConnection.RemoteEndPoint, exc.Message);
            }
            finally
            {
                if (abandon == false)
                    context.Dispose();
            }
        }

        private Task<TcpClient> AcceptOneClient()
        {
            throw new NotImplementedException();
        }
    }
}
