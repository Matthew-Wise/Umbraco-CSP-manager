import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
  resolve: {
    alias: {
      "@": resolve(__dirname, "./src"),
    },
  },
  build: {
    lib: {
      entry: "src/bundle.manifests.ts", // Bundle registers one or more manifests
      formats: ["es"],
      fileName: "umbraco-community-csp-manager",
    },
    outDir: "../wwwroot/App_Plugins/UmbracoCommunityCSPManager", // your web component will be saved in this location
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
    base: "/App_Plugins/UmbracoCommunityCSPManager/", // the base path of the app in the browser (used for assets)
});
