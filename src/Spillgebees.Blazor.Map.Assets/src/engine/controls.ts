// Control execution for the ops channel (control.set / control.remove /
// control.content / control.removeContent). Native MapLibre controls are created from
// data; custom shells (legend/panel/content) adopt Blazor-rendered DOM resolved by
// convention: a `data-sgb-control-placeholder="<controlId>"` element inside the map
// container whose first element child is the content root. Panel open/close state
// surfaces through an engine event handler id instead of a DotNet reference.

import {
  FullscreenControl,
  GeolocateControl,
  type IControl,
  type Map as MapLibreMap,
  NavigationControl,
  ScaleControl,
  TerrainControl,
} from "maplibre-gl";
import { CenterControl } from "../controls/centerControl";
import { ContentControl } from "../controls/contentControl";
import { LegendControl } from "../controls/legendControl";
import { PanelControl } from "../controls/panelControl";
import type { IContentMapControl, ILegendMapControl, IPanelMapControl } from "../interfaces/controls";
import type { ControlContentEvents, ControlData } from "./ops";

export interface ControlsController {
  set(control: ControlData): void;
  remove(id: string): void;
  setContent(id: string, events?: ControlContentEvents | null): void;
  removeContent(id: string): void;
  /** Attached control ids in render order (for tests/diagnostics). */
  ids(): string[];
  dispose(): void;
}

interface CustomControlEntry {
  kind: string;
  control: LegendControl | PanelControl | ContentControl;
}

interface NativeControlEntry {
  kind: string;
  signature: string;
  control: IControl;
}

const CUSTOM_KINDS = new Set(["legend", "panel", "content"]);

function findByAttribute(container: HTMLElement, attribute: string, id: string): Element | null {
  for (const element of container.querySelectorAll(`[${attribute}]`)) {
    if (element.getAttribute(attribute) === id) {
      return element;
    }
  }

  return null;
}

function nativeSignature(control: ControlData): string {
  return JSON.stringify([
    control.kind,
    control.showCompass,
    control.showZoom,
    control.unit,
    control.trackUser,
    control.sourceId,
    control.events?.click,
  ]);
}

function createNativeControl(
  map: MapLibreMap,
  control: ControlData,
  emit: (handlerId: number, payload: unknown) => void,
): IControl | null {
  switch (control.kind) {
    case "navigation":
      return new NavigationControl({
        showCompass: control.showCompass ?? undefined,
        showZoom: control.showZoom ?? undefined,
      });
    case "scale":
      return new ScaleControl({ unit: control.unit as "metric" | "imperial" | "nautical" | undefined });
    case "fullscreen":
      return new FullscreenControl();
    case "geolocate":
      return new GeolocateControl({ trackUserLocation: control.trackUser ?? undefined });
    case "terrain":
      if (!control.sourceId || !map.getSource(control.sourceId)) {
        // biome-ignore lint/suspicious/noConsole: explicit control diagnostics for terrain source mismatches
        console.warn(
          `[Spillgebees.Map] terrain control '${control.controlId}' ignored because source '${control.sourceId}' was not found.`,
        );
        return null;
      }

      return new TerrainControl({ source: control.sourceId });
    case "center": {
      const clickHandlerId = control.events?.click;
      return new CenterControl(clickHandlerId != null ? () => emit(clickHandlerId, {}) : undefined);
    }
    default:
      return null;
  }
}

export function createControlsController(
  map: MapLibreMap,
  container: HTMLElement,
  emit: (handlerId: number, payload: unknown) => void,
): ControlsController {
  // definitions in set order — set order doubles as declaration order for sorting
  const definitions = new Map<string, ControlData>();
  const nativeControls = new Map<string, NativeControlEntry>();
  const customControls = new Map<string, CustomControlEntry>();
  const attached: { id: string; control: IControl }[] = [];

  function resolveContentElements(id: string): { placeholder: HTMLElement; content: HTMLElement } | null {
    const placeholder = findByAttribute(container, "data-sgb-control-placeholder", id);
    const content = placeholder?.firstElementChild;
    if (!(placeholder instanceof HTMLElement) || !(content instanceof HTMLElement)) {
      // biome-ignore lint/suspicious/noConsole: explicit control diagnostics for interop mismatches
      console.warn(`[Spillgebees.Map] control content for '${id}' was not found in the map container.`);
      return null;
    }

    return { placeholder, content };
  }

  function panelStateShim(events: ControlContentEvents | null | undefined) {
    const handlerId = events?.openChanged;
    if (handlerId == null) {
      return null;
    }

    // PanelControl talks DotNet.DotNetObject; adapt it onto the engine event channel.
    return {
      invokeMethodAsync: (_method: string, isOpen: boolean) => {
        emit(handlerId, { open: isOpen });
        return Promise.resolve();
      },
    } as never;
  }

  function recompose(): void {
    for (const entry of attached) {
      map.removeControl(entry.control);
    }
    attached.length = 0;

    const ordered: { id: string; control: IControl; position: string; order: number; declarationOrder: number }[] = [];
    let declarationOrder = 0;
    for (const definition of definitions.values()) {
      const index = declarationOrder++;
      if (!definition.visible) {
        continue;
      }

      const control = resolveControl(definition);
      if (!control) {
        continue;
      }

      ordered.push({
        id: definition.controlId,
        control,
        position: definition.position,
        order: definition.order,
        declarationOrder: index,
      });
    }

    ordered.sort((left, right) => {
      if (left.position !== right.position) {
        // cross-position ordering is irrelevant; stable sort preserves declaration order across buckets
        return 0;
      }

      if (left.order !== right.order) {
        return left.order - right.order;
      }

      if (left.declarationOrder !== right.declarationOrder) {
        return left.declarationOrder - right.declarationOrder;
      }

      return left.id.localeCompare(right.id);
    });

    for (const entry of ordered) {
      map.addControl(entry.control, entry.position as never);
      attached.push({ id: entry.id, control: entry.control });
    }
  }

  function resolveControl(definition: ControlData): IControl | null {
    const custom = customControls.get(definition.controlId);
    if (custom) {
      return custom.control;
    }

    if (CUSTOM_KINDS.has(definition.kind)) {
      // custom shells render only once their Blazor content is bound
      return null;
    }

    const signature = nativeSignature(definition);
    const existing = nativeControls.get(definition.controlId);
    if (existing && existing.kind === definition.kind && existing.signature === signature) {
      return existing.control;
    }

    const control = createNativeControl(map, definition, emit);
    if (!control) {
      nativeControls.delete(definition.controlId);
      return null;
    }

    nativeControls.set(definition.controlId, { kind: definition.kind, signature, control });
    return control;
  }

  function dropCustom(id: string): void {
    customControls.delete(id);
  }

  return {
    set(control) {
      definitions.set(control.controlId, control);
      const custom = customControls.get(control.controlId);
      if (custom && custom.kind !== control.kind) {
        dropCustom(control.controlId);
      }

      // a live custom shell follows definition updates (chrome, class, panel state)
      if (custom && custom.kind === control.kind) {
        if (custom.control instanceof LegendControl) {
          custom.control.update(control as unknown as ILegendMapControl);
        } else if (custom.control instanceof PanelControl) {
          custom.control.update(control as unknown as IPanelMapControl);
        } else {
          custom.control.update(control as unknown as IContentMapControl);
        }
      }

      recompose();
    },
    remove(id) {
      definitions.delete(id);
      nativeControls.delete(id);
      dropCustom(id);
      recompose();
    },
    setContent(id, events) {
      const definition = definitions.get(id);
      if (!definition || !CUSTOM_KINDS.has(definition.kind)) {
        // biome-ignore lint/suspicious/noConsole: explicit control diagnostics for interop mismatches
        console.warn(`[Spillgebees.Map] control.content ignored: '${id}' is not a registered custom control.`);
        return;
      }

      const existing = customControls.get(id);
      if (existing && existing.kind === definition.kind) {
        if (existing.control instanceof LegendControl) {
          existing.control.update(definition as unknown as ILegendMapControl);
        } else if (existing.control instanceof PanelControl) {
          existing.control.update(definition as unknown as IPanelMapControl, panelStateShim(events));
        } else {
          existing.control.update(definition as unknown as IContentMapControl);
        }

        return;
      }

      const elements = resolveContentElements(id);
      if (!elements) {
        return;
      }

      let control: CustomControlEntry["control"];
      if (definition.kind === "legend") {
        control = new LegendControl(definition as unknown as ILegendMapControl, elements.placeholder, elements.content);
      } else if (definition.kind === "panel") {
        control = new PanelControl(
          definition as unknown as IPanelMapControl,
          elements.placeholder,
          elements.content,
          panelStateShim(events),
        );
      } else {
        control = new ContentControl(
          definition as unknown as IContentMapControl,
          elements.placeholder,
          elements.content,
        );
      }

      customControls.set(id, { kind: definition.kind, control });
      recompose();
    },
    removeContent(id) {
      if (!customControls.has(id)) {
        return;
      }

      dropCustom(id);
      recompose();
    },
    ids: () => attached.map((entry) => entry.id),
    dispose() {
      for (const entry of attached) {
        map.removeControl(entry.control);
      }
      attached.length = 0;
      definitions.clear();
      nativeControls.clear();
      customControls.clear();
    },
  };
}
