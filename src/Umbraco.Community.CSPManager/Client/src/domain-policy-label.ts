import type { UmbLocalizationController } from '@umbraco-cms/backoffice/localization-api';

/**
 * The label of a domain policy: the name of the content node it belongs to, or a "deleted content"
 * label when the node no longer exists. User content, so only ever render it as text.
 */
export const domainPolicyLabel = (
	localize: Pick<UmbLocalizationController, 'term'>,
	policy: { contentName?: string | null; contentKey?: string | null },
): string =>
	policy.contentName || localize.term('cspManagerDomainPolicy_deletedContent', (policy.contentKey ?? '').slice(0, 8));
