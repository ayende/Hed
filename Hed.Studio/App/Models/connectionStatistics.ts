class connectionStatistics {
    connectionString: string;
    optimalCount = ko.observable<number>(0);
    normalCount = ko.observable<number>(0);
    slowCount = ko.observable<number>(0);
    hiccupsCount = ko.observable<number>(0);
    droppingCount = ko.observable<number>(0);
    downCount = ko.observable<number>(0);   
    constructor(conStr) {
        this.connectionString = conStr;
    } 
}