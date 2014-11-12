using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Hed.ConsoleHost
{
	public class HedConfiguration
	{
		private readonly string config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "topology.json");
		private readonly ProxyTopology _topology;

		public static HedConfiguration Instance { get; private set; }
		public ProxyTopology Topology { get { return _topology; }}

		static HedConfiguration()
		{
			Instance = new HedConfiguration();
		}

		public HedConfiguration()
		{
			if (File.Exists(config))
			{
				_topology = JsonConvert.DeserializeObject<ProxyTopology>(File.ReadAllText(config));
			}
		}

		public bool TryGetPath(string value, out ProxyPath path)
		{
			return _topology.Paths.TryGetValue(value, out path);
		}

		public object Add(string src, string dest)
		{
			var key = "1";
			if (_topology.Paths.Count > 0)
				key = (_topology.Paths.Keys.Select(int.Parse).Max() + 1).ToString(CultureInfo.InvariantCulture);
			_topology.Paths.Add(key, new ProxyPath
			{
				Behavior = ProxyBehavior.Optimal,
				From = src,
				To = new Uri(dest)
			});

			return key;
		}

		public void Flush()
		{
			File.WriteAllText(config, JsonConvert.SerializeObject(_topology));
		}

		public void Delete(string id)
		{
			_topology.Paths.Remove(id);
		}

		public void Set(string id, ProxyBehavior behavior)
		{
			_topology.Paths[id].Behavior = behavior;
		}
	}
}