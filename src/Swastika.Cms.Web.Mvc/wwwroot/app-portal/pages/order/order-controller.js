﻿'use strict';
app.controller('OrderController', ['$scope', '$rootScope', 'ngAppSettings', '$routeParams', '$timeout', '$location', 'AuthService', 'OrderServices', 'ngAppSettings',
    function ($scope, $rootScope, ngAppSettings, $routeParams, $timeout, $location, authService, orderServices) {
        $scope.request = ngAppSettings.request;
        $scope.request.swStatus = [
            'Waiting',
            'Serving',
            'Rated',
            'Finished'
        ];
        $scope.request.status = '2';
        $scope.activedOrder = null;
        $scope.relatedOrders = [];
        $rootScope.isBusy = false;
        $scope.data = {
            pageIndex: 0,
            pageSize: 1,
            totalItems: 0,
            items: []
        };
        $scope.errors = [];

        $scope.range = function (max) {
            var input = [];
            for (var i = 1; i <= max; i += 1) input.push(i);
            return input;
        }

        $scope.loadOrder = async function () {
            $rootScope.isBusy = true;
            var id = $routeParams.id;
            var response = await orderServices.getOrder(id, 'be');
            if (response.isSucceed) {
                $scope.activedOrder = response.data;
                $rootScope.initEditor();
                $rootScope.isBusy = false;
                $scope.$apply();
            }
            else {
                $rootScope.showErrors(response.errors);
                $rootScope.isBusy = false;
                $scope.$apply();
            }
        };
        $scope.loadOrders = async function (pageIndex) {
            if (pageIndex !== undefined) {
                $scope.request.pageIndex = pageIndex;
            }
            if ($scope.request.fromDate !== null) {
                var d = new Date($scope.request.fromDate);
                $scope.request.fromDate = d.toISOString();
            }
            if ($scope.request.toDate !== null) {
                $scope.request.toDate = $scope.request.toDate.toISOString();
            }
            $rootScope.isBusy = true;
            var resp = await orderServices.getOrders($scope.request);
            if (resp && resp.isSucceed) {

                ($scope.data = resp.data);
                //$("html, body").animate({ "scrollTop": "0px" }, 500);
                $.each($scope.data.items, function (i, order) {

                    $.each($scope.activedOrders, function (i, e) {
                        if (e.orderId === order.id) {
                            order.isHidden = true;
                        }
                    })
                })
                $rootScope.isBusy = false;
                $scope.$apply();
            }
            else {
                if (resp) { $rootScope.showErrors(resp.errors); }
                $rootScope.isBusy = false;
                $scope.$apply();
            }
        };

        $scope.removeOrder = function (id) {
            $rootScope.showConfirm($scope, 'removeOrderConfirmed', [id], null, 'Remove Order', 'Are you sure');
        };

        $scope.removeOrderConfirmed = async function (id) {
            $rootScope.isBusy = true;
            var result = await orderServices.removeOrder(id);
            if (result.isSucceed) {
                $scope.loadOrders();
            }
            else {
                $rootScope.showMessage('failed');
                $rootScope.isBusy = false;
                $scope.$apply();
            }
        };

        $scope.saveOrder = async function (order) {
            order.content = $('.editor-content.content').val();
            order.excerpt = $('.editor-content.excerpt').val();
            $rootScope.isBusy = true;
            var resp = await orderServices.saveOrder(order);
            if (resp && resp.isSucceed) {
                $scope.activedOrder = resp.data;
                $rootScope.showMessage('Thành công', 'success');
                $rootScope.isBusy = false;
                $scope.$apply();
                //$location.path('/backend/order/details/' + resp.data.id);
            }
            else {
                if (resp) { $rootScope.showErrors(resp.errors); }
                $rootScope.isBusy = false;
                $scope.$apply();
            }
        };

        $scope.preview = function (item) {
            $rootScope.preview('order', item, item.title, 'modal-lg');
        };
    }]);