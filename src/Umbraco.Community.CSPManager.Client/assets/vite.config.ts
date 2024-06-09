import { defineConfig } from 'vite';

export default defineConfig({
	build: {
		lib: {
			entry: 'src/index.ts', // your web component source file
			formats: ['es'],
		},
		outDir: '../wwwroot/App_Plugins/Umbraco.Community.CSPManager',
		emptyOutDir: true,
		sourcemap: true,
		rollupOptions: {
			external: [/^@umbraco/],
		},
	},
});
