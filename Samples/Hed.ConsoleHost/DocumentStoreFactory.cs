using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Client;
using Raven.Client.Document;

namespace Hed.ConsoleHost
{
    public class DocumentStoreFactory
    {
        public static IDocumentStore GetDocumentStoreForUrl(string host, string database)
        {
                var documentStore = new DocumentStore { Url = host, DefaultDatabase = database };
                return documentStore.Initialize();

        }
    }
}
