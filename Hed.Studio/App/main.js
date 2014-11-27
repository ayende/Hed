requirejs.config({
    paths: {
        //durandal: '../Scripts/durandal',
    }
});

define('jquery', function() { return jQuery; });
define('knockout', ko);
define('mapping', ko.mapping);
define(["require", "exports", 'ViewModels/homeViewModel'], function (a, b, model) {
    var vm = new model();
    ko.applyBindings(vm, document.getElementById('homeViewModel'));
    //ko.applyBindings(vm.requestStatistics(), document.getElementById('requests'));
});
