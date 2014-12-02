define(["require", "exports"], function(require, exports) {
    var topology = (function () {
        function topology() {
            this.paths = ko.observableArray();
        }
        return topology;
    })();

    
    return topology;
});
