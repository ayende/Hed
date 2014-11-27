define(["require", "exports", "Models/topology", "Models/connection"], function(require, exports, topology, connection) {
    var homeViewModel = (function () {
        function homeViewModel() {
            var _this = this;
            this.newVertex = ko.observable("");
            this.edgeFrom = ko.observable("");
            this.edgeTo = ko.observable("");
            this.behavior = ko.observable(["Optimal"]);
            this.behaviors = ko.observableArray(["Optimal", "Normal", "Slow", "Hiccups", "Dropping", "Down"]);
            this.actualBehaviors = ko.observableArray(["503", "CloseTsp", "Optimal", "Slow", "Drop", "Repeated", "HalfSend"]);
            this.hostName = ko.observable("");
            this.databaseName = ko.observable("");
            this.databases = ko.observableArray();
            this.dbFrom = ko.observable("");
            this.dbTo = ko.observable("");
            this.operationsJson = ko.mapping.fromJS({});
            this.operations = ko.observableArray();
            this.vertexes = ko.observableArray();
            this.currentTopo = ko.observable(new topology());
            $.ajax("/topology/view", "GET").done(function (x) {
                //this.requestStatistics(x);
                _this.g = new Graph();
                _this.generateNewGraphFromTopo(x, false);
                _this.websocket = new WebSocket("ws://localhost:9091/");
                _this.websocket.onmessage = function (event) {
                    return _this.generateRequestStatistic(event.data);
                };
                _this.websocket.onclose = function () {
                };
                window.onbeforeunload = _this.dispose;
            });
        }
        homeViewModel.prototype.addDatabaseOnClick = function () {
            var dbFullName = this.hostName() + "/databases/" + this.databaseName();
            var match = ko.utils.arrayFirst(this.databases(), function (item) {
                return dbFullName === item;
            });
            if (!match) {
                this.databases.push(dbFullName);
            }
        };

        homeViewModel.prototype.addRelationship = function () {
            var _this = this;
            if (this.dbFrom()[0] === this.dbTo()[0]) {
                alert("Can not set a relationship between one database and himself.");
                return;
            }
            $.ajax("/replication/set?&dbFrom=" + encodeURIComponent(this.dbFrom()) + "&dbTo=" + encodeURIComponent(this.dbTo()), "GET").done(function (x) {
                _this.generateNewGraphFromTopo(x, true);
            }).fail(function () {
                alert("Failed to add relationship between " + _this.dbFrom() + " and " + _this.dbTo());
            });
        };
        homeViewModel.prototype.generateRequestStatistic = function (dataAsJson) {
            this.operationsJson = ko.mapping.fromJSON(dataAsJson);
            this.operations(this.computeStatistics());
        };
        homeViewModel.prototype.computeStatistics = function () {
            var res = [];
            for (var path in this.operationsJson) {
                if (path === "__ko_mapping__")
                    continue;
                var pathProp = this.operationsJson[path];
                var pathOperation = {
                    key: path, Behavior_503: pathProp.hasOwnProperty("503") ? pathProp["503"]["Value"] : 0,
                    Behavior_CloseTsp: pathProp.hasOwnProperty("CloseTsp") ? pathProp["CloseTsp"]["Value"] : 0,
                    Behavior_Optimal: pathProp.hasOwnProperty("Optimal") ? pathProp["Optimal"]["Value"] : 0,
                    Behavior_Slow: pathProp.hasOwnProperty("Slow") ? pathProp["Slow"]["Value"] : 0,
                    Behavior_Drop: pathProp.hasOwnProperty("Drop") ? pathProp["Drop"]["Value"] : 0,
                    Behavior_Repeated: pathProp.hasOwnProperty("Repeated") ? pathProp["Repeated"]["Value"] : 0,
                    Behavior_HalfSend: pathProp.hasOwnProperty("HalfSend") ? pathProp["HalfSend"]["Value"] : 0
                };
                res.push(pathOperation);
            }
            return res;
        };
        homeViewModel.prototype.dispose = function () {
            this.websocket.close(homeViewModel.normalGoingAwayClosureCode);
        };
        homeViewModel.prototype.endPointChanged = function () {
            var from = this.edgeFrom()[0];
            var to = this.edgeTo()[0];
            for (var key in this.currentTopo["Paths"]) {
                var path = this.currentTopo["Paths"][key];
                if (path.From === from && path.To === to) {
                    this.behavior([path.Behavior]);
                    continue;
                }
            }
        };
        homeViewModel.prototype.generateNewGraphFromTopo = function (topo, flush) {
            if (flush) {
                this.flushGraph();
            }
            this.g = new Graph();
            this.currentTopo().paths.removeAll();
            for (var key in topo.Paths) {
                var path = topo.Paths[key];
                this.g.addNode(path.From);
                this.g.addNode(path.To);
                var behaviorColor = this.getColorFromBehavior(path.Behavior);

                this.currentTopo().paths.push(new connection(path.From, path.To, path.Behavior));

                var behavior = {
                    directed: true, label: path.Behavior,
                    "label-style": {
                        "font-size": 20,
                        "color": behaviorColor
                    }
                };
                this.g.addEdge(path.From, path.To, behavior);
                this.addVertexUnique(path.From);
                this.addVertexUnique(path.To);
            }

            //this.requestStatistics().setActiveConnections(this.currentTopo());
            this.layouter = new Graph.Layout.Spring(this.g);
            this.renderer = new Graph.Renderer.Raphael('canvas', this.g, 1500, 500);
        };

        homeViewModel.prototype.getColorFromBehavior = function (behavior) {
            var behaviorColor;
            switch (behavior) {
                case "Optimal":
                    behaviorColor = "#008000";
                    break;
                case "Normal":
                    behaviorColor = "#0000FF";
                    break;
                case "Slow":
                    behaviorColor = "#FF6600";
                    break;
                case "Hiccups":
                    behaviorColor = "#FF00FF";
                    break;
                case "Dropping":
                    behaviorColor = "#FF0000";
                    break;
                case "Down":
                    behaviorColor = "#000000";
                    break;
            }
            return behaviorColor;
        };

        homeViewModel.prototype.flushGraph = function () {
            var canvaas = document.getElementById('canvas');
            canvaas.removeChild(canvaas.childNodes[0]);
            delete this.g;
            delete this.layouter;
            delete this.renderer;
            this.vertexes.removeAll();
        };

        homeViewModel.prototype.addEdge = function () {
            var from = this.edgeFrom()[0];
            var to = this.edgeTo()[0];
            this.addEdgeCore(from, to);
        };
        homeViewModel.prototype.addEdgeCore = function (from, to) {
            var _this = this;
            if (from === to) {
                alert("No self connections are allowed.");
                return;
            }
            var url = "/topology/set?&from=" + encodeURIComponent(from) + "&to=" + encodeURIComponent(to) + "&behavior=" + this.behavior()[0];
            $.ajax(url, "GET").done(function (x) {
                _this.generateNewGraphFromTopo(x, true);
            }).fail(function () {
                alert("Failed to add path from " + from + " to " + to + " such path already exists.");
            });
        };

        homeViewModel.prototype.removeEdge = function () {
            var from = this.edgeFrom()[0];
            var to = this.edgeTo()[0];
            this.removeEdgeCore(from, to);
        };
        homeViewModel.prototype.removeEdgeCore = function (from, to) {
            var _this = this;
            var url = "/topology/del?&from=" + encodeURIComponent(from) + "&to=" + encodeURIComponent(to);
            $.ajax(url, "GET").done(function (x) {
                _this.generateNewGraphFromTopo(x, true);
            }).fail(function () {
                alert("Failed to remove connection from " + from + " to " + to + ".");
            });
        };

        homeViewModel.prototype.addVertexUnique = function (itemToAdd) {
            var match = ko.utils.arrayFirst(this.vertexes(), function (item) {
                return itemToAdd === item;
            });
            if (!match)
                this.vertexes.push(itemToAdd);
        };

        homeViewModel.prototype.addEndPointOnClick = function () {
            this.g.addNode(this.newVertex());
            this.addVertexUnique(this.newVertex());
            this.redraw();
        };
        homeViewModel.prototype.redraw = function () {
            this.layouter.layout();
            this.renderer.draw();
        };
        homeViewModel.prototype.onButtonClick = function () {
            $.ajax("/topology/view", "GET").done(function (x) {
                alert(x);
            });
        };
        homeViewModel.normalClosureCode = 1000;
        homeViewModel.normalGoingAwayClosureCode = 1001;
        return homeViewModel;
    })();

    
    return homeViewModel;
});
//# sourceMappingURL=homeViewModel.js.map
