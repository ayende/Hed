// -----------------------------------------------------------------------
//  <copyright file="ProxyTopology.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Hed.ConsoleHost
{
	public class ProxyTopology
	{
		public Dictionary<string,ProxyPath>  Paths = new Dictionary<string, ProxyPath>(StringComparer.OrdinalIgnoreCase);
	}

	public class ProxyPath
	{
		public string From;
		public Uri To;
		public ProxyBehavior Behavior;

        public ConcurrentDictionary<string, Ref> Operations = new ConcurrentDictionary<string, Ref>(); 

	    public void Operation(string name)
	    {
	        var r = Operations.GetOrAdd(name, s => new Ref());
	        Interlocked.Increment(ref r.Value);
	    }
	}

    public class Ref
    {
        public int Value;
    }

	public enum ProxyBehavior
	{
		Optimal,
		Normal, // 95% success, 3% slow, 1% Hiccup, 1% Dropping
		Slow, // 500ms - 5000ms additional latency
		Hiccups, // 15% chance dropped connection, 
				 // 15% chance slow
				 // 15% chance repeated request
				 // 15% chance send request, but if has body, only send half, and stop
				 // rest, normal
		Dropping, // 30% chance dropped connection, 30% chance 503 error, 30% chance close TCP, 10% normal
		Down, // 50% dropped connection, 30% close TCP, rest 503 error

	}
}