define(["require", "exports"], function(require, exports) {
    var connection = (function () {
        function connection(from, to, behavior) {
            this.from = ko.observable();
            this.to = ko.observable();
            this.behavior = ko.observable();
            this.from = from;
            this.to = to;
            this.behavior = behavior;
        }
        return connection;
    })();

    
    return connection;
});
//# sourceMappingURL=connection.js.map
