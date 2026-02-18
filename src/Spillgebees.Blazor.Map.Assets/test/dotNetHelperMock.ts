import type { DotNet } from "@microsoft/dotnet-js-interop";
import { vi } from "vitest";

export function createMockDotNetHelper(): DotNet.DotNetObject {
  return {
    invokeMethodAsync: vi.fn().mockResolvedValue(undefined),
    dispose: vi.fn(),
    serializeAsArg: vi.fn(),
  } as unknown as DotNet.DotNetObject;
}
