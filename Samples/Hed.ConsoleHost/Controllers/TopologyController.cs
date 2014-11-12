using System;
using System.Globalization;
using System.Web.Http;

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
		[Route("topology/add")]
		public object Add(string from, string to)
		{
			if (string.IsNullOrEmpty(from)) throw new ArgumentNullException("from");
			if (string.IsNullOrEmpty(to)) throw new ArgumentNullException("to");

			HedConfiguration.Instance.Add(from, to);
			HedConfiguration.Instance.Flush();

			return Redirect(new Uri("/topology/view", UriKind.Relative));
		}

		[HttpGet]
		[Route("topology/del")]
		public object Del(int id)
		{
			HedConfiguration.Instance.Delete(id.ToString(CultureInfo.InvariantCulture));
			HedConfiguration.Instance.Flush();
			return Redirect(new Uri("/topology/view", UriKind.Relative));
		}

		[HttpGet]
		[Route("topology/set")]
		public object Set(int id, string behavior)
		{
			HedConfiguration.Instance.Set(id.ToString(CultureInfo.InvariantCulture),
				(ProxyBehavior) Enum.Parse(typeof (ProxyBehavior), behavior));
			HedConfiguration.Instance.Flush();
			return Redirect(new Uri("/topology/view", UriKind.Relative));
		}
	}
}