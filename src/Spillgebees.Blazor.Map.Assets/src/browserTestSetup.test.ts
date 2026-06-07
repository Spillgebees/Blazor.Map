import { afterEach, describe, expect, it, vi } from "vitest";

const originalCi = process.env.CI;
const originalNoBuild = process.env.SGB_PLAYWRIGHT_NO_BUILD;

describe("browser test setup", () => {
  afterEach(() => {
    // arrange
    process.env.CI = originalCi;
    process.env.SGB_PLAYWRIGHT_NO_BUILD = originalNoBuild;
    vi.resetModules();
  });

  it("uses buildable apps for Playwright web servers by default", async () => {
    // arrange
    process.env.CI = "";
    process.env.SGB_PLAYWRIGHT_NO_BUILD = "";

    // act
    const commands = await getWebServerCommands();

    // assert
    expect(commands).toHaveLength(2);
    expect(commands.every((command) => command.startsWith("dotnet run --project"))).toBe(true);
    expect(commands.every((command) => !command.includes("--no-build"))).toBe(true);
    expect(commands.every((command) => !command.includes("--configuration Release"))).toBe(true);
  });

  it("uses prebuilt Release apps for Playwright web servers in no-build mode", async () => {
    // arrange
    process.env.CI = "";
    process.env.SGB_PLAYWRIGHT_NO_BUILD = "1";

    // act
    const commands = await getWebServerCommands();

    // assert
    expect(commands).toHaveLength(2);
    expect(
      commands.every((command) => command.startsWith("dotnet run --no-build --configuration Release --project")),
    ).toBe(true);
    expect(commands.every((command) => command.includes("--no-build"))).toBe(true);
    expect(commands.every((command) => command.includes("--configuration Release"))).toBe(true);
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
