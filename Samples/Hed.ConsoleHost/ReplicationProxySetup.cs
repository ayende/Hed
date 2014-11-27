using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Hed.ConsoleHost;
using Raven.Abstractions.Replication;
using Raven.Client.Connection;
using Raven.Client.Document;
using Raven.Imports.Newtonsoft.Json;
using Raven.Json.Linq;

namespace Hed.ConsoleHost
{
    public class ReplicationProxySetup
    {
        public static ReplicationProxySetup Instance { get; private set; }

        static ReplicationProxySetup()
        {
            Instance = new ReplicationProxySetup();
        }
        private ReplicationProxySetup()
        {
        }

        private void FetchPathsAndRedirectReplication(String fromHost, String fromDb, String toHost, String toDb)
        {
            using (var documentStore = DocumentStoreFactory.GetDocumentStoreForUrl(fromHost, fromDb))
            {
                var fulFromUrl = String.Format("{0}/databases/{1}", fromHost, fromDb);
                var fulToUrl = String.Format("{0}/databases/{1}", toHost, toDb);

                using (var session = documentStore.OpenSession())
                {
                    var replicateDoc = session.Load<ReplicationDocument>(ReplicationDocKey);
                    if (null == replicateDoc) replicateDoc = new ReplicationDocument();
                    var destination = new ReplicationDestination();
                    replicateDoc.Destinations.Add(destination);
                    destination.TransitiveReplicationBehavior = TransitiveReplicationOptions.Replicate;
                    HedConfiguration.Instance.Topology.Paths[currentPathKey.ToString()] = new ProxyPath { Behavior = ProxyBehavior.Optimal, From = fulFromUrl, To = new Uri(fulToUrl) };
                    destination.Url = string.Format("{0}/{1}", RedirectPrefix, currentPathKey++);
                    //destination.Database = toDb;
                    var doc = RavenJObject.FromObject(replicateDoc);
                    doc.Remove("Id");
                    documentStore.DatabaseCommands.Put(ReplicationDocKey, null, doc, new RavenJObject());
                }
            }
        }
        public void SetRelationship(string dbFrom, string dbTo)
        {
            var dbFromSplit = dbFrom.Split(new[] { "/databases/" }, StringSplitOptions.RemoveEmptyEntries);
            var dbToSplit = dbTo.Split(new[] { "/databases/" }, StringSplitOptions.RemoveEmptyEntries);
            var dbFromHost = dbFromSplit[0];
            var dbFromDb = dbFromSplit[1];
            var dbToHost = dbToSplit[0];
            var dbToDb = dbToSplit[1];
            FetchPathsAndRedirectReplication(dbFromHost, dbFromDb, dbToHost, dbToDb);
            FetchPathsAndRedirectReplication(dbToHost, dbToDb, dbFromHost, dbFromDb);
        }
        private const string ReplicationDocKey = "Raven/Replication/Destinations";
        private String[] databasesUrls;
        private Dictionary<String, ProxyPath> paths = new Dictionary<string, ProxyPath>(StringComparer.OrdinalIgnoreCase);
        private int currentPathKey = HedConfiguration.Instance.Topology.Paths.Count+1;
        private readonly string RedirectPrefix = "http://localhost.:9090";


    }
}
