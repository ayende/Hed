using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hed.ConsoleHost.Common
{
    public class RequestSummary
    {
        public ConcurrentQueue<ProxyPath> requests = new ConcurrentQueue<ProxyPath>();
    }
}
