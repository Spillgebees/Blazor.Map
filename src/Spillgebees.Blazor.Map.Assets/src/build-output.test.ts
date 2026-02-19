import { execSync } from "node:child_process";
import { existsSync, readFileSync, statSync } from "node:fs";
import { resolve } from "node:path";
import { beforeAll, describe, expect, it } from "vitest";

const distDir = resolve(import.meta.dirname!, "../../Spillgebees.Blazor.Map/wwwroot");

type BuildName = "dev" | "prod";

const buildConfigs: {
  name: BuildName;
  script: string;
  expectJsSourcemap: boolean;
  expectCssSourcemap: boolean;
  expectMinifiedSmaller?: boolean;
}[] = [
  {
    name: "dev",
    script: "build:dev",
    expectJsSourcemap: true,
    expectCssSourcemap: false,
  },
  {
    name: "prod",
    script: "build:prod",
    expectJsSourcemap: false,
    expectCssSourcemap: false,
    expectMinifiedSmaller: true,
  },
];

type BuildResult = {
  jsContent: string;
  cssContent: string;
  jsSize: number;
  hasJsFile: boolean;
  hasCssFile: boolean;
  hasJsMapFile: boolean;
  hasCssMapFile: boolean;
  leafletImagesExist: Record<string, boolean>;
};

const results = new Map<BuildName, BuildResult>();

beforeAll(() => {
  const cwd = resolve(import.meta.dirname!, "..");
  for (const cfg of buildConfigs) {
    execSync(`pnpm run clean && pnpm run ${cfg.script}`, {
      cwd,
      stdio: "pipe",
    });

    const jsFile = resolve(distDir, "Spillgebees.Blazor.Map.lib.module.js");
    const cssFile = resolve(distDir, "Spillgebees.Blazor.Map.lib.module.css");
    const jsMapFile = `${jsFile}.map`;
    const cssMapFile = `${cssFile}.map`;

    const leafletImages = ["layers-2x.png", "layers.png", "marker-icon-2x.png", "marker-icon.png", "marker-shadow.png"];

    const hasJsFile = existsSync(jsFile);
    const hasCssFile = existsSync(cssFile);
    const hasJsMapFile = existsSync(jsMapFile);
    const hasCssMapFile = existsSync(cssMapFile);
    const jsContent = hasJsFile ? readFileSync(jsFile, "utf-8") : "";
    const cssContent = hasCssFile ? readFileSync(cssFile, "utf-8") : "";
    const jsSize = hasJsFile ? statSync(jsFile).size : 0;
    const leafletImagesExist: Record<string, boolean> = {};

    for (const image of leafletImages) {
      leafletImagesExist[image] = existsSync(resolve(distDir, image));
    }

    results.set(cfg.name, {
      jsContent,
      cssContent,
      jsSize,
      hasJsFile,
      hasCssFile,
      hasJsMapFile,
      hasCssMapFile,
      leafletImagesExist,
    });
  }
}, 120_000);

describe.each(buildConfigs)("$name build (parametrized)", (cfg) => {
  const name = cfg.name;
  let res!: BuildResult;

  beforeAll(() => {
    const maybe = results.get(name);
    if (!maybe) throw new Error(`No build results for ${name}`);
    res = maybe;
  });

  it("should produce JS output file", () => {
    expect(res.hasJsFile).toBe(true);
  });

  it("should produce CSS output file", () => {
    expect(res.hasCssFile).toBe(true);
  });

  it(`${cfg.expectJsSourcemap ? "should" : "should NOT"} produce JS sourcemap file`, () => {
    expect(res.hasJsMapFile).toBe(cfg.expectJsSourcemap);
  });

  it(`${cfg.expectCssSourcemap ? "should" : "should NOT"} produce CSS sourcemap file`, () => {
    expect(res.hasCssMapFile).toBe(cfg.expectCssSourcemap);
  });

  it(`${cfg.expectJsSourcemap ? "should" : "should NOT"} contain sourceMappingURL reference in JS`, () => {
    if (cfg.expectJsSourcemap) {
      // biome-ignore lint/security/noSecrets: false positive
      expect(res.jsContent).toContain("//# sourceMappingURL=");
    } else {
      // biome-ignore lint/security/noSecrets: false positive
      expect(res.jsContent).not.toContain("//# sourceMappingURL=");
    }
  });

  it("should be valid ESM (contains export statements)", () => {
    expect(res.jsContent).toMatch(/export\s*\{/);
  });

  it("should export the 8 lifecycle hooks", () => {
    const expectedExports = [
      "beforeWebStart",
      "afterWebStarted",
      "beforeWebAssemblyStart",
      "afterWebAssemblyStarted",
      "beforeServerStart",
      "afterServerStarted",
      "beforeStart",
      "afterStarted",
    ];
    for (const n of expectedExports) expect(res.jsContent).toContain(n);
  });

  it("should contain .leaflet-container in CSS (leaflet base)", () => {
    expect(res.cssContent).toContain(".leaflet-container");
  });

  it("should contain .sgb-map-container in CSS (custom styles)", () => {
    expect(res.cssContent).toContain(".sgb-map-container");
  });

  it("should contain .sgb-map-dark in CSS (dark theme)", () => {
    expect(res.cssContent).toContain(".sgb-map-dark");
  });

  it("should contain .sgb-map-center-control in CSS (center control)", () => {
    expect(res.cssContent).toContain(".sgb-map-center-control");
  });

  it("should copy all 5 Leaflet images", () => {
    for (const [image, exists] of Object.entries(res.leafletImagesExist)) {
      expect(exists, `Expected Leaflet image ${image} to exist in build output`).toBe(true);
    }
  });

  if (cfg.expectMinifiedSmaller) {
    it("prod should produce minified JS smaller than dev JS", () => {
      const dev = results.get("dev");
      const prod = results.get("prod");
      expect(prod && dev && prod.jsSize < dev.jsSize).toBe(true);
    });
  }
});
