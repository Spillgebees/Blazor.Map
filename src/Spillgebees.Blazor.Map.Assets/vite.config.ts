import { resolve } from "node:path";
import { defineConfig } from "vite";

export default defineConfig(({ mode }) => {
  const isProduction = mode === "production";

  return {
    build: {
      lib: {
        entry: resolve(import.meta.dirname!, "src/index.ts"),
        formats: ["es"],
        fileName: () => "Spillgebees.Blazor.Map.lib.module.js",
      },
      outDir: resolve(import.meta.dirname!, "../Spillgebees.Blazor.Map/wwwroot"),
      emptyOutDir: true,
      sourcemap: true,
      minify: isProduction,
      target: "es2022",
      rolldownOptions: {
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
  };
});
