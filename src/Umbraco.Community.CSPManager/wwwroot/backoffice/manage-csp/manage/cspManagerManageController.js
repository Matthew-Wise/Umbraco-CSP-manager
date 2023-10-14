(function () {
    "use strict";

    function controller($scope, cspManagerResource, localizationService, overlayService) {
        let vm = this;
        vm.definition = $scope.model;
        vm.addSource = addSource;
        vm.updateDirectiveOnSource = updateDirectiveOnSource;
        vm.expanded = [];
        vm.expandAccordion = expandAccordion;
        vm.deleteSource = deleteSource;
        vm.copySource = copySource;
        vm.changeReporting = (directive) => vm.definition.ReportingDirective = directive;
        vm.reportingDirectives = [
            {
                value: null,
                label:"Disabled"
            },
            {
                value: "report-to",
                label: "report-to"
            },
            {
                value: "report-uri",
                label: "report-uri"
            }];
        vm.tabs = [{
            id: 0,
            alias: "Sources",
            label: "Sources",
            active: true
        },
        {
            id: 1,
            alias: "Settings",
            label: "Settings",
            active: false
        }];

        vm.changeTab = (changedTab) => {
            vm.tabs.forEach(function (tab) {
                tab.active = false;
            });

            changedTab.active = true;
            vm.tab = changedTab.alias;
        };

        vm.tab = vm.tabs[0].alias

        function init() {
            cspManagerResource.getCspDirectiveOptions()
                .then(function (result) {
                    let newItems = [];
                    for (let i = 0; i < result.length; i++) {
                        newItems.push({
                            id: result[i],
                            sortOrder: 0,
                            value: result[i],
                        });
                    }

                    vm.cspDirectiveOptions = newItems;
                }, function (error) {
                    console.warn(error);
                    notificationsService.error('Error', 'Failed to retrieve CSP Directives');
                });
        }

        function addSource() {
            vm.definition.Sources.push({
                DefinitionId: vm.definition.Id,
                Source: "",
                Directives: []
            });

            vm.expanded.push(vm.definition.Sources.length - 1);
        }


        function updateDirectiveOnSource(source, directive) {
            if (source.Directives.includes(directive)) {
                source.Directives = source.Directives.filter(e => e !== directive);
            } else {
                source.Directives.push(directive);
            }
        }

        function expandAccordion(event, sourceIndex) {
            if (event.target.nextElementSibling == undefined) {
                return;
            }
            if (vm.expanded.includes(sourceIndex)) {
                vm.expanded = vm.expanded.filter(e => e !== sourceIndex);
            } else {
                vm.expanded.push(sourceIndex);
            }
        }

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

    angular.module("umbraco").controller("cspManagerManageController", controller);
})();