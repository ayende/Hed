var connectionStatistics = (function () {
    function connectionStatistics() {
        this.NormalCount = ko.observable(0);
        this.SlowCount = ko.observable(0);
        this.HiccupsCount = ko.observable(0);
        this.DroppingCount = ko.observable(0);
        this.DownCount = ko.observable(0);
    }
    return connectionStatistics;
})();
//# sourceMappingURL=connectionStatistics1.js.map
