# MapLibre GL JS Migration Plan

## Goal

Replace Leaflet with [MapLibre GL JS](https://maplibre.org/maplibre-gl-js/docs/) as the rendering engine
for Spillgebees.Blazor.Map. This is a clean v2.0 — no backward compatibility layer.

### Why

| Concern | Leaflet (current) | MapLibre GL JS |
|---|---|---|
| Rendering | DOM-based (SVG/Canvas) | WebGL (GPU-accelerated) |
| 3D | None | Terrain, buildings, globe, pitch, bearing |
| Marker rotation | Custom prototype patch | Native `rotation` option |
| UI dark theme | 135 lines of CSS hacking `.leaflet-*` | Clean CSS targeting `.maplibregl-*` |
| Clustering | Not supported | Built-in on GeoJSON sources |
| Scale at volume | ~10K markers before lag | Millions via symbol layers |
| API key | None | None (tile-provider-agnostic) |
| TypeScript | `@types/leaflet` (DefinitelyTyped) | Native TypeScript (first-class) |

### Default tiles

[OpenFreeMap](https://openfreemap.org/) — free vector tiles, no API key, no registration.

---

## Design Principles

1. **Zero friction** — sensible defaults, simplest hello-world is minimal code
2. **Blazor-native** — records, `EventCallback<T>`, standard parameter conventions
3. **MapLibre-aware, not MapLibre-bound** — clean abstractions, intuitive naming
4. **No Leaflet ghosts** — no holdover naming, no legacy patterns
5. **Performance-first** — GPU-rendered shapes, consolidated JS interop, efficient diffing

---

## Terminology

The library uses its own consistent terminology. Where it maps to MapLibre concepts,
the mapping is documented here.

| Library term | MapLibre equivalent | Notes |
|---|---|---|
| Feature | Layer / Feature | Umbrella term for markers, circles, polylines |
| MapStyle | Style specification | The visual appearance of the map (tiles + styling rules) |
| TileOverlay | Raster source + raster layer | Additional raster tiles on top of the base style |
| Circle | Circle layer (GeoJSON source) | Pixel-radius circle at a geographic point |
| Polyline | Line layer (GeoJSON source) | Connected line segments |
| Marker | Marker (DOM element) | Interactive point with icon/popup |
| Popup | Popup | Content displayed on click, hover, or permanently |
| Theme | N/A (custom) | UI chrome styling (controls, popups) — not the map tiles |

### Key renames from v1 (Leaflet)

| v1 (Leaflet) | v2 (MapLibre) | Reason |
|---|---|---|
| `LayerDiffer` | `FeatureDiffer` | "Layer" means something different in MapLibre |
| `LayerDiffResult<T>` | `FeatureDiffResult<T>` | Consistency |
| `SyncLayersAsync` | `SyncFeaturesAsync` | Consistency |
| `LayerIds` (FitBoundsOptions) | `FeatureIds` | Consistency |
| `CircleMarker` | `Circle` | Simpler, not Leaflet jargon |
| `TileLayer` | `MapStyle` / `TileOverlay` | Different concepts in MapLibre |
| `MapTheme` | `MapTheme` (kept) | Now means UI chrome theme, not map tiles |
| `StrokeWeight` | `StrokeWidth` / `Width` | More intuitive |
| `InvalidateMapSizeAsync` | `ResizeAsync` | MapLibre's native method name |
| `IPath` | removed | No shared interface needed |

---

## Complete Public API

### Core Types

```csharp
// Unchanged from v1 — human-friendly (lat, lng) order.
// JS interop converts to [lng, lat] at the boundary.
public record Coordinate(double Latitude, double Longitude);

public record Point(double X, double Y);
```

### MapStyle

Replaces `TileLayer`, `TileLayerOptions`, `WmsLayerOptions`.

```csharp
public record MapStyle
{
    internal string? Url { get; }
    internal RasterTileSource? RasterSource { get; }
    internal WmsTileSource? WmsSource { get; }

    public static MapStyle FromUrl(string url);
    public static MapStyle FromRasterUrl(string urlTemplate, string attribution, int tileSize = 256);
    public static MapStyle FromWmsUrl(
        string baseUrl, string layers, string attribution,
        string format = "image/png", bool transparent = false,
        string version = "1.1.1", int tileSize = 256);

    // Presets
    public static class OpenFreeMap
    {
        public static MapStyle Liberty;    // clean, modern
        public static MapStyle Bright;     // colorful
        public static MapStyle Positron;   // light, minimal — good for data viz
    }

    public static class OpenStreetMap
    {
        public static MapStyle Standard;   // classic raster tiles
    }
}
```

### MapOptions

```csharp
public record MapOptions(
    Coordinate Center,
    int Zoom = 0,
    MapStyle? Style = null,                    // null = OpenFreeMap.Liberty
    double Pitch = 0,                          // tilt, 0-85 degrees
    double Bearing = 0,                        // rotation, 0-360 degrees
    MapProjection Projection = MapProjection.Mercator,
    bool Terrain = false,
    double TerrainExaggeration = 1.0,
    FitBoundsOptions? FitBoundsOptions = null,
    int? MinZoom = null,
    int? MaxZoom = null,
    bool Interactive = true,
    bool CooperativeGestures = false
)
{
    public static MapOptions Default => new(new Coordinate(0, 0));
}

public enum MapProjection { Mercator, Globe }
```

### TileOverlay

For raster tile layers rendered on top of the base map style (e.g., OpenRailwayMap).

```csharp
public record TileOverlay(
    string Id,
    string UrlTemplate,
    string Attribution = "",
    int TileSize = 256,
    double Opacity = 1.0
);
```

### Marker

```csharp
public record Marker(
    string Id,
    Coordinate Position,
    string? Title = null,
    PopupOptions? Popup = null,
    MarkerIcon? Icon = null,
    string? Color = null,           // default pin color (CSS color)
    double? Scale = null,           // default pin scale factor
    double? Rotation = null,        // clockwise degrees
    bool Draggable = false,
    double? Opacity = null,
    string? ClassName = null
);
```

### MarkerIcon

```csharp
public record MarkerIcon(
    string Url,                     // HTTP, relative, or data URI (inline SVG)
    Point? Size = null,
    Point? Anchor = null
);
```

### Circle

Implemented via a MapLibre GeoJSON source + circle layer (GPU-rendered).

```csharp
public record Circle(
    string Id,
    Coordinate Position,
    int Radius = 8,                 // pixels
    string? Color = null,           // fill color
    double? Opacity = null,         // fill opacity
    string? StrokeColor = null,
    double? StrokeWidth = null,
    double? StrokeOpacity = null,
    PopupOptions? Popup = null
);
```

### Polyline

Implemented via a MapLibre GeoJSON source + line layer (GPU-rendered).

```csharp
public record Polyline(
    string Id,
    ImmutableList<Coordinate> Coordinates,
    string? Color = null,           // line color
    double? Width = null,           // line width in pixels
    double? Opacity = null,         // line opacity
    PopupOptions? Popup = null
);
```

### PopupOptions

Unified concept replacing separate Tooltip/Popup. The `Trigger` controls behavior.

```csharp
public record PopupOptions(
    string Content,                             // HTML content
    PopupTrigger Trigger = PopupTrigger.Click,
    PopupAnchor Anchor = PopupAnchor.Auto,
    Point? Offset = null,
    bool CloseButton = true,
    string? MaxWidth = null,                    // CSS max-width (default "240px")
    string? ClassName = null
);

public enum PopupTrigger
{
    Click,      // show on click, dismiss via close button or clicking elsewhere
    Hover,      // show on mouse enter, hide on mouse leave
    Permanent,  // always visible — ideal for labels
}

public enum PopupAnchor
{
    Auto,       // MapLibre chooses based on available space
    Top,        // popup above the feature
    Bottom,     // popup below the feature
    Left,       // popup to the left
    Right,      // popup to the right
}
```

### MapControlOptions

```csharp
public record MapControlOptions(
    NavigationControlOptions? Navigation = null,
    ScaleControlOptions? Scale = null,
    FullscreenControlOptions? Fullscreen = null,
    GeolocateControlOptions? Geolocate = null,
    TerrainControlOptions? Terrain = null,
    CenterControlOptions? Center = null
)
{
    public static MapControlOptions Default => new(
        Navigation: new NavigationControlOptions()
    );
}

public record NavigationControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    bool ShowCompass = true,
    bool ShowZoom = true
);

public record ScaleControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.BottomLeft,
    ScaleUnit Unit = ScaleUnit.Metric
);

public record FullscreenControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight
);

public record GeolocateControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight,
    bool TrackUser = false
);

public record TerrainControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopRight
);

public record CenterControlOptions(
    bool Enable = true,
    ControlPosition Position = ControlPosition.TopLeft,
    Coordinate? Center = null,
    int? Zoom = null,
    FitBoundsOptions? FitBoundsOptions = null
);

public enum ControlPosition { TopLeft, TopRight, BottomLeft, BottomRight }
public enum ScaleUnit { Metric, Imperial, Nautical }
```

### FitBoundsOptions

```csharp
public record FitBoundsOptions(
    ImmutableList<string> FeatureIds,
    Point? Padding = null,
    Point? TopLeftPadding = null,
    Point? BottomRightPadding = null
);
```

### Events

```csharp
public record MapClickEventArgs(Coordinate Position);

public record MapViewEventArgs(
    Coordinate Center,
    double Zoom,
    double Bearing,
    double Pitch
);

public record MarkerClickEventArgs(string MarkerId, Coordinate Position);
public record MarkerDragEventArgs(string MarkerId, Coordinate Position);
```

### MapTheme

Controls the UI chrome (controls, popups, attribution) — NOT the map tiles.
The map tiles are controlled by `MapOptions.Style`.

```csharp
public enum MapTheme
{
    Light,
    Dark,
}
```

### SgbMap Component

```csharp
// Parameters
[Parameter] public MapOptions MapOptions { get; set; } = MapOptions.Default;
[Parameter] public MapControlOptions ControlOptions { get; set; } = MapControlOptions.Default;
[Parameter] public MapTheme Theme { get; set; } = MapTheme.Light;
[Parameter] public List<Marker> Markers { get; set; } = [];
[Parameter] public List<Circle> Circles { get; set; } = [];
[Parameter] public List<Polyline> Polylines { get; set; } = [];
[Parameter] public List<TileOverlay> Overlays { get; set; } = [];
[Parameter] public string? Width { get; set; }
[Parameter] public string? Height { get; set; } = "500px";
[Parameter] public string ContainerId { get; set; } = $"map-container-{Guid.NewGuid()}";
[Parameter] public string ContainerClass { get; set; } = string.Empty;

// Event callbacks
[Parameter] public EventCallback<MapClickEventArgs> OnMapClick { get; set; }
[Parameter] public EventCallback<MapViewEventArgs> OnMoveEnd { get; set; }
[Parameter] public EventCallback<MapViewEventArgs> OnZoomEnd { get; set; }
[Parameter] public EventCallback<MarkerClickEventArgs> OnMarkerClick { get; set; }
[Parameter] public EventCallback<MarkerDragEventArgs> OnMarkerDragEnd { get; set; }

// Public methods
public ValueTask FlyToAsync(Coordinate center, int? zoom = null, double? bearing = null, double? pitch = null);
public ValueTask FitBoundsAsync(FitBoundsOptions options);
public ValueTask ResizeAsync();
```

---

## JS Interop Architecture

### Protocol Version Handshake

The JS interop contract can change between releases. When a consumer upgrades the NuGet
package but their browser serves a cached old JS module, the C# side would call functions
that don't exist or pass parameter shapes the old JS doesn't understand, causing cryptic
`JSException` errors.

To guard against this:

1. **JS side (primary defense)**: `bootstrap()` checks `window.Spillgebees.Map.getProtocolVersion()`
   against its own `PROTOCOL_VERSION`. If the existing namespace is missing, incompatible, or
   outdated, it **force-clears the entire `window.Spillgebees.Map` namespace** and reinitializes
   it with the current version's functions and stores. If the version already matches, it's a
   no-op (idempotent). This handles hot reload, Blazor Server reconnections, and package upgrades
   automatically — no user action required.

2. **C# side (safety net)**: `BaseMap.InitializeMapAsync` calls `getProtocolVersion` before any
   other interop call. If the function is missing (the rare case where the old JS itself is
   cached and the new JS never loaded at all) or returns a different version, a clear
   `InvalidOperationException` is thrown:

   > *Spillgebees.Blazor.Map: JavaScript/C# version mismatch. The loaded JavaScript module
   > is protocol version 0 but the .NET library expects protocol version 1.
   > Clear your browser cache and reload the page.*

   This is a last-resort safety net — in practice, the JS-side force-clear handles 99% of cases.

3. **Versioning rule**: `PROTOCOL_VERSION` is bumped whenever the JS interop contract changes
   (function names, parameter shapes, return types). It is NOT bumped for internal JS-only
   changes that don't affect the C#/JS boundary.

### Namespace

```
window.Spillgebees.Map.getProtocolVersion()   → number
window.Spillgebees.Map.mapFunctions.*         interop functions (unchanged pattern)
window.Spillgebees.Map.maps                   Map<HTMLElement, MapLibreMap>
window.Spillgebees.Map.features               Map<MapLibreMap, FeatureStorage>
window.Spillgebees.Map.overlays               Map<MapLibreMap, Map<string, RasterSource>>
window.Spillgebees.Map.controls               Map<MapLibreMap, Set<IControl>>
```

### Interop Functions

| Function | C# caller | Description |
|---|---|---|
| `createMap` | `InitializeMapAsync` | Create MapLibre map, apply style, controls, initial features/overlays |
| `syncFeatures` | `SyncFeaturesAsync` | Single consolidated call with `{ markers: { added, updated, removedIds }, circles: { ... }, polylines: { ... } }` |
| `setOverlays` | `OnParametersSetAsync` | Add/remove raster tile overlay sources |
| `setControls` | `OnParametersSetAsync` | Replace controls |
| `setMapOptions` | `OnParametersSetAsync` | Update style, pitch, bearing, terrain, projection |
| `setTheme` | `OnParametersSetAsync` | Apply/remove dark theme CSS class |
| `fitBounds` | `FitBoundsAsync` | Fit view to feature bounds |
| `flyTo` | `FlyToAsync` | Animated camera flight |
| `resize` | `ResizeAsync` | Recalculate map dimensions |
| `disposeMap` | `DisposeAsync` | Remove map and clean up stores |

### Performance Design

1. **Single `syncFeatures` call** per render cycle — consolidates add/update/remove into one
   interop boundary crossing (was up to 3 separate calls)
2. **Circles and polylines as GeoJSON sources** — one `source.setData()` call updates all circles
   or all polylines. GPU renders at 60fps, no DOM overhead
3. **Marker updates are property-targeted** — when only position/rotation changes (the common
   real-time case), only `marker.setLngLat()` + `marker.setRotation()` are called, no DOM recreation
4. **FeatureDiffer** on the C# side computes minimal diffs via record value equality —
   only changed features cross the interop boundary

---

## Implementation Phases

### Phase 1: Bootstrap — TypeScript scaffold + build pipeline

- Replace `leaflet` with `maplibre-gl` in `package.json`
- Update Vite config:
  - Remove `vite-plugin-static-copy` (no Leaflet images to copy)
  - Remove `@laynezh/vite-plugin-lib-assets` (no static image assets)
  - Add MapLibre CSS to the build
- Update `styles.scss`:
  - Import `maplibre-gl/dist/maplibre-gl.css` (replaces `leaflet/dist/leaflet.css`)
  - Rewrite `.sgb-map-dark` theme targeting `.maplibregl-*` classes
  - Rewrite CenterControl styles
- Update `global.d.ts` for new `window.Spillgebees` types
- Update TypeScript interfaces to match new C# models
- Implement `PROTOCOL_VERSION` constant + `getProtocolVersion()` in bootstrap
- Verify build produces `Spillgebees.Blazor.Map.lib.module.{js,css}`

### Phase 2: Core map lifecycle

- JS: `createMap` / `disposeMap` using `maplibregl.Map`
- JS: `setMapOptions` for runtime style/pitch/bearing/terrain/projection changes
- JS: `setTheme` for UI chrome theming
- C#: New models — `MapOptions`, `MapStyle`, `MapProjection`, `MapTheme`
- C#: `Coordinate` stays `(Latitude, Longitude)` — JS converts at boundary
- C#: Update `BaseMap` / `SgbMap` component (new parameters, remove Leaflet-specific ones)
- C#: Protocol version validation in `InitializeMapAsync` (before `createMap`)
- C#: `FeatureDiffer` (rename from `LayerDiffer`)
- C#: `FitBoundsOptions` with `FeatureIds`
- C#: Update JSON serialization (`ControlPosition` with `JsonStringEnumMemberName`)

### Phase 3: Controls

- C#: `MapControlOptions` with all new control types
- JS: `setControls` using MapLibre built-in controls
- JS: Reimplement `CenterControl` as MapLibre `IControl`
- Both themes (light/dark) applied to all controls

### Phase 4: Markers + popups

- JS: Markers via `maplibregl.Marker` (native rotation)
- JS: Custom marker icons via `element` option (HTMLElement with `<img>`)
- JS: Popups via `maplibregl.Popup` with trigger modes (click, hover, permanent)
- C#: `Marker`, `MarkerIcon`, `PopupOptions`, `PopupTrigger`, `PopupAnchor`
- JS: `syncFeatures` — marker add/update/remove logic
- C#: `FeatureDiffer` integration for markers

### Phase 5: Circles + polylines (GPU-rendered)

- JS: Circles via GeoJSON source (`sgb-circles-source`) + circle layer (`sgb-circles-layer`)
  with data-driven paint properties from feature properties
- JS: Polylines via GeoJSON source (`sgb-polylines-source`) + line layer (`sgb-polylines-layer`)
  with data-driven paint properties
- JS: Popups on circles/polylines via layer click/hover events
- C#: `Circle`, `Polyline` models
- JS: `syncFeatures` — circle and polyline sync via source data updates
- C#: `FeatureDiffer` integration for circles and polylines

### Phase 6: Tile overlays

- C#: `TileOverlay` model
- JS: `setOverlays` — add/remove raster sources and raster layers at runtime
- C#: SgbMap `Overlays` parameter with change detection

### Phase 7: FitBounds + FlyTo + Resize

- JS: `fitBounds` — calculate merged bounds from feature IDs (markers, circles, polylines)
- JS: `flyTo` — animated camera transition
- JS: `resize` — `map.resize()`
- C#: Public methods on BaseMap: `FitBoundsAsync`, `FlyToAsync`, `ResizeAsync`

### Phase 8: Map events

- C#: Event args: `MapClickEventArgs`, `MapViewEventArgs`, `MarkerClickEventArgs`, `MarkerDragEventArgs`
- C#: `EventCallback<T>` parameters on BaseMap
- JS: Wire MapLibre events → `dotNetHelper.invokeMethodAsync`
  - `map.on('click')` → `OnMapClick`
  - `map.on('moveend')` → `OnMoveEnd`
  - `map.on('zoomend')` → `OnZoomEnd`
  - Per-marker click/drag listeners → `OnMarkerClick`, `OnMarkerDragEnd`

### Phase 9: Tests

- Rewrite TypeScript test mocks (MapLibre mock replacing Leaflet mock)
- Rewrite all TypeScript tests for new interop functions
- Update .NET/bUnit tests (interop function names, model changes)
- Test coverage for FeatureDiffer (should be straightforward rename)
- Test PopupOptions with all trigger modes

### Phase 10: Sample apps

- Migrate BasicMapExample → OpenFreeMap + circles/polylines/markers/fitBounds
- Migrate ThemeExample → style switcher + UI theme toggle
- Migrate WmsTileLayerExample → `MapStyle.FromWmsUrl(...)`
- Migrate TrainTrackingExample → OpenFreeMap vector tiles + OpenRailwayMap overlay
  + permanent popups as labels + native rotation
- Update all custom CSS to target `.maplibregl-*` classes
- Verify all three hosting modes (Server, WASM, WebApp)

---

## Deferred Features

Features not in the initial implementation, tracked for future work.

### Priority: High (needed for real-world usage)

| Feature | Description | Rationale for deferring |
|---|---|---|
| Polygon | Closed shape with fill. Same pattern as Polyline but with `fill` layer | Not used in any current example |
| Marker decorations | Visual elements positioned relative to markers (labels, badges, icons) | Requires design work for HTML marker children approach. WIP from Leaflet branch provides model reference |
| Symbol layers (GPU markers) | GPU-rendered markers via `symbol` layer for 10K+ points | DOM markers are fine for current scale. Add when consumers need massive point clouds |
| Clustering | Built-in MapLibre GeoJSON source clustering | Requires Source/Layer API. Add alongside symbol layers |

### Priority: Medium (nice-to-have)

| Feature | Description | Rationale for deferring |
|---|---|---|
| GeoJSON Source/Layer API | Direct access to MapLibre's source + layer architecture for power users | Core abstraction (markers, circles, polylines) covers most use cases first |
| `OnCircleClick` / `OnPolylineClick` events | Click events on circles and polylines | Popup click/hover handles most interaction needs. Add when consumers request direct callbacks |
| Heatmap layer | Point density visualization via MapLibre `heatmap` layer | Requires Source/Layer API |
| Fill-extrusion (3D buildings) | 3D extruded polygons | Requires Source/Layer API |
| Multiple popups per feature | Support both a permanent label AND a click popup on the same marker | One popup per feature covers 99% of use cases |
| `RiseOnHover` equivalent | Raise marker z-index on hover | Achievable via CSS `:hover`. Add as Marker parameter if consumers request it |

### Priority: Low (future enhancements)

| Feature | Description |
|---|---|
| Image/video overlays | Georeferenced images or videos on the map |
| Custom HTML markers via RenderFragment | Blazor component content projection into marker DOM |
| Retina tile support | `@2x` URL support for raster tile overlays |
| Cooperative gesture customization | Custom messages for Ctrl+scroll gesture requirement |
| Map comparison (swipe/side-by-side) | Split-screen map comparison |

---

## Breaking Changes from v1

| Change | Migration |
|---|---|
| `TileLayers` parameter removed | Use `MapOptions.Style` (presets or `FromRasterUrl`) |
| `TileLayer` type removed | Use `MapStyle` for base map, `TileOverlay` for overlays |
| `CircleMarker` renamed to `Circle` | Find-and-replace |
| `CircleMarkers` parameter renamed to `Circles` | Find-and-replace |
| `Marker.Coordinate` renamed to `Marker.Position` | Find-and-replace |
| `CircleMarker.Coordinate` renamed to `Circle.Position` | Find-and-replace |
| `Marker.RotationAngle` renamed to `Marker.Rotation` | Find-and-replace |
| `Marker.RotationOrigin` removed | MapLibre rotates natively around center |
| `Marker.ZIndexOffset` removed | Not applicable in MapLibre |
| `Marker.RiseOnHover` / `RiseOffset` removed | Use CSS `:hover` if needed |
| `TooltipOptions` replaced by `PopupOptions` | Use `PopupOptions` with appropriate `Trigger` |
| `TooltipOffset` removed | Use `Point` for popup offset |
| `TooltipDirection` replaced by `PopupAnchor` | `Right` → `PopupAnchor.Left` (anchor is opposite side) |
| `IPath` interface removed | Circle and Polyline have own styling properties |
| `Polyline.SmoothFactor` / `NoClip` removed | Not applicable in MapLibre (WebGL rendering) |
| `Polyline.StrokeColor/Weight/Opacity` simplified | Use `Color`/`Width`/`Opacity` directly |
| `Circle`: `Stroke`/`Fill` booleans removed | Presence of color value implies enablement |
| `MarkerIcon` simplified | No shadow, no popup/tooltip anchors |
| `MapOptions.ShowLeafletPrefix` removed | Not applicable |
| `MapOptions.Theme` moved to `SgbMap.Theme` | Separate parameter on component |
| `MapTheme.Default` renamed to `MapTheme.Light` | Find-and-replace |
| `MapControlOptions` parameter renamed to `ControlOptions` | Find-and-replace |
| `ZoomControlOptions` replaced by `NavigationControlOptions` | Richer control (zoom + compass + pitch) |
| `FitBoundsOptions.LayerIds` renamed to `FeatureIds` | Find-and-replace |
| `InvalidateMapSizeAsync()` renamed to `ResizeAsync()` | Find-and-replace |
| `MapContainerHtmlId` renamed to `ContainerId` | Find-and-replace |
| `MapContainerClass` renamed to `ContainerClass` | Find-and-replace |
| `LayerDiffer` renamed to `FeatureDiffer` | Internal, but public type |
| `LayerDiffResult<T>` renamed to `FeatureDiffResult<T>` | Internal, but public type |
