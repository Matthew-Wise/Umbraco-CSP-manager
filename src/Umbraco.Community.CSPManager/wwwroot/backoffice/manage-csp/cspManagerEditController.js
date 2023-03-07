(function () {
	"use strict";

	function controller($routeParams, navigationService, notificationsService, cspManagerResource, ) {
		let vm = this;
		vm.saving = undefined;
		vm.save = save;

		let init = () => {
			navigationService.syncTree({ tree: 'manage-csp', path: ["-1", $routeParams.id], forceReload: false });

			vm.page = {
				isBackOffice: $routeParams.id === "1",
				loading: true,
				navigation: [{
					active: false,
					alias: "edit",
					icon: "icon-document-dashed-line",
					name: "Edit",
					view: "/App_Plugins/CspManager/backoffice/manage-csp/manage/manage-csp.html",
					weight: 0,
					viewModel: null,
					badge: null
				},
				{
					active: true,
					alias: "evaluate",
					icon: "icon-lense",
					name:"Evaluate",
					view: "/App_Plugins/CspManager/backoffice/manage-csp/evaluate/evaluate.html",
					weight: 1,
					viewModel: null,
					badge: null
				}]
			};
			vm.page.name = `Manage ${vm.page.isBackOffice ? "Back Office" : "Front end"} CSP`;

			getDefinition(vm.page.isBackOffice);
		}

		function getDefinition(isBackOffice) {
			cspManagerResource.getDefinition(isBackOffice)
				.then(function (result) {
					vm.definition = result;
					vm.page.loading = false;
				}, function (error) {
					console.warn(error);
					notificationsService.error('Error', 'Failed to retrieve definition for CSP Manager');
				});
		}

		function save() {
			vm.saving = "waiting";
			cspManagerResource.saveDefinition(vm.definition)
				.then(function (result) {
					vm.definition = result;
					vm.saving = "success";
				}, function (error) {
					vm.saving = "failed";
					console.warn(error);
					notificationsService.error('Error', 'Failed to save definition for CSP Manager');
				});
		}

		init();
	}

	angular.module("umbraco").controller("cspManagerEditController", controller);
})();