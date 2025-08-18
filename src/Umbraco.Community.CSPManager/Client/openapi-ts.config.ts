import { defineConfig } from '@hey-api/openapi-ts';
import { defaultPlugins } from '@hey-api/openapi-ts';


process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0'; // Disable SSL verification for local development

export default defineConfig({
	input: 'https://localhost:44370/umbraco/swagger/csp/swagger.json',
	output: {
		format: 'prettier',
		lint: 'eslint',
		path: 'src/api',
	},
	plugins: [
		...defaultPlugins,
		{
			name: '@hey-api/client-fetch',
			exportFromIndex: true,
			throwOnError: true,
		},
		{
			name: '@hey-api/typescript',
			enums: 'typescript',
			readOnlyWriteOnlyBehavior: 'off',
		},
		{
			name: '@hey-api/sdk',
			asClass: true,
		},
	],
});