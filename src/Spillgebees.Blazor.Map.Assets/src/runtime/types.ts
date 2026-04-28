import type { DotNet } from "@microsoft/dotnet-js-interop";
import type { RegisteredMapLayer } from "../interfaces/spillgebees";

export type LayerOrdering = RegisteredMapLayer["ordering"];

export type SceneMutation =
  | {
      kind: "addSource";
      sourceId: string;
      sourceSpec: Record<string, unknown>;
    }
  | {
      kind: "removeSource";
      sourceId: string;
    }
  | {
      kind: "setSourceData";
      sourceId: string;
      data: unknown;
    }
  | {
      kind: "setSourceDataAnimated";
      sourceId: string;
      data: unknown;
      animationDuration: number;
      animationEasing: string;
    }
  | {
      kind: "addLayer";
      layerId: string;
      layerSpec: Record<string, unknown>;
      beforeLayerId: string | null;
      ordering: LayerOrdering;
    }
  | {
      kind: "removeLayer";
      layerId: string;
    }
  | {
      kind: "moveLayer";
      layerId: string;
      beforeLayerId: string | null;
    }
  | {
      kind: "setPaintProperty";
      layerId: string;
      propertyName: string;
      propertyValue: unknown;
    }
  | {
      kind: "setLayoutProperty";
      layerId: string;
      propertyName: string;
      propertyValue: unknown;
    }
  | {
      kind: "setFilter";
      layerId: string;
      filter: unknown;
    }
  | {
      kind: "setLayerZoomRange";
      layerId: string;
      minZoom: number;
      maxZoom: number;
    }
  | {
      kind: "wireLayerEvents";
      layerId: string;
      dotNetRef: DotNet.DotNetObject;
      onClick: boolean;
      onMouseEnter: boolean;
      onMouseLeave: boolean;
    }
  | {
      kind: "unregisterLayerEvents";
      layerId: string;
    }
  | {
      kind: "setVisibilityGroup";
      groupId: string;
      visible: boolean;
      targets: Array<{
        styleId: string;
        layerIds: string[];
      }>;
    }
  | {
      kind: "removeVisibilityGroup";
      groupId: string;
    }
  | {
      kind: "reconcileOrdering";
    };

export interface SceneMutationBatch {
  mutations: SceneMutation[];
}
