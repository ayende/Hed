using System.Web.Http;
using Owin;

namespace Hed.ConsoleHost
{
	public class Startup
	{
		public void Configuration(IAppBuilder appBuilder)
		{
			var config = new HttpConfiguration();
			config.MapHttpAttributeRoutes();
			config.Formatters.Remove(config.Formatters.XmlFormatter);
			config.Formatters.JsonFormatter.SerializerSettings.Converters
				.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
			appBuilder.UseWebApi(config);
		}
	}
}