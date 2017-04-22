﻿(function () {
    'use strict';

    angular
        .module("app")
        .controller('Event.IndexController', Controller);


    function Controller(classService, userService, $localStorage) {
        /* jshint validthis:true */
        var vm = this;

        vm.activate = activate;
        vm.createEvent = createEvent;
        vm.partners = [];
        vm.isTutor = $localStorage.currentUser.userIsTutor;
        vm.partnerLabel = vm.isTutor ? "Choose student" : "Choose tutor";

        function activate() {
            vm.partners.slice(0);
            userService.getPartners().then(function (response) {
                var partners = response.data;
                var len = partners.length;
                for (var i = 0; i < len; i++) {
                    vm.partners.push(partners[i]);
                }
            });
        }

        function createEvent() {
            var partner = vm.selectedPartner.Id;
            var event
            if (vm.isTutor) {
                event = { subject: vm.subject, StudentId: partner, startTime: vm.startTime, endTime: vm.endTime };
            } else {
                event = { subject: vm.subject, TutorId: partner, startTime: vm.startTime, endTime: vm.endTime };
            }

            classService.createClass(event).then(function () {
                console.log("Creating event");
            });
        }
    }
})();