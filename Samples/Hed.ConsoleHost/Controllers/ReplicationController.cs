using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Hed.ConsoleHost.Controllers
{
    public class ReplicationController : ApiController
    {
        [HttpGet]
        [Route("replication/set")]
        public object Set(string dbFrom, string dbTo)
        {
            ReplicationProxySetup.Instance.SetRelationship(dbFrom, dbTo);
            return Redirect(new Uri("/topology/view", UriKind.Relative));
        }
        
    }
}
