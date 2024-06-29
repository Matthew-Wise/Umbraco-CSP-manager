import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
	input: 'https://localhost:44378/umbraco/swagger/csp/swagger.json',
	output: {
		format: 'prettier',
		lint: 'eslint',
		path: 'src/api',
	},
	types: {
		enums: 'typescript',
	},
	schemas: false,
});
