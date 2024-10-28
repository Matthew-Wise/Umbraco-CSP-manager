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

        let containsPreFetch = false;
        let preFetchSrcKey = 'prefetch-src';

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

                // Prefetch-src is deprecated but doesn't get flagged in the CSP-Evaluator
                // so we need to take matters into our own hands!
                if (containsPreFetch) {
                    const preFetchDeprecationWarning = {
                        description: "The prefetch-src is <a href=\"https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy/prefetch-src\" target=\"_blank\">deprecated</a> and no longer recommended.",
                        directive: preFetchSrcKey,
                        severity: 20,
                        type: 309 // DEPRECATED_DIRECTIVE
                    };
                    if (!findings[preFetchSrcKey]) {
                        findings[preFetchSrcKey] = {
                            issues: [preFetchDeprecationWarning],
                            severity: preFetchDeprecationWarning.severity
                        }
                    } else {
                        findings[preFetchSrcKey].issues.push(preFetchDeprecationWarning);
                    }
                }

                vm.findings = findings;
                //console.log(findings);
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
                cspValue += key + " " + cspDirectives[key].join(" ") + "; ";

                // Do we have the prefetch-src directive here?
                if (key == preFetchSrcKey) {
                    containsPreFetch = true;
                }
            }

            return cspValue;

        }
    }

    angular.module("umbraco").controller("cspManagerEvaluateController", controller);
})();