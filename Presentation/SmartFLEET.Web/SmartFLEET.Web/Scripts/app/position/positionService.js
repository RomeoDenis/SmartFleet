angular.module('app.services').service('positionService', ['$http', function ($http) {

    this.getPosition = function getPosition(vehicleId, startPeriod) {
        return $http({
            method: 'GET',
            url: currentLang+'/Position/GetPositionByDate?vehicleId=' + vehicleId + "&start="+ startPeriod+"00:00"
        });
    }
    
}]);
