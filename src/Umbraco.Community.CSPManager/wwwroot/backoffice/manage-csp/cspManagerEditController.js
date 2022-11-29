(function(){
	"use strict";
	
	function controller($scope, $routeParams, navigationService)
	{
		let vm = this;
		
		let init = () => {
			navigationService.syncTree({ tree: 'manage-csp', path: ["-1", $routeParams.id], forceReload: false });

			vm.page = {
				isBackOffice: $routeParams.id === "1",
			};
			vm.page.name =`Manage ${ vm.page.isBackOffice ? "Back Office" : "Front end"} CSP`;
		}
		
		init();
	}
	
	angular.module("umbraco").controller("cspManagerEditController", controller);	
})();