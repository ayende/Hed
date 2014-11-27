/// <reference path="../../Scripts/knockout.mapping.d.ts" />
import topology = require("Models/topology");
//import requestStatistics = require("Models/requestStatistics");
import connection = require("Models/connection");

class homeViewModel {  
    newVertex = ko.observable("");
    edgeFrom = ko.observable("");
    edgeTo = ko.observable("");
    behavior = ko.observable(["Optimal"]);
    behaviors = ko.observableArray<String>(["Optimal", "Normal", "Slow", "Hiccups", "Dropping", "Down"]);
    actualBehaviors = ko.observableArray<String>(["503", "CloseTsp", "Optimal", "Slow", "Drop", "Repeated", "HalfSend"]);
    hostName = ko.observable("");
    databaseName = ko.observable("");
    databases = ko.observableArray<String>();
    dbFrom = ko.observable("");
    dbTo = ko.observable("");
    operationsJson = ko.mapping.fromJS({});
    operations = ko.observableArray(); 
    vertexes = ko.observableArray<String>();
    currentTopo = ko.observable<topology>(new topology());
    //requestStatistics = ko.observable<requestStatistics>(new requestStatistics(this.currentTopo()));
    private static  normalClosureCode = 1000;
    private static normalGoingAwayClosureCode = 1001;
    layouter: any;
    renderer: any;
    g: any;
    websocket : any
    constructor() {
        $.ajax("/topology/view", "GET").done(x => {
            //this.requestStatistics(x);
            this.g = new Graph();
            this.generateNewGraphFromTopo(x, false);
            this.websocket = new WebSocket("ws://localhost:9091/");
            this.websocket.onmessage = (event) => this.generateRequestStatistic(event.data);
            this.websocket.onclose = () => { };
            window.onbeforeunload = this.dispose;
        });
    }

    addDatabaseOnClick() {
        var dbFullName = this.hostName() + "/databases/" + this.databaseName();
        var match = ko.utils.arrayFirst(this.databases(), function (item) {
            return dbFullName === item;
        })
        if (!match) { 
            this.databases.push(dbFullName); 
        }
    }

    addRelationship() {
        if (this.dbFrom()[0] === this.dbTo()[0]) {
            alert("Can not set a relationship between one database and himself.");
            return;
        }
        $.ajax("/replication/set?&dbFrom=" + encodeURIComponent(this.dbFrom()) + "&dbTo=" + encodeURIComponent(this.dbTo()), "GET").done(x => {
            this.generateNewGraphFromTopo(x, true);
        }).fail(() => {
                alert("Failed to add relationship between " + this.dbFrom() + " and " + this.dbTo());
            }
            )
    }
    generateRequestStatistic(dataAsJson) {
        this.operationsJson = ko.mapping.fromJSON(dataAsJson);
        this.operations(this.computeStatistics());
    }
    computeStatistics() {
        var res = []
        for (var path in this.operationsJson) {
            if (path === "__ko_mapping__") continue;
            var pathProp = this.operationsJson[path];
            var pathOperation = {
                key: path, Behavior_503: pathProp.hasOwnProperty("503") ? pathProp["503"]["Value"] : 0
                , Behavior_CloseTsp: pathProp.hasOwnProperty("CloseTsp") ? pathProp["CloseTsp"]["Value"] : 0
                , Behavior_Optimal: pathProp.hasOwnProperty("Optimal") ? pathProp["Optimal"]["Value"] : 0
                , Behavior_Slow: pathProp.hasOwnProperty("Slow") ? pathProp["Slow"]["Value"] : 0 
                , Behavior_Drop: pathProp.hasOwnProperty("Drop") ? pathProp["Drop"]["Value"] : 0
                , Behavior_Repeated: pathProp.hasOwnProperty("Repeated") ? pathProp["Repeated"]["Value"] : 0 
                , Behavior_HalfSend: pathProp.hasOwnProperty("HalfSend") ? pathProp["HalfSend"]["Value"] : 0
            }
            res.push(pathOperation);
        }
        return res;
    }
    dispose() {
        this.websocket.close(homeViewModel.normalGoingAwayClosureCode);
    }
    endPointChanged() {        
        var from = this.edgeFrom()[0];
        var to = this.edgeTo()[0];
        for (var key in this.currentTopo["Paths"]) {
            var path = this.currentTopo["Paths"][key];
            if (path.From === from && path.To === to) {
                this.behavior([path.Behavior]);
                continue;
            }
        } 
    }
    generateNewGraphFromTopo(topo,flush) {
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
            }
            this.g.addEdge(path.From, path.To, behavior);
            this.addVertexUnique(path.From);
            this.addVertexUnique(path.To);
        }
        //this.requestStatistics().setActiveConnections(this.currentTopo());
        this.layouter = new Graph.Layout.Spring(this.g);
        this.renderer = new Graph.Renderer.Raphael('canvas', this.g, 1500, 500);       
    }

    getColorFromBehavior(behavior) {
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
    }

    flushGraph() {
        var canvaas = document.getElementById('canvas');
        canvaas.removeChild(canvaas.childNodes[0]);
        delete this.g;
        delete this.layouter;
        delete this.renderer;
        this.vertexes.removeAll();
    }

    addEdge() {
        var from = this.edgeFrom()[0];
        var to = this.edgeTo()[0];
        this.addEdgeCore(from, to);
    }
    addEdgeCore(from,to) {
        if (from === to) {
            alert("No self connections are allowed.");
            return;
        }
        var url = "/topology/set?&from=" + encodeURIComponent(from) + "&to=" + encodeURIComponent(to) + "&behavior=" + this.behavior()[0];
        $.ajax(url, "GET").done(x => {
            this.generateNewGraphFromTopo(x,true);
        }).fail(() => { alert("Failed to add path from " + from + " to " + to + " such path already exists.")});              
    }

    removeEdge() {
        var from = this.edgeFrom()[0];
        var to = this.edgeTo()[0];
        this.removeEdgeCore(from, to);
    }
    removeEdgeCore(from,to) {        
        var url = "/topology/del?&from=" + encodeURIComponent(from) + "&to=" + encodeURIComponent(to);  
        $.ajax(url, "GET").done(x => {
            this.generateNewGraphFromTopo(x, true);            
        }).fail(() => { alert("Failed to remove connection from " + from + " to " + to + ".") });
    }
 
    addVertexUnique( itemToAdd) {
        var match = ko.utils.arrayFirst(this.vertexes(), function(item) {
            return itemToAdd === item;
        })
        if (!match) this.vertexes.push(itemToAdd);
    }

    addEndPointOnClick() {
        this.g.addNode(this.newVertex());
        this.addVertexUnique(this.newVertex());
        this.redraw();
    }    
    redraw () {
        this.layouter.layout();
        this.renderer.draw();
    }
    onButtonClick() {
     
        $.ajax("/topology/view","GET").done(x => {
            alert(x);
        });
    }
}

export = homeViewModel;


