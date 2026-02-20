import { resolve } from "node:path";
import libAssetsPlugin from "@laynezh/vite-plugin-lib-assets";
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
      outDir: resolve(import.meta.dirname!, "../Spillgebees.Blazor.Map/wwwroot"),
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
      // include css/js imported assets
      libAssetsPlugin({
        name: "[name].[ext]",
        outputPath: ".",
      }),
      // include runtime referenced assets
      viteStaticCopy({
        targets: [
          {
            src: [
              "node_modules/leaflet/dist/images/marker-icon-2x.png",
              "node_modules/leaflet/dist/images/marker-shadow.png",
            ],
            dest: ".",
          },
        ],
      }),
    ],
  };
});
