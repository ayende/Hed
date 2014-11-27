define(["require", "exports"], function(require, exports) {
    var requestStatistics = (function () {
        function requestStatistics(topo) {
            var _this = this;
            this.activeConnectionsStrings = ko.observableArray();
            this.connectionCounters = ko.observableArray();
            this.activeConnectionCounters = ko.computed(function () {
                var res = [];
                var dummy = _this.connectionCounters;
                for (var conKey in _this.activeConnectionsStrings()) {
                    try  {
                        res.push(_this.connections[conKey]);
                    } catch (ex) {
                        alert(ex.message);
                    }
                }
                return res;
            }, this).extend({ rateLimit: 500 });
            this.connections = {};
            for (var path in topo.paths) {
                try  {
                    this.activeConnectionsStrings.push(path);
                    var connectionString = path.from + "=>" + path.to;
                    var conStats = new connectionStatistics(connectionString);
                    this.connectionCounters.push(conStats);
                    this.connections[connectionString] = conStats;
                } catch (ex) {
                }
            }
        }
        requestStatistics.prototype.setActiveConnections = function (topo) {
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
        };
        return requestStatistics;
    })();
    
    return requestStatistics;
});
//# sourceMappingURL=requestStatistics.js.map
