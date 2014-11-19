///<reference path='../../Scripts/typings/jquery/jquery.d.ts' />
///<reference path='../../Scripts/knockout.d.ts' />
/// <amd-dependency path="lib/Graph">
class homeViewModel {  
    newVertex = ko.observable("")
    edgeFrom = ko.observable("")
    edgeTo = ko.observable("")
    behavior = ko.observable(["Optimal"])
    behaviors = ko.observableArray<String>(["Optimal","Normal","Slow","Hiccups","Dropping","Down"]);
    vertexes = ko.observableArray<String>();
    currentTopo = ko.observable();
    layouter: any;
    renderer: any;
    g:any;
    constructor() {
        $.ajax("/topology/view", "GET").done(x => {
            this.g = new Graph();
            this.generateNewGraphFromTopo(x,false);            
        });
    }

    generateNewGraphFromTopo(topo,flush) {
        if (flush) {
            this.flushGraph();
        }     
        this.g = new Graph();
        this.currentTopo = topo;
        for (var key in topo.Paths) {
            var path = topo.Paths[key];
            this.g.addNode(path.From);
            this.g.addNode(path.To);
            var behaviorColor = this.getColorFromBehavior(path.Behavior);
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
        canvaas.removeChild(canvaas.childNodes[0])
        delete this.g;
        delete this.layouter;
        delete this.renderer;
        this.vertexes.removeAll();
    }

    addEdge() {
        var from = this.edgeFrom();
        var to = this.edgeTo();
        var url = "/topology/set?&from=" + encodeURIComponent(from) + "&to=" + encodeURIComponent(to) + "&behavior=" + this.behavior()[0];
        $.ajax(url, "GET").done(x => {
            this.generateNewGraphFromTopo(x,true);
        }).fail(() => { alert("Failed to add path from " + from + " to " + to + " such path already exists.")});              
    }

    removeEdge() {
        var from = this.edgeFrom()[0];
        var to = this.edgeTo()[0];
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



