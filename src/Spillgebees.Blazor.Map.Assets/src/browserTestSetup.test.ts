import { afterEach, describe, expect, it, vi } from "vitest";

const originalCi = process.env.CI;

describe("browser test setup", () => {
  afterEach(() => {
    // arrange
    process.env.CI = originalCi;
    vi.resetModules();
  });

  it("uses buildable apps for Playwright web servers by default", async () => {
    // arrange
    process.env.CI = "";

    // act
    const commands = await getWebServerCommands();

    // assert
    expect(commands).toHaveLength(3);
    expect(commands.every((command) => command.startsWith("dotnet run --project"))).toBe(true);
    expect(commands.every((command) => !command.includes("--no-build"))).toBe(true);
    expect(commands.every((command) => !command.includes("--configuration Release"))).toBe(true);
  });

  it("uses the same Playwright web server commands in CI", async () => {
    // arrange
    process.env.CI = "true";

    // act
    const commands = await getWebServerCommands();

    // assert
    expect(commands).toHaveLength(3);
    expect(commands.every((command) => command.startsWith("dotnet run --project"))).toBe(true);
    expect(commands.every((command) => !command.includes("--no-build"))).toBe(true);
    expect(commands.every((command) => !command.includes("--configuration Release"))).toBe(true);
  });

  it("keeps browser artifacts inside the browser tests folder", async () => {
    // arrange
    process.env.CI = "";

    // act
    vi.resetModules();
    const { default: playwrightConfig } = await import("../playwright.config");

    // assert
    expect(playwrightConfig.outputDir).toBe("./tests/browser/test-results");
    expect(playwrightConfig.reporter).toContainEqual([
      "html",
      { open: "never", outputFolder: "./tests/browser/playwright-report" },
    ]);
  });
});

async function getWebServerCommands(): Promise<string[]> {
  vi.resetModules();
  const { default: playwrightConfig } = await import("../playwright.config");
  const webServers = Array.isArray(playwrightConfig.webServer)
    ? playwrightConfig.webServer
    : [playwrightConfig.webServer];

  return webServers.map((server) => server?.command ?? "");
}
