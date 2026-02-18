import { resolve } from "node:path";
import { defineConfig } from "vite";
import { viteStaticCopy } from "vite-plugin-static-copy";

export default defineConfig(({ mode }) => {
  const isProduction = mode === "production";

  return {
    build: {
      lib: {
        entry: resolve(import.meta.dirname!, "src/index.ts"),
        formats: ["es"],
        fileName: () => "Spillgebees.Blazor.Map.lib.module.js",
      },
      outDir: "dist",
      emptyOutDir: true,
      sourcemap: !isProduction,
      minify: isProduction,
      target: "es2022",
      rollupOptions: {
        output: {
          assetFileNames: (assetInfo) => {
            if (assetInfo.names?.some((name) => name.endsWith(".css"))) {
              return "Spillgebees.Blazor.Map.lib.module.css";
            }
            return "[name][extname]";
          },
        },
      },
    },
    plugins: [
      viteStaticCopy({
        targets: [
          {
            src: "node_modules/leaflet/dist/images/*",
            dest: ".",
          },
        ],
      }),
    ],
  };
});
