(function () {
	"use strict";

	function controller($scope) {
		let vm = this;
		vm.definition = $scope.model;
		vm.evaluateCsp = evaluateCsp;
		vm.evaluating = "";
		vm.issueColor = (severity) => {
			if (severity == 50 || severity == 30) {
				return { "text-warning": true };
			}
			if (severity == 10 || severity == 40) {
				return { "text-error": true };
			}

			return { "color-grey": true };

		}
		vm.icon = (severity) => {
			switch (parseInt(severity)) {
				//maybe
				case 40:
				case 50:
				case 20: //syntax
					return "info";
				//issue
				case 30:
				case 10: return "forbidden";
				default: "info";
			}
		}

		function evaluateCsp() {

			vm.evaluating = "waiting";
			vm.findings = [];

			let cspValue = getCspString();
			if (cspValue != '') {
				vm.csp = cspValue
				const parsed = new CspParser(cspValue).csp;
				var evaluator = new CspEvaluator(parsed);

				var findings = {};
				evaluator.evaluate().forEach((finding) => {
					if (!findings[finding.directive]) {
						findings[finding.directive] = {
							issues: [finding],
							severity: finding.severity
						}
					}
					else {
						findings[finding.directive].issues.push(finding)
						if (finding.severity < findings[finding.directive].severity) {
							findings[finding.directive].severity = finding.severity
						}
					}
				});

				vm.findings = findings;
				console.log(findings);
			}
			vm.evaluating = "";
		}

		function getCspString() {
			let cspDirectives = {};
			vm.definition.Sources.forEach(({ Source, Directives }) => {
				if (Directives) {
					Directives.forEach(
						(name) => (cspDirectives[name] = cspDirectives[name]
							? [Source, ...cspDirectives[name]]
							: [Source]));
				}
			});

			var cspValue = '';
			for (var key in cspDirectives) {
				cspValue += key + " " + cspDirectives[key].join(" ") + "; "
			}

			return cspValue;

		}
	}

	angular.module("umbraco").controller("cspManagerEvaluateController", controller);
})();