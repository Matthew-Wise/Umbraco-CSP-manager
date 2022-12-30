(function(){
	"use strict";
	
	function controller($scope, $routeParams, navigationService, notificationsService, cspManagerResource, localizationService, overlayService)
	{
		let vm = this;
		
		let init = () => {
			navigationService.syncTree({ tree: 'manage-csp', path: ["-1", $routeParams.id], forceReload: false });

			vm.page = {
				isBackOffice: $routeParams.id === "1",
			};
			vm.page.name =`Manage ${ vm.page.isBackOffice ? "Back Office" : "Front end"} CSP`;

			getCspDirectiveOptions();

			getDefinition(vm.page.isBackOffice);
		}

		function getDefinition(isBackOffice) {
            cspManagerResource.getDefinition(isBackOffice)
                .then(function (result) {
                    vm.definition = result;
                    vm.loading = false; 
                }, function (error) {
                    console.warn(error);
                    notificationsService.error('Error', 'Failed to retrieve definition for CSP Manager');
                });
        }

		function getCspDirectiveOptions() {
			cspManagerResource.getCspDirectiveOptions()
				.then(function (result) {
					var newItems = [];
					for (var i = 0; i < result.length; i++) {
						newItems.push({ id: result[i], sortOrder: 0, value: result[i] });
					}

					vm.cspDirectiveOptions = newItems;
				}, function (error) {
					console.warn(error);
					notificationsService.error('Error', 'Failed to retrieve CSP Directives');
				});
		}

		vm.saving = undefined;
		vm.save = save;
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

		vm.addSource = addSource;
		function addSource() {
			vm.definition.Sources.push({
				DefinitionId: vm.definition.Id,
				Source: "",
				Directives: []
			});

			vm.expanded.push(vm.definition.Sources.length - 1);
		}
		
		vm.updateDirectiveOnSource = updateDirectiveOnSource;
		function updateDirectiveOnSource(source, directive) {
			if(source.Directives.includes(directive)) {
				source.Directives = source.Directives.filter(e => e !== directive);
			} else {
				source.Directives.push(directive);
			}
		}

		vm.expanded = [];
		vm.expandAccordion = expandAccordion;
		function expandAccordion(event, sourceIndex) {
			if(event.target.nextElementSibling == undefined) {
				return;
			}
			if(vm.expanded.includes(sourceIndex)) {
				vm.expanded = vm.expanded.filter(e => e !== sourceIndex);
			} else {
				vm.expanded.push(sourceIndex);
			}
		}

		vm.deleteSource = deleteSource;
		function deleteSource(sourceIndex) {

			localizationService.localizeMany(["general_delete", "defaultdialogs_confirmdelete", "contentTypeEditor_yesDelete"]).then(function (data) {
				const overlay = {
					title: data[0],
					content: data[1],
					submitButtonLabel: data[2],
					close: function () {
						overlayService.close();
					},
					submit: function () {
						vm.definition.Sources.splice(sourceIndex, 1);
						vm.expanded = vm.expanded.filter(e => e !== sourceIndex);

						overlayService.close();
					}
				};
	
				overlayService.confirmDelete(overlay);
			});
		}

		vm.copySource = copySource;
		function copySource(sourceIndex) {
			vm.definition.Sources.push(
				{
					DefinitionId: vm.definition.Sources[sourceIndex].Id,
					Source: vm.definition.Sources[sourceIndex].Source,
					Directives: [...vm.definition.Sources[sourceIndex].Directives]
				}
			);
		}

		init();
	}
	
	angular.module("umbraco").controller("cspManagerEditController", controller);	
})();