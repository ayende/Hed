using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Hed.ConsoleHost
{
	public class HedConfiguration
	{
		private  string config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "topology.json");
		private  ProxyTopology _topology;

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

	    public void OverrideTopologyPath(string topoPath)
	    {
	        if (File.Exists(topoPath)) config = topoPath;
            _topology = JsonConvert.DeserializeObject<ProxyTopology>(File.ReadAllText(config));
	    }

		public bool TryGetPath(string value, out ProxyPath path)
		{
			return _topology.Paths.TryGetValue(value, out path);
		}

        public string Set(string src, string dest, ProxyBehavior behavior = ProxyBehavior.Optimal)
		{
            var destUri = new Uri(dest);
            if (_topology.Paths.Count == 0)
            {
                _topology.Paths.Add("1", new ProxyPath
                {
                    Behavior = behavior,
                    To =   destUri,
                    From =  src
                });
                return "1";
            }


            var path = _topology.Paths.FirstOrDefault(x => x.Value.From == src && x.Value.To == destUri);
            if (path.Value != null)
            {
                path.Value.Behavior = behavior;
                return path.Key;
            }

            var key = (_topology.Paths.Keys.Select(int.Parse).Max() + 1).ToString(CultureInfo.InvariantCulture);
			_topology.Paths.Add(key, new ProxyPath
			{
                Behavior = behavior,
				From = src,
				To = destUri
			});

			return key;
		}

		public void Flush()
		{
			File.WriteAllText(config, JsonConvert.SerializeObject(_topology));
		}

		public void Delete(string src, string dest)
		{
            var destUri = new Uri(dest);
            var path = _topology.Paths.FirstOrDefault(x => x.Value.From == src && x.Value.To == destUri);
            if (path.Value != null)
            {
                _topology.Paths.Remove(path.Key);
            }

		}

		public void Set(string id, ProxyBehavior behavior)
		{
			_topology.Paths[id].Behavior = behavior;
		}
	}
}