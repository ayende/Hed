
class connection {
    key = ko.observable<String>();
    from = ko.observable<String>();
    to = ko.observable<String>();
    behavior = ko.observable<String>();
    constructor(key, from, to, behavior) {
        this.key = key;
        this.from = from;
        this.to = to;
        this.behavior = behavior;
    }
}

export =connection;