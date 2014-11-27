using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hed.ConsoleHost;
using Raven.Abstractions.Replication;
using Raven.Client.Connection;
using Raven.Client.Document;
using Raven.Imports.Newtonsoft.Json;
using Raven.Json.Linq;

namespace ReplicationProxySetup
{
    public class ReplicationProxySetup
    {
        public ReplicationProxySetup(String[] urls, String redirectPrefix)
        {
            databasesUrls = urls;
            RedirectPrefix = redirectPrefix;
        }

        public void Run()
        {
            //todo: change to parallel for
            foreach (var url in databasesUrls)
            {
                FetchPathsAndRedirectReplication(url);
            }
            var topoPath = CreateTopologyFile();
            Hed.ConsoleHost.Program.Main(new[]{topoPath});
        }

        private void FetchPathsAndRedirectReplication(string replicationServerUrl)
        {
            using (var documentStore = DocumentStoreFactory.GetDocumentStoreForUrl(replicationServerUrl))
            {
                var replicationDocJson = documentStore.DatabaseCommands.Get(ReplicationDocKey);
                if (null == replicationDocJson) throw new ArgumentException(String.Format("The given server url={0} does not support redirection",replicationServerUrl));
                var replicationDoc = replicationDocJson.ToJson().Deserialize<ReplicationDocument>(new DocumentConvention());
                if (null == replicationDoc) throw new JsonSerializationException("Failed to deserialize replication document.");
                var destinations = replicationDoc.Destinations.Select(des =>
                {
                    // fixing url so we use right key and won't re-add prefix 
                    if (des.Url.StartsWith(RedirectPrefix))
                    {
                        var keyLength = des.Url.Substring(RedirectPrefix.Length).Split(new []{'/'},StringSplitOptions.RemoveEmptyEntries).First().Length;
                        des.Url = des.Url.Substring(RedirectPrefix.Length + 2 + keyLength);
                    }
                    paths[currentPathKey.ToString()] = new ProxyPath { Behavior = ProxyBehavior.Optimal, From = replicationServerUrl, To = new Uri(des.Url +"/"+ des.Database) };
                    des.Url = string.Format("{0}/{1}/{2}", RedirectPrefix, currentPathKey++, des.Url);
                    return des;
                }
                    ).ToList();
                var doc = RavenJObject.FromObject(replicationDoc);
                doc.Remove("Id");
                documentStore.DatabaseCommands.Put(ReplicationDocKey, null, doc, new RavenJObject());
            }
        }

        private string CreateTopologyFile(String topologyFileDir = "")
        {
            var topologyPath = Path.Combine(topologyFileDir, "topology.json");
            var fi = new FileInfo(topologyPath);
            var topo = new ProxyTopology();
            topo.Paths = paths;
            var topoJson = JsonConvert.SerializeObject(topo);
            var topoWriter = fi.CreateText();
            topoWriter.Write(topoJson);
            topoWriter.Close();
            return fi.FullName;
        }
        private const string ReplicationDocKey = "Raven/Replication/Destinations";
        private String[] databasesUrls ;
        private Dictionary<String, ProxyPath> paths = new Dictionary<string, ProxyPath>(StringComparer.OrdinalIgnoreCase);
        private int currentPathKey = 0;
        private readonly string RedirectPrefix;
    }
}
