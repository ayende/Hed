define(["require", "exports"], function(require, exports) {
    var connection = (function () {
        function connection(key, from, to, behavior) {
            this.key = ko.observable();
            this.from = ko.observable();
            this.to = ko.observable();
            this.behavior = ko.observable();
            this.key = key;
            this.from = from;
            this.to = to;
            this.behavior = behavior;
        }
        return connection;
    })();

    
    return connection;
});
//# sourceMappingURL=connection.js.map
