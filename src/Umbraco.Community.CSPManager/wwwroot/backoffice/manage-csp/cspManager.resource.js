function resource($http, umbRequestHelper)
{
	return {
		getDefinition: function (isBackOffice) {
			return umbRequestHelper.resourcePromise(
				$http.get(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"GetDefinition",
						{isBackOffice: isBackOffice}
					)),
				"Failed to retrieve definition for CSP Manager"
			);
		},
		saveDefinition: function (definition) {
			return umbRequestHelper.resourcePromise(
				$http.post(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"SaveDefinition"
					),
					definition
				),
				"Failed to save definition for CSP Manager"
			);
		},
		getCspDirectiveOptions: function () {
			return umbRequestHelper.resourcePromise(
				$http.get(
					umbRequestHelper.getApiUrl(
						"cspManagerBaseUrl",
						"GetCspDirectiveOptions"
					)),
				"Failed to retrieve CSP Directive options"
			);
		},
	}
}
angular.module("umbraco.resources").factory("cspManagerResource", resource);	