using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Hed.ConsoleHost;
using Raven.Abstractions.Replication;
using Raven.Client;
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

        private void FetchPathsAndRedirectReplication(String fromHost, String fromDb, String toHost, String toDb, ProxyBehavior parsedBehavior)
        {
            using (var documentStore = DocumentStoreFactory.GetDocumentStoreForUrl(fromHost, fromDb))
            {                
                var fullFromUrl = String.Format("{0}/databases/{1}", fromHost, fromDb);
                var fullToUrl = String.Format("{0}/databases/{1}", toHost, toDb);

                using (var session = documentStore.OpenSession())
                {
                    var replicateDoc = session.Load<ReplicationDocument>(ReplicationDocKey);
                    if (null == replicateDoc) replicateDoc = new ReplicationDocument();
                    var destination = new ReplicationDestination();
                    replicateDoc.Destinations.Add(destination);
                    destination.TransitiveReplicationBehavior = TransitiveReplicationOptions.Replicate;
                    bool pathInTopology;
                    var key = HedConfiguration.Instance.Set(fullFromUrl, fullToUrl, out pathInTopology, parsedBehavior);
                    destination.Url = string.Format("{0}/{1}", RedirectPrefix, key);
                    //destination.Database = toDb;
                    var doc = RavenJObject.FromObject(replicateDoc);
                    doc.Remove("Id");
                    documentStore.DatabaseCommands.Put(ReplicationDocKey, null, doc, new RavenJObject());
                }
            }
        }
        private void RemoveReplication(String fromHost, String fromDb, string key)
        {
            using (var documentStore = DocumentStoreFactory.GetDocumentStoreForUrl(fromHost, fromDb))
            {
                using (var session = documentStore.OpenSession())
                {
                    var replicateDoc = session.Load<ReplicationDocument>(ReplicationDocKey);
                    if (null == replicateDoc) return;
                    var destination = (from dest in replicateDoc.Destinations
                        where dest.Url.Equals(string.Format("{0}/{1}", RedirectPrefix, key))
                        select dest).FirstOrDefault();
                    if (destination == null) return;
                    replicateDoc.Destinations.Remove(destination);
                    var doc = RavenJObject.FromObject(replicateDoc);
                    doc.Remove("Id");
                    documentStore.DatabaseCommands.Put(ReplicationDocKey, null, doc, new RavenJObject());
                }
            }
        }
        public bool TrySetReplication(string dbFrom, string dbTo,ProxyBehavior parsedBehavior)
        {
            string dbFromHost, dbFromDb, dbToHost, dbToDb;
            SplitDatabasesPathFromTo(dbFrom, dbTo, out dbFromHost, out dbFromDb, out dbToHost, out dbToDb);

            if (CheckIfRealDatabase(dbFromHost, dbFromDb) && CheckIfRealDatabase(dbToHost, dbToDb))
            {
                FetchPathsAndRedirectReplication(dbFromHost, dbFromDb, dbToHost, dbToDb, parsedBehavior);
                return true;
            }
            return false;
        }
        public bool TryRemoveReplication(string key)
        {
            if (!HedConfiguration.Instance.Topology.Paths.ContainsKey(key)) return false;
            var dbFrom = HedConfiguration.Instance.Topology.Paths[key].From;
            var splitedFrom = SplitDatabasePathToHostAndName(dbFrom);
            var dbFromHost = splitedFrom.Item1;
            var dbFromDb = splitedFrom.Item2;
            if (CheckIfRealDatabase(dbFromHost, dbFromDb))
            {
                RemoveReplication(dbFromHost, dbFromDb, key);
                return true;
            }
            return false;
        }

        private static bool CheckIfRealDatabase(string host, string database)
        {
            using (var documentStore = DocumentStoreFactory.GetDocumentStoreForUrl(host))
            {
                try
                {
                    var headers = documentStore.DatabaseCommands.Head("Raven/Databases/" + database);
                    return headers != null;
                }
                catch (Exception e)
                {
                    return false;
                }
                return true;
            }
        }

        public void SetRelationship(string dbFrom, string dbTo, ProxyBehavior parsedBehavior)
        {            
            string dbFromHost, dbFromDb, dbToHost, dbToDb;
            SplitDatabasesPathFromTo(dbFrom, dbTo, out dbFromHost, out dbFromDb, out dbToHost, out dbToDb);
            FetchPathsAndRedirectReplication(dbFromHost, dbFromDb, dbToHost, dbToDb, parsedBehavior);
            FetchPathsAndRedirectReplication(dbToHost, dbToDb, dbFromHost, dbFromDb, parsedBehavior);
        }

        private void SplitDatabasesPathFromTo(string dbFrom, string dbTo, out string dbFromHost, out string dbFromDb,
            out string dbToHost, out string dbToDb)
        {
            var dbFromSplit = SplitDatabasePathToHostAndName(dbFrom);
            var dbToSplit = SplitDatabasePathToHostAndName(dbTo);
            dbFromHost = dbFromSplit.Item1;
            dbFromDb = dbFromSplit.Item2;
            dbToHost = dbToSplit.Item1;
            dbToDb = dbToSplit.Item2;
        }
        private Tuple<String, String> SplitDatabasePathToHostAndName(String fullDatabasePath)
        {
            var dbPathSplit = fullDatabasePath.Split(new[] { databaseSeperator }, StringSplitOptions.RemoveEmptyEntries);
            return new Tuple<string, string>(dbPathSplit[0], dbPathSplit[1]);
        }

        private const String databaseSeperator = "/databases/";
        private const string ReplicationDocKey = "Raven/Replication/Destinations";
        private String[] databasesUrls;
        private Dictionary<String, ProxyPath> paths = new Dictionary<string, ProxyPath>(StringComparer.OrdinalIgnoreCase);       
        private readonly string RedirectPrefix = "http://localhost.fiddler:9090";


    }
}
