import  topology = require("topology")
class requestStatistics {
    activeConnectionsStrings = ko.observableArray<string>()
    connectionCounters = ko.observableArray<connectionStatistics>()
    activeConnectionCounters = ko.computed(() => {
        var res = [];
        var dummy = this.connectionCounters; // trying to force computed 
        for (var conKey in this.activeConnectionsStrings()) {
            try {
                res.push(this.connections[conKey]);
            } catch (ex) {
                alert(ex.message)
            }
        }
        return res;
    },this).extend({ rateLimit: 500 })
    connections: { [id: string]: connectionStatistics} = {};

    constructor(topo: topology) {
        for (var path in topo.paths) {
            try {
                this.activeConnectionsStrings.push(path);
                var connectionString = path.from + "=>" + path.to;
                var conStats = new connectionStatistics(connectionString);
                this.connectionCounters.push(conStats);
                this.connections[connectionString] = conStats;
            } catch  (ex){
                
            }
        }
    }
    setActiveConnections(topo: topology) {
        this.activeConnectionsStrings.removeAll();
        for (var path in topo.paths) {
            this.activeConnectionsStrings.push(path);
            var connectionString = path.from + "=>" + path.to;
            if (typeof this.connections[connectionString] === 'undefined') {
                var conStats = new connectionStatistics(connectionString);
                this.connectionCounters.push(conStats);
                this.connections[connectionString] = conStats;
            }           
        }
    }
} 
export = requestStatistics