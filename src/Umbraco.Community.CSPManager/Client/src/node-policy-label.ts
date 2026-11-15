import type { UmbLocalizationController } from '@umbraco-cms/backoffice/localization-api';

type Localize = Pick<UmbLocalizationController, 'term'>;

/**
 * The label of a node policy: the name of the content node it belongs to, or a "deleted content"
 * label when the node no longer exists, followed by its culture when it is for one culture. User
 * content, so only ever render it as text.
 */
export const nodePolicyLabel = (
	localize: Localize,
	policy: { contentName?: string | null; contentKey?: string | null; culture?: string | null },
): string => {
	const name =
		policy.contentName || localize.term('cspManagerNodePolicy_deletedContent', (policy.contentKey ?? '').slice(0, 8));

	return policy.culture ? localize.term('cspManagerNodePolicy_labelWithCulture', name, policy.culture) : name;
};

/** "All cultures", or the culture's ISO code. */
export const cultureLabel = (localize: Localize, culture: string | null | undefined): string =>
	culture || localize.term('cspManagerNodePolicy_allCultures');
