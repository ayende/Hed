using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicationProxySetup
{
    class Program
    {
        static void Main(string[] args)
        {
            var replicationSetup = new ReplicationProxySetup(args, @"127.0.0.1:9090");
            replicationSetup.Run();
        }
    }
}
