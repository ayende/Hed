using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Client.Document;

namespace ReplicationProxySetup
{
    public class DocumentStoreFactory
    {
        public static IDocumentStore GetDocumentStoreForUrl(string url)
        {
            var dbname = url.Split('/').Last();
            var realUrl = url.Substring(0, url.Length - dbname.Length - 1);
            if (!documentStores.ContainsKey(url)) documentStores[url] = new DocumentStore { Url = realUrl, DefaultDatabase = dbname };
            documentStores[url].Initialize();
            return documentStores[url];
        }

        private static Dictionary<string,IDocumentStore> documentStores = new Dictionary<string, IDocumentStore>();
    }
}
