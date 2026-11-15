export default {
    "uSync": {
			"CspDefinition": "CSP"
    },
		"usyncpublish": {
			"CspDefinition": "CSP"
	},
	cspManager: {
		noDefinition: 'No CSP definition available',
	},
	cspManagerDomainPolicy: {
		addAction: 'Add Domain Policy',
		addModalHeadline: 'Add Domain Policy',
		addModalEmpty:
			'No content available. Either every content node with a hostname already has a CSP policy, or no hostnames are configured under Culture and Hostnames.',
		addModalLoadFailed: 'The content nodes could not be loaded.',
		createUnavailable:
			'A domain policy cannot be created for this content node. It no longer has a hostname in Culture and Hostnames, was deleted or moved to the recycle bin, or already has a CSP policy.',
		deletedContent: 'Deleted content (%0%…)',
		workspaceHeadline: '%0% CSP Management',
		orphanedPolicyInfo:
			'This content node has no hostname in Culture and Hostnames (or was deleted), so this policy applies to no requests. Assign a hostname to the node to bring it back into use, or delete the policy.',
		domainInfoHeadline: 'Linked content',
		appliesTo: 'Applies to requests on:',
		openContent: 'Open content item',
		statusLabel: 'Domain policy',
		statusActive: 'Use Domain Policy',
		statusInactiveFallback: 'Use Frontend Policy',
		statusInactiveNoHeader: 'No CSP header',
		summaryActive: 'Every hostname of %0% uses this policy.',
		summaryActiveReportOnly: 'Every hostname of %0% uses this policy, in report-only mode.',
		summaryFallback: 'Every hostname of %0% uses the Frontend policy.',
		summaryNoHeader: 'Every hostname of %0% gets no CSP header.',
		treeInactive: '%0% (inactive)',
		treeNoHostname: '%0% (no hostname)',
		deleteAction: 'Delete domain policy',
		deleting: 'Deleting...',
		deleteConfirmHeadline: 'Delete domain policy',
		deleteConfirmContent: 'Delete the CSP policy for %0%? Requests on its hostnames will use the Frontend policy. This cannot be undone.',
		deletedHeadline: 'Policy deleted',
		deletedMessage: 'The CSP policy for %0% has been deleted.',
		deleteFailedHeadline: 'Delete failed',
		deleteFailedMessage: 'An error occurred while deleting the CSP policy.',
		deleteUnsaved: 'Only a saved domain policy can be deleted.',
	},
};
