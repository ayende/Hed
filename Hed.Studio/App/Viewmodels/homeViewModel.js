define(["require", "exports", "Models/topology", "Models/connection"], function(require, exports, topology, connection) {
    var homeViewModel = (function () {
        function homeViewModel() {
            var _this = this;
            this.behavior = ko.observable(["Optimal"]);
            this.behaviors = ko.observableArray(["Optimal", "Normal", "Slow", "Hiccups", "Dropping", "Down"]);
            this.actualBehaviors = ko.observableArray(["503", "CloseTsp", "Optimal", "Slow", "Drop", "Repeated", "HalfSend"]);
            this.hostName = ko.observable("");
            this.databaseName = ko.observable("");
            this.databases = ko.observableArray();
            this.ctrlKeyPressed = ko.observable(false);
            this.shiftKeyPressed = ko.observable(false);
            this.operationsJson = ko.mapping.fromJS({});
            this.operations = ko.observableArray();
            this.currentTopo = ko.observable(new topology());
            this.selectedNode = ko.observable("");
            document.onkeydown = function (event) {
                return _this.OnKeyDown(event);
            };
            document.onkeyup = function (event) {
                return _this.OnKeyUp(event);
            };
            $.ajax("/topology/view", "GET").done(function (x) {
                _this.createGraph();
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
        homeViewModel.prototype.addEndPointOnClick = function () {
            var _this = this;
            if (typeof this.databaseName() === 'undefined' || this.databaseName() === "") {
                $.ajax("/topology/getdatabases?" + "&url=" + encodeURIComponent(this.hostName()), "GET").done(function (x) {
                    for (var db in x) {
                        _this.pushDatabaseUnique(x[db]);
                    }
                });
            } else {
                this.pushDatabaseUnique(this.databaseName());
            }
            this.redraw();
        };

        homeViewModel.prototype.pushDatabaseUnique = function (dbName) {
            var dbFullName = this.hostName() + "/databases/" + dbName;
            var match = ko.utils.arrayFirst(this.databases(), function (item) {
                return dbFullName === item;
            });
            if (!match) {
                this.databases.push(dbFullName);
                this.g.setNode(dbFullName, { label: "Host: " + this.hostName() + ", Database: " + dbName, width: 250, height: 65 });
                this.redraw();
            }
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
                var pathId = this.CheckIfPathInCurrentTopology(path);
                if (pathId === "-1")
                    continue;
                var pathProp = this.operationsJson[path];
                var pathOperation = {
                    Key: pathId,
                    Path: path, Behavior_503: pathProp.hasOwnProperty("503") ? pathProp["503"]["Value"] : 0,
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

        homeViewModel.prototype.CheckIfPathInCurrentTopology = function (path) {
            var splitPath = path.split("=>");
            for (var key in this.currentTopo().paths()) {
                var connection = this.currentTopo().paths()[key];
                if (connection.from === splitPath[0] && connection.to === splitPath[1]) {
                    return connection.key;
                }
            }
            return "-1";
        };

        homeViewModel.prototype.dispose = function () {
            this.websocket.close(homeViewModel.normalGoingAwayClosureCode);
        };

        homeViewModel.prototype.createGraph = function () {
            this.g = new dagreD3.graphlib.Graph();
            this.g.setGraph({});
            this.g.setDefaultEdgeLabel(function () {
                return {};
            });
            var render = new dagreD3.render();
            this.renderer = render;
        };

        homeViewModel.prototype.generateNewGraphFromTopo = function (topo, flush) {
            if (flush) {
                this.flushGraph();
            }
            this.currentTopo().paths.removeAll();
            for (var key in topo.Paths) {
                var path = topo.Paths[key];
                var fromSplit = path.From.split("/databases/");
                var toSplit = path.To.split("/databases/");
                this.g.setNode(path.From, { label: "Host: " + fromSplit[0] + ", Database: " + fromSplit[1], width: 250, height: 65 });
                this.g.setNode(path.To, { label: "Host: " + toSplit[0] + ", Database: " + toSplit[1], width: 250, height: 65 });
                var behaviorColor = this.getColorFromBehavior(path.Behavior);
                this.currentTopo().paths.push(new connection(key, path.From, path.To, path.Behavior));
                this.g.setEdge(path.From, path.To, {
                    label: path.Behavior,
                    labelStyle: "fill: " + behaviorColor
                });
                this.databases().push(path.From);
                this.databases().push(path.To);
            }
            this.redraw();
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
            delete this.g;
            this.createGraph();
            this.databases.removeAll();
        };

        homeViewModel.prototype.addEdgeCore = function (from, to, behavior) {
            var _this = this;
            if (from === to) {
                alert("No self connections are allowed.");
                return;
            }
            var url = "/topology/set?&from=" + encodeURIComponent(from) + "&to=" + encodeURIComponent(to) + "&behavior=" + behavior;
            $.ajax(url, "GET").done(function (x) {
                _this.generateNewGraphFromTopo(x, true);
            }).fail(function () {
                alert("Failed to add path from " + from + " to " + to + " such path already exists.");
            });
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

        homeViewModel.prototype.redraw = function () {
            var svg = d3.select("svg"), inner = svg.select("g");
            this.svg = svg;
            this.inner = inner;

            // Set up zoom support
            var zoom = d3.behavior.zoom().on("zoom", function () {
                inner.attr("transform", "translate(" + d3.event.translate + ")" + "scale(" + d3.event.scale + ")");
            });
            this.zoom = zoom;
            this.svg.call(zoom);
            this.renderer(this.inner, this.g);

            // Center the graph
            var initialScale = 0.75;
            this.zoom.translate([(this.svg.attr("width") - this.g.graph().width * initialScale) / 2, 20]).scale(initialScale).event(this.svg);
            this.svg.attr('height', this.g.graph().height * initialScale + 40);
            inner.selectAll("g.node").attr('onclick', function (n) {
                return 'ko.dataFor(document.getElementById("homeViewModel")).onNodeClick("' + n + '");';
            });
        };

        homeViewModel.prototype.OnKeyDown = function (event) {
            if (event.keyCode === 17)
                this.ctrlKeyPressed(true);
            else if (event.keyCode === 16)
                this.shiftKeyPressed(true);
            else if (event.keyCode === 38)
                this.behavior([this.behaviors()[(this.behaviors.indexOf(this.behavior()[0]) + this.behaviors().length - 1) % this.behaviors().length]]);
            else if (event.keyCode === 40)
                this.behavior([this.behaviors()[(this.behaviors.indexOf(this.behavior()[0]) + this.behaviors().length + 1) % this.behaviors().length]]);
            return true;
        };

        homeViewModel.prototype.OnKeyUp = function (event) {
            if (event.keyCode === 17) {
                this.ctrlKeyPressed(false);
                this.selectedNode("");
            }
            if (event.keyCode === 16) {
                this.shiftKeyPressed(false);
                this.selectedNode("");
            }
            return true;
        };

        homeViewModel.prototype.onNodeClick = function (n) {
            if (this.ctrlKeyPressed()) {
                if (this.selectedNode() === "") {
                    this.selectedNode(n);
                } else {
                    this.addEdgeCore(this.selectedNode(), n, this.behavior()[0]);
                    this.selectedNode("");
                }
            } else if (this.shiftKeyPressed) {
                if (this.selectedNode() === "") {
                    this.selectedNode(n);
                } else {
                    this.removeEdgeCore(this.selectedNode(), n);
                    this.selectedNode("");
                }
            }
        };
        homeViewModel.normalClosureCode = 1000;
        homeViewModel.normalGoingAwayClosureCode = 1001;
        return homeViewModel;
    })();

    
    return homeViewModel;
});
