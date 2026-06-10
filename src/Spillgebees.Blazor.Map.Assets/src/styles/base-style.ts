// Builds the MapLibre base style from the typed C# MapStyle model (URL, raster
// tiles, or WMS). Shared by the engine bootstrap and style composition.

import type { StyleSpecification } from "maplibre-gl";
import type { IMapStyle } from "../interfaces/map";

const DEFAULT_STYLE_URL = "https://tiles.openfreemap.org/styles/liberty";

/**
 * Converts an `IMapStyle` (from C# interop) to a MapLibre-compatible style.
 * Returns either a string URL or a `StyleSpecification` object.
 */
export function buildStyleFromOptions(style: IMapStyle | null): string | StyleSpecification {
  if (style === null) {
    return DEFAULT_STYLE_URL;
  }

  // Prefer URL over raster/WMS sources
  if (style.url) {
    return style.url;
  }

  if (style.rasterSource) {
    return {
      version: 8,
      sources: {
        "raster-tiles": {
          type: "raster",
          tiles: [style.rasterSource.urlTemplate],
          tileSize: style.rasterSource.tileSize,
          attribution: style.rasterSource.attribution,
        },
      },
      layers: [{ id: "raster-layer", type: "raster", source: "raster-tiles" }],
    };
  }

  if (style.wmsSource) {
    const { baseUrl, layers, format, transparent, version, tileSize } = style.wmsSource;
    // WMS 1.3.0 introduced CRS; all earlier versions (1.0.0, 1.1.0, 1.1.1) use SRS
    const crsParam = version === "1.3.0" ? "CRS" : "SRS";
    const wmsUrl = [
      `${baseUrl}?SERVICE=WMS`,
      `&VERSION=${version}`,
      // biome-ignore lint/security/noSecrets: WMS query parameter, not a secret
      "&REQUEST=GetMap",
      `&LAYERS=${layers}`,
      `&FORMAT=${format}`,
      `&TRANSPARENT=${String(transparent)}`,
      `&${crsParam}=EPSG:3857`,
      "&STYLES=",
      `&WIDTH=${String(tileSize)}`,
      `&HEIGHT=${String(tileSize)}`,
      "&BBOX={bbox-epsg-3857}",
    ].join("");

    return {
      version: 8,
      sources: {
        "raster-tiles": {
          type: "raster",
          tiles: [wmsUrl],
          tileSize: style.wmsSource.tileSize,
          attribution: style.wmsSource.attribution,
        },
      },
      layers: [{ id: "raster-layer", type: "raster", source: "raster-tiles" }],
    };
  }

  // Fallback to default if no style configuration is recognized
  return DEFAULT_STYLE_URL;
}
