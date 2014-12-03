var connectionStatistics = (function () {
    function connectionStatistics(conStr) {
        this.optimalCount = ko.observable(0);
        this.normalCount = ko.observable(0);
        this.slowCount = ko.observable(0);
        this.hiccupsCount = ko.observable(0);
        this.droppingCount = ko.observable(0);
        this.downCount = ko.observable(0);
        this.connectionString = conStr;
    }
    return connectionStatistics;
})();
