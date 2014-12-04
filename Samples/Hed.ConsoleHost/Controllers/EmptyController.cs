using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Hed.ConsoleHost.Controllers
{
    [RoutePrefix("")]
    public class EmptyController : ApiController
    {
        [HttpGet]
        [Route("")]
        public object Get()
        {
            return Redirect(new Uri("/studio/index.html", UriKind.Relative));
        }
        
    }
}
