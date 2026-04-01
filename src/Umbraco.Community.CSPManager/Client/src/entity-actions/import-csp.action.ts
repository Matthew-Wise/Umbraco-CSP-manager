import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { umbOpenModal } from '@umbraco-cms/backoffice/modal';
import { CspConstants } from '@/constants';
import { UmbCspDefinitionRepository } from '../repository/csp-definition.repository.js';
import { UmbCspDirectivesRepository } from '../repository/csp-directives.repository.js';
import { IMPORT_CSP_MODAL } from '../modals/import-csp-modal.token.js';

const UNIQUE_TO_POLICY_TYPE: Record<string, (typeof CspConstants.policyTypes)[keyof typeof CspConstants.policyTypes]> =
	{
		[CspConstants.policyTypes.backoffice.value]: CspConstants.policyTypes.backoffice,
		[CspConstants.policyTypes.frontend.value]: CspConstants.policyTypes.frontend,
	};

export class UmbImportCspEntityAction extends UmbEntityActionBase<never> {
	override async execute() {
		const unique = this.args.unique as string;
		const policyType = UNIQUE_TO_POLICY_TYPE[unique] ?? CspConstants.policyTypes.frontend;
		const isBackOffice = policyType === CspConstants.policyTypes.backoffice;

		const definitionRepository = new UmbCspDefinitionRepository(this);
		const directivesRepository = new UmbCspDirectivesRepository(this);

		const [{ data: definition }, { data: directives }] = await Promise.all([
			definitionRepository.get(isBackOffice),
			directivesRepository.getAll(),
		]);

		if (!definition) return;

		const result = await umbOpenModal(this, IMPORT_CSP_MODAL, {
			data: {
				policyLabel: policyType.label,
				availableDirectives: directives ?? [],
			},
		}).catch(() => null);

		if (!result) return;

		const updatedDefinition = {
			...definition,
			sources: result.sources.map((s) => ({
				definitionId: definition.id,
				source: s.source,
				directives: s.directives,
			})),
			upgradeInsecureRequests: result.upgradeInsecureRequests,
			...(result.reportingDirective !== null
				? {
						reportingDirective: result.reportingDirective,
						reportUri: result.reportUri,
					}
				: {}),
		};

		const { error } = await definitionRepository.save(updatedDefinition);

		if (!error) {
			window.dispatchEvent(new CustomEvent('csp:definition-saved', { detail: { unique } }));
			history.pushState(null, '', `section/csp-manager/workspace/csp-policy/edit/${unique}`);
		}
	}
}

export { UmbImportCspEntityAction as api };
