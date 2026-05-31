import { defineConfig, devices } from "@playwright/test";

const requestedProjects = getRequestedProjects(process.argv);

export default defineConfig({
  testDir: "./tests/browser",
  timeout: 30_000,
  fullyParallel: true,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    screenshot: "only-on-failure",
    trace: "retain-on-failure",
    video: "retain-on-failure",
  },
  webServer: [
    {
      command:
        "dotnet run --project ../Spillgebees.Blazor.Map.Docs/Spillgebees.Blazor.Map.Docs.csproj --urls http://127.0.0.1:5002",
      url: "http://127.0.0.1:5002",
      reuseExistingServer: true,
      timeout: 120_000,
    },
    {
      command:
        "dotnet run --project ../Spillgebees.Blazor.Map.IntegrationTests/Spillgebees.Blazor.Map.IntegrationTests.csproj --urls http://127.0.0.1:5012",
      url: "http://127.0.0.1:5012",
      reuseExistingServer: false,
      timeout: 120_000,
    },
  ].filter((server) => shouldStartServer(server.url, requestedProjects)),
  projects: [
    {
      name: "docs",
      testMatch: /docs\/.*\.spec\.ts/,
      use: {
        ...devices["Desktop Chrome"],
        baseURL: "http://127.0.0.1:5002",
      },
    },
    {
      name: "integration",
      testMatch: /integration\/.*\.spec\.ts/,
      use: {
        ...devices["Desktop Chrome"],
        baseURL: "http://127.0.0.1:5012",
      },
    },
  ],
});

function getRequestedProjects(args: string[]): Set<string> {
  const projects = new Set<string>();

  for (let index = 0; index < args.length; index += 1) {
    const arg = args[index];

    if (arg === "--project" && args[index + 1]) {
      projects.add(args[index + 1]);
      index += 1;
      continue;
    }

    if (arg.startsWith("--project=")) {
      projects.add(arg.slice("--project=".length));
    }
  }

  return projects;
}

function shouldStartServer(url: string, projects: Set<string>): boolean {
  if (projects.size === 0) {
    return true;
  }

  return (
    (url === "http://127.0.0.1:5002" && projects.has("docs")) ||
    (url === "http://127.0.0.1:5012" && projects.has("integration"))
  );
}
