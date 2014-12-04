using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using Microsoft.Web.WebSockets;
using Newtonsoft.Json;
using Raven.Client.Connection;

namespace Hed.ConsoleHost.Controllers
{
    public class TopologyController : ApiController
    {
        [HttpGet]
        [Route("topology/view")]
        public object Get()
        {
            return HedConfiguration.Instance.Topology;
        }

        [HttpGet]
        [Route("topology/addEndpoints")]
        public object AddEndpoints(string url)
        {
            try
            {
                var res = DocumentStoreFactory.GetDocumentStoreForUrl(url).DatabaseCommands.GetDatabaseNames(32, 0);
                HedConfiguration.Instance.AddEndpoints(res.Select(dbname => url + "/databases/" + dbname));
                return Redirect(new Uri("/topology/view", UriKind.Relative));
            }
            catch (Exception)
            {
                return Redirect(new Uri("/topology/view", UriKind.Relative));
            }
            
        }

        [HttpGet]
        [Route("topology/addEndpoint")]
        public object AddEndpoint(string url)
        {
            HedConfiguration.Instance.AddEndpoint(url);
            return Redirect(new Uri("/topology/view", UriKind.Relative));
        }

        [HttpGet]
        [Route("topology/removeEndpoint")]
        public object RemoveEndpoint(string url)
        {
            HedConfiguration.Instance.RemoveEndpoint(url);
            return Redirect(new Uri("/topology/view", UriKind.Relative));
        }
        [HttpGet]
        [Route("topology/set")]
        public object Set(string from, string to, string behavior)
        {
            if (string.IsNullOrEmpty(from)) throw new ArgumentNullException("from");
            if (string.IsNullOrEmpty(to)) throw new ArgumentNullException("to");
            ProxyBehavior parsedBehavior = string.IsNullOrEmpty(behavior)
                ? ProxyBehavior.Optimal
                : Enum.TryParse(behavior, out parsedBehavior) ? parsedBehavior : ProxyBehavior.Optimal;
            bool pathInTopology;
            HedConfiguration.Instance.Set(from, to, out pathInTopology, parsedBehavior);
            HedConfiguration.Instance.Flush();
            if (!pathInTopology) ReplicationProxySetup.Instance.TrySetReplication(from, to, parsedBehavior);
            return Redirect(new Uri("/topology/view", UriKind.Relative));
        }

        [HttpGet]
        [Route("topology/del")]
        public object Del(string from, string to)
        {
            var key = HedConfiguration.Instance.Delete(from, to);
            HedConfiguration.Instance.Flush();
            if (!key.Equals("-1"))
            {
                ReplicationProxySetup.Instance.TryRemoveReplication(key);
            }
            return Redirect(new Uri("/topology/view", UriKind.Relative));
        }

 

    }
}