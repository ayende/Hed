using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Hed.Server.Connection;
using Switchboard.Server.Connection;

namespace Switchboard.Server
{
    public class SecureSwitchboardServer : SwitchboardServer
    {
        private X509Certificate certificate;
        public SecureSwitchboardServer(IPEndPoint endPoint, IHedRequestHandler handler, X509Certificate certificate)
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
