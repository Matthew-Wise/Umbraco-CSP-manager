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
	cspManagerNodePolicy: {
		addAction: 'Add Node Policy',
		addModalHeadline: 'Add Node Policy',
		addModalContentLabel: 'Content',
		addModalContentDescription: 'The site or page the policy is for.',
		addModalSites: 'Or pick a site:',
		addModalCultureDescription:
			'One language, or every language of the content. A policy for one language wins over the policy for all cultures.',
		addModalExists: 'This content already has a policy for %0%. It can only have one.',
		addModalOpenExisting: 'Open that policy',
		addModalContinue: 'Continue',
		addModalLoadFailed: 'The sites and their policies could not be loaded.',
		createUnavailable:
			'A node policy cannot be created here. The content was deleted or moved to the recycle bin, the culture is not one of the languages of the site, or the content already has a CSP policy for this culture.',
		draftLoadFailed: 'The new policy could not be loaded. Try again in a moment.',
		deletedContent: 'Deleted content (%0%…)',
		labelWithCulture: '%0% (%1%)',
		allCultures: 'All cultures',
		cultureLabel: 'Culture',
		workspaceHeadline: '%0% CSP Management',
		orphanedPolicyInfo:
			'This policy applies to no requests: its content was deleted or moved to the recycle bin, or its culture is no longer one of the languages of the site. Restore the content or the language to bring the policy back into use, or delete the policy.',
		nodeInfoHeadline: 'Linked content',
		hostnamesLabel: 'Hostnames',
		openContent: 'Open content item',
		inheritanceLabel: 'Policy type',
		inheritanceReplace: 'Complete policy',
		inheritanceReplaceDescription:
			'Replaces the policy the content inherits: only the sources and settings of this policy apply.',
		addModalReplaceStart: 'It starts as a copy of the policy the content inherits today.',
		inheritanceExtend: 'Inherit and add',
		inheritanceExtendDescription:
			'Keeps the policy the content inherits and adds the sources of this policy to it. Sources can only be added, never removed.',
		appliesToLabel: 'Applies to',
		appliesToDescription: 'Whether the pages below this content get the policy as well.',
		appliesToDescendants: 'This content and the pages below it',
		appliesToNodeOnly: 'This content only',
		scopeDescendants: '%0% and the pages below it',
		extendSourcesIntro:
			'Only the extra sources for this content. They are added to the policy it inherits: the nearest policy further up the content tree, or the Frontend policy.',
		extendSettingsInfo:
			'Report-only mode, reporting and upgrade-insecure-requests are not set here: they come from the policy this one adds to.',
		statusLabel: 'Node policy',
		statusActive: 'Use this policy',
		statusInactiveFallback: 'Use the inherited policy',
		statusInactiveNoHeader: 'No CSP header',
		summaryActive: 'This policy applies to %0%.',
		summaryActiveReportOnly: 'This policy applies to %0%, in report-only mode.',
		summaryExtendActive: 'The sources of this policy are added to the policy inherited by %0%.',
		summaryFallback:
			'This policy is switched off. Requests for %0% get the inherited policy: the nearest policy further up the content tree, or the Frontend policy.',
		summaryNoHeader: 'This policy is switched off, so requests for %0% get no CSP header.',
		treeInactive: '%0% (inactive)',
		treeOrphaned: '%0% (not in use)',
		deleteAction: 'Delete node policy',
		deleting: 'Deleting...',
		deleteConfirmHeadline: 'Delete node policy',
		deleteConfirmContent:
			'Delete the CSP policy for %0%? Requests for it will get the policy it inherits: the nearest policy further up the content tree, or the Frontend policy. This cannot be undone.',
		deletedHeadline: 'Policy deleted',
		deletedMessage: 'The CSP policy for %0% has been deleted.',
		deleteFailedHeadline: 'Delete failed',
		deleteFailedMessage: 'An error occurred while deleting the CSP policy.',
		deleteUnsaved: 'Only a saved node policy can be deleted.',
	},
	cspManagerEffective: {
		viewLabel: 'Effective',
		headline: 'What a request gets',
		intro:
			'The policy a request for %0% gets today. It is worked out from the saved policies: the policy of the content itself, the policies it inherits further up the content tree, and the Frontend policy.',
		unsaved: 'Changes that are not saved yet are not included.',
		notSavedYet: 'This policy is not saved yet, so this is what the content gets without it.',
		cultureDescription: 'A request always has a culture. The default language is used when the hostname does not set one.',
		defaultLanguage: '%0% (default)',
		loadFailed: 'The effective policy could not be loaded.',
		noHeader: 'No CSP header is sent: the policy that applies is switched off.',
		noHeaderEmpty: 'No CSP header is sent: the policy that applies has no sources yet.',
		headerLabel: 'Header',
		headerNote:
			'Built from the saved policies. Nonces, and anything code on the site changes for a request, are added to each response and are not shown.',
		directivesLabel: 'Directives',
		fromLabel: 'Comes from',
		fromFrontend: 'The Frontend policy',
		fromNode: 'The policy of %0%',
		fromCombined: 'The policy of %0%, added to the policy it inherits',
		openPolicy: 'Open that policy',
		evaluatesEffective:
			'An inherit-and-add policy is not a policy on its own, so this evaluates what a request for %0% gets from the saved policies.',
	},
};
