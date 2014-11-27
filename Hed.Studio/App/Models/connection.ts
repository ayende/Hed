
class connection {
    from = ko.observable<String>();
    to = ko.observable<String>();
    behavior = ko.observable<String>();
    constructor(from, to, behavior) {
        this.from = from;
        this.to = to;
        this.behavior = behavior;
    }
}

export =connection;