using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Hed.Server.Connection;
using Hed.Server.Handlers;

namespace Hed.Server.Server
{
    public class SecureHedServer : HedServer
    {
        private X509Certificate certificate;
        public SecureHedServer(IPEndPoint endPoint, IHedRequestHandler handler, X509Certificate certificate)
            : base(endPoint, handler)
        {
            this.certificate = certificate;
        }

        protected override Task<InboundConnection> CreateInboundConnection(TcpClient client)
        {
            return Task.FromResult<InboundConnection>(new SecureInboundConnection(client, this.certificate));
        }
    }
}
