using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Spillgebees.Blazor.Map;
using Spillgebees.Blazor.Map.Runtime.Scene;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// A GeoJSON source component that manages MapLibre sources and their child layers.
/// Place <see cref="LayerBase"/>-derived components (e.g., <see cref="LineLayer"/>,
/// <see cref="CircleLayer"/>) as children to create layers that reference this source.
/// </summary>
public partial class GeoJsonSource : ComponentBase, IMapSource, IAsyncDisposable
{
    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = null!;

    /// <summary>
    /// The parent map component.
    /// </summary>
    [CascadingParameter]
    public BaseMap? Map { get; set; }

    [CascadingParameter]
    private MapSectionContext? SectionContext { get; set; }

    /// <summary>
    /// A unique identifier for this source.
    /// </summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    /// <summary>
    /// The GeoJSON data for this source. Can be a GeoJSON object or a URL string.
    /// </summary>
    [Parameter, EditorRequired]
    public object? Data { get; set; }

    /// <summary>
    /// Child content (layer components).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Parameter-based layer definitions rendered against this GeoJSON source.
    /// These layers are registered before child layer components for deterministic ordering.
    /// </summary>
    [Parameter]
    public IReadOnlyList<MapLayerDefinition>? Layers { get; set; }

    [Parameter]
    public bool AllowOutsideMapSources { get; set; }

    [Parameter]
    public string? LayerGroup { get; set; }

    [Parameter]
    public string? BeforeLayerGroup { get; set; }

    [Parameter]
    public string? AfterLayerGroup { get; set; }

    // Clustering

    /// <summary>
    /// Whether to cluster point features in this source.
    /// </summary>
    [Parameter]
    public bool Cluster { get; set; }

    /// <summary>
    /// The radius of each cluster in pixels. Default is 50.
    /// </summary>
    [Parameter]
    public int ClusterRadius { get; set; } = 50;

    /// <summary>
    /// The maximum zoom level at which clustering is applied.
    /// </summary>
    [Parameter]
    public int? ClusterMaxZoom { get; set; }

    /// <summary>
    /// The minimum number of points required to form a cluster.
    /// </summary>
    [Parameter]
    public int? ClusterMinPoints { get; set; }

    /// <summary>
    /// Custom properties to aggregate across clustered features using MapLibre reduce expressions.
    /// Keys are the output property names available on cluster features. Values are MapLibre
    /// <a href="https://maplibre.org/maplibre-style-spec/sources/#geojson-clusterProperties">accumulator expressions</a>.
    /// </summary>
    /// <example>
    /// <code>
    /// ClusterProperties="@(new Dictionary&lt;string, object&gt; {
    ///     ["totalValue"] = new object[] { "+", new object[] { "get", "value" } },
    ///     ["hasAlert"] = new object[] { "any", new object[] { "get", "alert" } },
    /// })"
    /// </code>
    /// </example>
    [Parameter]
    public IDictionary<string, object>? ClusterProperties { get; set; }

    /// <summary>
    /// Shared source-level clustering options. When provided, this supersedes the legacy
    /// cluster parameters on this component.
    /// </summary>
    [Parameter]
    public ClusterOptions? ClusterOptions { get; set; }

    // Options

    /// <summary>
    /// The maximum zoom level for the source. Default is 18.
    /// </summary>
    [Parameter]
    public int MaxZoom { get; set; } = 18;

    /// <summary>
    /// Whether to auto-generate unique feature IDs.
    /// </summary>
    [Parameter]
    public bool GenerateId { get; set; }

    /// <summary>
    /// A property to use as the feature ID.
    /// </summary>
    [Parameter]
    public string? PromoteId { get; set; }

    /// <summary>
    /// Attribution text for the source.
    /// </summary>
    [Parameter]
    public string? Attribution { get; set; }

    /// <summary>
    /// Whether to calculate line metrics for gradient lines.
    /// </summary>
    [Parameter]
    public bool LineMetrics { get; set; }

    /// <summary>
    /// When set, position changes in the source data are smoothly interpolated
    /// over the specified duration. Only affects Point geometry features.
    /// </summary>
    [Parameter]
    public AnimationOptions? Animation { get; set; }

    private bool _isInitialized;
    private object? _previousData;
    private readonly List<LayerBase> _pendingLayers = [];
    private readonly List<LayerBase> _registeredLayers = [];
    private readonly List<string> _registeredDefinitionLayerIds = [];
    private readonly List<string> _registeredClusterEventLayerIds = [];
    private MapLayerOrderOptions _previousOrderOptions = MapLayerOrderOptions.Empty;
    private IReadOnlyList<MapLayerDefinition> _previousLayerDefinitions = [];
    private IReadOnlyDictionary<string, object?> _previousSourceConfiguration = new Dictionary<string, object?>();
    private static readonly object[] ClusterLayerFilter = ["has", "point_count"];
    private DotNetObjectReference<GeoJsonSource>? _dotNetRef;

    /// <inheritdoc/>
    MapLayerOrderOptions IMapSource.OrderOptions => new(LayerGroup, BeforeLayerGroup, AfterLayerGroup);

    private MapLayerOrderOptions OrderOptions => new(LayerGroup, BeforeLayerGroup, AfterLayerGroup);

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        if (!AllowOutsideMapSources && SectionContext?.Kind is not MapContentSectionKind.Sources)
        {
            throw new InvalidOperationException("GeoJsonSource must be placed inside MapSources.");
        }
    }

    /// <inheritdoc/>
    public async Task RegisterLayerAsync(LayerBase layer)
    {
        if (!_registeredLayers.Contains(layer))
        {
            _registeredLayers.Add(layer);
        }

        if (!_isInitialized)
        {
            _pendingLayers.Add(layer);
            return;
        }

        await AddLayerToMapAsync(layer);
    }

    /// <inheritdoc/>
    public async Task UnregisterLayerAsync(LayerBase layer)
    {
        _registeredLayers.Remove(layer);
        _pendingLayers.Remove(layer);

        if (!_isInitialized)
        {
            return;
        }

        try
        {
            await Map!.SceneRegistry.UnregisterLayerAsync(layer.Id);
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Map is not null)
        {
            var layerDefinitions = GetCurrentLayerDefinitions();
            ValidateUniqueDefinitionLayerIds(layerDefinitions);

            // Wait for map to be ready
            var mapReady = await Map.WhenReadyAsync();
            if (!mapReady)
            {
                return;
            }

            await AddSourceToMapAsync();
            _isInitialized = true;

            await RegisterDefinitionLayersAsync(layerDefinitions);

            // Add any layers that registered before the source was ready
            if (_pendingLayers.Count > 0)
            {
                await Map.SceneRegistry.RegisterLayersAsync(
                    _pendingLayers.Select(layer => new MapLayerDescriptor(
                        layer.Id,
                        layer.BuildLayerSpec(),
                        layer.BeforeLayerId,
                        layer.GetLayerOrderRegistration()
                    ))
                );

                foreach (var layer in _pendingLayers)
                {
                    await layer.NotifyLayerAddedAsync();
                }
            }

            _pendingLayers.Clear();
        }
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        var currentSourceConfiguration = BuildSourceConfiguration();
        var currentLayerDefinitions = GetCurrentLayerDefinitions();
        ValidateUniqueDefinitionLayerIds(currentLayerDefinitions);

        if (_isInitialized && !SourceConfigurationsEqual(_previousSourceConfiguration, currentSourceConfiguration))
        {
            await ReplaceSourceAsync(currentSourceConfiguration);
            return;
        }

        if (_isInitialized && Data != _previousData && Data is not null)
        {
            _previousData = Data;
            var batch = Map!.SceneRegistry.CreateBatchBuilder();
            batch.SetSourceData(Id, Data, Animation);
            await Map.SceneRegistry.ApplyBatchAsync(batch);
        }

        if (_isInitialized && _previousOrderOptions != OrderOptions)
        {
            await Map!.SceneRegistry.RegisterLayersAsync(
                BuildDefinitionLayerDescriptors(currentLayerDefinitions)
                    .Concat(
                        _registeredLayers.Select(layer => new MapLayerDescriptor(
                            layer.Id,
                            layer.BuildLayerSpec(),
                            layer.BeforeLayerId,
                            layer.GetLayerOrderRegistration()
                        ))
                    )
            );

            foreach (var layer in _registeredLayers)
            {
                await layer.NotifyLayerAddedAsync();
            }

            _previousOrderOptions = OrderOptions;
        }

        if (_isInitialized && !LayerDefinitionsEqual(_previousLayerDefinitions, currentLayerDefinitions))
        {
            await ReplaceDefinitionLayersAsync(currentLayerDefinitions);
        }

        if (
            _isInitialized
            && !ClusterLayerEventRegistrationsEqual(
                _registeredClusterEventLayerIds,
                GetCurrentInteractiveClusterLayerIds()
            )
        )
        {
            await RewireClusterLayerEventsAsync();
        }
    }

    private async Task AddSourceToMapAsync()
    {
        var sourceConfiguration = BuildSourceConfiguration();
        var cleanSpec = BuildSourceSpec(sourceConfiguration);

        _previousData = Data;
        _previousOrderOptions = OrderOptions;
        _previousSourceConfiguration = CloneSourceConfiguration(sourceConfiguration);
        await Map!.SceneRegistry.RegisterSourceAsync(new MapSourceDescriptor(Id, cleanSpec));
    }

    private IReadOnlyDictionary<string, object?> BuildSourceConfiguration()
    {
        var clusterOptions = GetEffectiveClusterOptions();
        var sourceConfiguration = new Dictionary<string, object?>
        {
            ["type"] = "geojson",
            ["maxzoom"] = MaxZoom,
            ["cluster"] = clusterOptions.Enabled ? true : null,
            ["generateId"] = GenerateId ? true : null,
            ["lineMetrics"] = LineMetrics ? true : null,
        };

        if (clusterOptions.Enabled)
        {
            sourceConfiguration["clusterRadius"] = clusterOptions.Radius;
            if (clusterOptions.MaxZoom.HasValue)
            {
                sourceConfiguration["clusterMaxZoom"] = clusterOptions.MaxZoom.Value;
            }

            if (clusterOptions.MinPoints.HasValue)
            {
                sourceConfiguration["clusterMinPoints"] = clusterOptions.MinPoints.Value;
            }

            if (clusterOptions.Properties is not null)
            {
                sourceConfiguration["clusterProperties"] = clusterOptions.Properties;
            }
        }

        if (PromoteId is not null)
        {
            sourceConfiguration["promoteId"] = PromoteId;
        }

        if (Attribution is not null)
        {
            sourceConfiguration["attribution"] = Attribution;
        }

        return sourceConfiguration
            .Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
    }

    private Dictionary<string, object?> BuildSourceSpec(IReadOnlyDictionary<string, object?> sourceConfiguration)
    {
        var sourceSpec = new Dictionary<string, object?>(sourceConfiguration, StringComparer.Ordinal)
        {
            ["data"] = Data,
        };

        return sourceSpec;
    }

    private async Task ReplaceSourceAsync(IReadOnlyDictionary<string, object?> sourceConfiguration)
    {
        var layerDefinitions = GetCurrentLayerDefinitions();
        var definitionLayerDescriptors = BuildDefinitionLayerDescriptors(layerDefinitions).ToArray();
        var childLayerDescriptors = _registeredLayers
            .Select(layer => new MapLayerDescriptor(
                layer.Id,
                layer.BuildLayerSpec(),
                layer.BeforeLayerId,
                layer.GetLayerOrderRegistration()
            ))
            .ToArray();
        var batch = Map!.SceneRegistry.CreateBatchBuilder();

        foreach (var layerId in _registeredClusterEventLayerIds)
        {
            batch.UnregisterLayerEvents(layerId);
        }

        batch.RemoveSource(Id);
        batch.AddSource(new MapSourceDescriptor(Id, BuildSourceSpec(sourceConfiguration)));
        foreach (var descriptor in definitionLayerDescriptors.Concat(childLayerDescriptors))
        {
            batch.AddLayer(descriptor);
        }

        try
        {
            await Map.SceneRegistry.ApplyBatchAsync(batch);
        }
        catch (JSDisconnectedException)
        {
            return;
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        _previousData = Data;
        _previousOrderOptions = OrderOptions;
        _previousSourceConfiguration = CloneSourceConfiguration(sourceConfiguration);
        _registeredClusterEventLayerIds.Clear();
        _registeredDefinitionLayerIds.Clear();
        _registeredDefinitionLayerIds.AddRange(definitionLayerDescriptors.Select(descriptor => descriptor.LayerId));
        await RewireClusterLayerEventsAsync();
        _previousLayerDefinitions = layerDefinitions.ToArray();

        foreach (var layer in _registeredLayers)
        {
            await layer.NotifyLayerAddedAsync();
        }
    }

    private ClusterOptions GetEffectiveClusterOptions()
    {
        if (ClusterOptions is not null)
        {
            return ClusterOptions;
        }

        if (!Cluster)
        {
            return Spillgebees.Blazor.Map.ClusterOptions.None;
        }

        return Spillgebees.Blazor.Map.ClusterOptions.Create(
            ClusterRadius,
            ClusterMaxZoom,
            ClusterMinPoints,
            ClusterProperties is null ? null : new Dictionary<string, object>(ClusterProperties, StringComparer.Ordinal)
        );
    }

    private async Task AddLayerToMapAsync(LayerBase layer)
    {
        await Map!.SceneRegistry.RegisterLayerAsync(
            new MapLayerDescriptor(
                layer.Id,
                layer.BuildLayerSpec(),
                layer.BeforeLayerId,
                layer.GetLayerOrderRegistration()
            )
        );
        await layer.NotifyLayerAddedAsync();
    }

    private async Task ReplaceDefinitionLayersAsync(IReadOnlyList<MapLayerDefinition> layerDefinitions)
    {
        if (_registeredDefinitionLayerIds.Count > 0)
        {
            var registeredDefinitionLayerIds = _registeredDefinitionLayerIds.ToArray();
            _registeredDefinitionLayerIds.Clear();

            try
            {
                foreach (var layerId in registeredDefinitionLayerIds)
                {
                    await Map!.SceneRegistry.UnregisterLayerAsync(layerId);
                }
            }
            catch (JSDisconnectedException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }

        await RegisterDefinitionLayersAsync(layerDefinitions);
    }

    private async Task RegisterDefinitionLayersAsync(IReadOnlyList<MapLayerDefinition> layerDefinitions)
    {
        if (layerDefinitions.Count == 0)
        {
            _previousLayerDefinitions = [];
            return;
        }

        var descriptors = BuildDefinitionLayerDescriptors(layerDefinitions).ToArray();
        await Map!.SceneRegistry.RegisterLayersAsync(descriptors);

        _registeredDefinitionLayerIds.Clear();
        _registeredDefinitionLayerIds.AddRange(descriptors.Select(descriptor => descriptor.LayerId));
        await RewireClusterLayerEventsAsync();
        _previousLayerDefinitions = layerDefinitions.ToArray();
    }

    private async Task RewireClusterLayerEventsAsync()
    {
        if (Map is null)
        {
            return;
        }

        foreach (var layerId in _registeredClusterEventLayerIds.ToArray())
        {
            await Map.SceneRegistry.UnregisterLayerEventsAsync(layerId);
        }

        _registeredClusterEventLayerIds.Clear();

        var interactiveLayerIds = GetCurrentInteractiveClusterLayerIds().ToArray();
        if (interactiveLayerIds.Length == 0)
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
            return;
        }

        _dotNetRef ??= DotNetObjectReference.Create(this);
        foreach (var layerId in interactiveLayerIds)
        {
            await Map.SceneRegistry.WireLayerEventsAsync(
                new LayerEventDescriptor(layerId, _dotNetRef, true, false, false)
            );
            _registeredClusterEventLayerIds.Add(layerId);
        }
    }

    private IEnumerable<string> GetCurrentInteractiveClusterLayerIds()
    {
        if (
            ClusterOptions
            is not { Enabled: true, LayerSet.Enabled: true, ClickBehavior: ClusterClickBehavior.ZoomToDissolve }
        )
        {
            yield break;
        }

        foreach (var definition in ClusterOptions.LayerSet.Layers.Where(definition => definition.Interactive))
        {
            yield return $"{Id}-{definition.IdSuffix}";
        }
    }

    private IEnumerable<MapLayerDescriptor> BuildDefinitionLayerDescriptors(
        IReadOnlyList<MapLayerDefinition> layerDefinitions
    )
    {
        foreach (var layerDefinition in layerDefinitions)
        {
            var layerId = layerDefinition.ResolveId(Id);
            yield return new MapLayerDescriptor(
                layerId,
                BuildDefinitionLayerSpec(layerDefinition, layerId),
                layerDefinition.BeforeLayerId,
                GetDefinitionLayerOrderRegistration(layerDefinition)
            );
        }
    }

    private Dictionary<string, object?> BuildDefinitionLayerSpec(MapLayerDefinition layerDefinition, string layerId)
    {
        var spec = new Dictionary<string, object?>
        {
            ["id"] = layerId,
            ["type"] = layerDefinition.Type,
            ["source"] = Id,
        };

        if (layerDefinition.Filter is not null)
        {
            spec["filter"] = layerDefinition.Filter;
        }

        if (layerDefinition.MinZoom.HasValue)
        {
            spec["minzoom"] = layerDefinition.MinZoom.Value;
        }

        if (layerDefinition.MaxZoom.HasValue)
        {
            spec["maxzoom"] = layerDefinition.MaxZoom.Value;
        }

        var paint = GetDefinitionPaintProperties(layerDefinition)
            .Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        if (paint.Count > 0)
        {
            spec["paint"] = paint;
        }

        var layout = GetDefinitionLayoutProperties(layerDefinition)
            .Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        layout["visibility"] = layerDefinition.Visible ? "visible" : "none";
        if (layout.Count > 0)
        {
            spec["layout"] = layout;
        }

        return spec;
    }

    private static Dictionary<string, object?> GetDefinitionPaintProperties(MapLayerDefinition layerDefinition) =>
        layerDefinition switch
        {
            CircleLayerDefinition circle => new Dictionary<string, object?>
            {
                ["circle-radius"] = circle.Radius?.ToSerializable(),
                ["circle-color"] = circle.Color?.ToSerializable(),
                ["circle-opacity"] = circle.Opacity?.ToSerializable(),
                ["circle-stroke-width"] = circle.StrokeWidth?.ToSerializable(),
                ["circle-stroke-color"] = circle.StrokeColor?.ToSerializable(),
                ["circle-stroke-opacity"] = circle.StrokeOpacity?.ToSerializable(),
                ["circle-pitch-alignment"] = circle.PitchAlignment?.ToJsonName(),
            },
            SymbolLayerDefinition symbol => new Dictionary<string, object?>
            {
                ["text-color"] = symbol.TextColor?.ToSerializable(),
                ["text-halo-color"] = symbol.TextHaloColor?.ToSerializable(),
                ["text-halo-width"] = symbol.TextHaloWidth?.ToSerializable(),
                ["text-opacity"] = symbol.TextOpacity?.ToSerializable(),
                ["icon-opacity"] = symbol.IconOpacity?.ToSerializable(),
                ["icon-color"] = symbol.IconColor?.ToSerializable(),
            },
            _ => throw new NotSupportedException(
                $"Layer definition type '{layerDefinition.GetType().Name}' is not supported by GeoJsonSource."
            ),
        };

    private static Dictionary<string, object?> GetDefinitionLayoutProperties(MapLayerDefinition layerDefinition) =>
        layerDefinition switch
        {
            CircleLayerDefinition => [],
            SymbolLayerDefinition symbol => new Dictionary<string, object?>
            {
                ["text-field"] = symbol.TextField?.ToSerializable(),
                ["text-size"] = symbol.TextSize?.ToSerializable(),
                ["text-font"] = symbol.TextFont,
                ["text-anchor"] = symbol.TextAnchor?.ToJsonName(),
                ["text-offset"] = symbol.TextOffset,
                ["text-rotate"] = symbol.TextRotate?.ToSerializable(),
                ["text-pitch-alignment"] = symbol.TextPitchAlignment?.ToJsonName(),
                ["text-rotation-alignment"] = symbol.TextRotationAlignment?.ToJsonName(),
                ["text-transform"] = symbol.TextTransform?.ToJsonName(),
                ["text-max-width"] = symbol.TextMaxWidth,
                ["text-allow-overlap"] = symbol.TextAllowOverlap ? true : null,
                ["icon-image"] = symbol.IconImage?.ToSerializable(),
                ["icon-size"] = symbol.IconSize?.ToSerializable(),
                ["icon-rotate"] = symbol.IconRotate?.ToSerializable(),
                ["icon-offset"] = symbol.IconOffset,
                ["icon-anchor"] = symbol.IconAnchor?.ToSerializable(),
                ["icon-allow-overlap"] = symbol.IconAllowOverlap ? true : null,
                ["icon-text-fit"] = symbol.IconTextFit?.ToJsonName(),
                ["icon-text-fit-padding"] = symbol.IconTextFitPadding,
                ["icon-rotation-alignment"] = symbol.RotationAlignment?.ToJsonName(),
                ["symbol-placement"] = symbol.Placement?.ToJsonName(),
                ["symbol-spacing"] = symbol.Spacing,
                ["symbol-sort-key"] = symbol.SymbolSortKey?.ToSerializable(),
            },
            _ => throw new NotSupportedException(
                $"Layer definition type '{layerDefinition.GetType().Name}' is not supported by GeoJsonSource."
            ),
        };

    private LayerOrderRegistration GetDefinitionLayerOrderRegistration(MapLayerDefinition layerDefinition)
    {
        return Map!.SceneRegistry.ReserveLayerOrderRegistration(
            $"layer-definition:{Id}:{layerDefinition.Key}",
            new MapLayerOrderOptions(
                layerDefinition.LayerGroup,
                layerDefinition.BeforeLayerGroup,
                layerDefinition.AfterLayerGroup
            ),
            OrderOptions
        );
    }

    private IReadOnlyList<MapLayerDefinition> GetCurrentLayerDefinitions()
    {
        var clusterLayerDefinitions = GetCurrentClusterLayerDefinitions();
        if (clusterLayerDefinitions.Count == 0)
        {
            return Layers ?? [];
        }

        return Layers is { Count: > 0 } ? clusterLayerDefinitions.Concat(Layers).ToArray() : clusterLayerDefinitions;
    }

    private IReadOnlyList<MapLayerDefinition> GetCurrentClusterLayerDefinitions()
    {
        if (ClusterOptions is not { Enabled: true, LayerSet.Enabled: true })
        {
            return [];
        }

        return ClusterOptions.LayerSet.Layers.Select(MapClusterLayerDefinition).ToArray();
    }

    private static MapLayerDefinition MapClusterLayerDefinition(ClusterLayerDefinition layerDefinition) =>
        layerDefinition switch
        {
            ClusterCircleLayerDefinition circle => new CircleLayerDefinition(
                circle.IdSuffix,
                circle.Color,
                circle.Radius,
                circle.Opacity,
                circle.StrokeWidth,
                circle.StrokeColor,
                key: $"cluster:{circle.IdSuffix}",
                filter: ClusterLayerFilter,
                minZoom: circle.MinZoom,
                maxZoom: circle.MaxZoom,
                visible: circle.Visible,
                beforeLayerId: circle.BeforeLayerId,
                layerGroup: circle.LayerGroup,
                beforeLayerGroup: circle.BeforeLayerGroup,
                afterLayerGroup: circle.AfterLayerGroup
            ),
            ClusterSymbolLayerDefinition symbol => new SymbolLayerDefinition(
                symbol.IdSuffix,
                symbol.TextField ?? Expr.Get("point_count_abbreviated"),
                symbol.TextSize,
                textColor: symbol.TextColor,
                key: $"cluster:{symbol.IdSuffix}",
                filter: ClusterLayerFilter,
                minZoom: symbol.MinZoom,
                maxZoom: symbol.MaxZoom,
                visible: symbol.Visible,
                beforeLayerId: symbol.BeforeLayerId,
                layerGroup: symbol.LayerGroup,
                beforeLayerGroup: symbol.BeforeLayerGroup,
                afterLayerGroup: symbol.AfterLayerGroup
            ),
            _ => throw new NotSupportedException(
                $"Cluster layer definition type '{layerDefinition.GetType().Name}' is not supported by GeoJsonSource."
            ),
        };

    private static bool LayerDefinitionsEqual(
        IReadOnlyList<MapLayerDefinition> previous,
        IReadOnlyList<MapLayerDefinition> current
    ) =>
        previous.Count == current.Count
        && previous.Zip(current).All(values => LayerDefinitionsEqual(values.First, values.Second));

    private static bool LayerDefinitionsEqual(MapLayerDefinition previous, MapLayerDefinition current) =>
        previous.GetType() == current.GetType()
        && previous.Type == current.Type
        && previous.IdSuffix == current.IdSuffix
        && previous.Key == current.Key
        && ValuesEqual(previous.Filter, current.Filter)
        && previous.MinZoom == current.MinZoom
        && previous.MaxZoom == current.MaxZoom
        && previous.Visible == current.Visible
        && previous.BeforeLayerId == current.BeforeLayerId
        && previous.LayerGroup == current.LayerGroup
        && previous.BeforeLayerGroup == current.BeforeLayerGroup
        && previous.AfterLayerGroup == current.AfterLayerGroup
        && LayerDefinitionPropertiesEqual(previous, current);

    private static bool LayerDefinitionPropertiesEqual(MapLayerDefinition previous, MapLayerDefinition current) =>
        (previous, current) switch
        {
            (CircleLayerDefinition previousCircle, CircleLayerDefinition currentCircle) => StyleValuesEqual(
                previousCircle.Color,
                currentCircle.Color
            )
                && StyleValuesEqual(previousCircle.Radius, currentCircle.Radius)
                && StyleValuesEqual(previousCircle.Opacity, currentCircle.Opacity)
                && StyleValuesEqual(previousCircle.StrokeWidth, currentCircle.StrokeWidth)
                && StyleValuesEqual(previousCircle.StrokeColor, currentCircle.StrokeColor)
                && StyleValuesEqual(previousCircle.StrokeOpacity, currentCircle.StrokeOpacity)
                && previousCircle.PitchAlignment == currentCircle.PitchAlignment,
            (SymbolLayerDefinition previousSymbol, SymbolLayerDefinition currentSymbol) => StyleValuesEqual(
                previousSymbol.TextField,
                currentSymbol.TextField
            )
                && StyleValuesEqual(previousSymbol.TextSize, currentSymbol.TextSize)
                && ValuesEqual(previousSymbol.TextFont, currentSymbol.TextFont)
                && previousSymbol.TextAnchor == currentSymbol.TextAnchor
                && ValuesEqual(previousSymbol.TextOffset, currentSymbol.TextOffset)
                && StyleValuesEqual(previousSymbol.TextRotate, currentSymbol.TextRotate)
                && previousSymbol.TextPitchAlignment == currentSymbol.TextPitchAlignment
                && previousSymbol.TextRotationAlignment == currentSymbol.TextRotationAlignment
                && previousSymbol.TextTransform == currentSymbol.TextTransform
                && previousSymbol.TextMaxWidth == currentSymbol.TextMaxWidth
                && previousSymbol.TextAllowOverlap == currentSymbol.TextAllowOverlap
                && StyleValuesEqual(previousSymbol.TextColor, currentSymbol.TextColor)
                && StyleValuesEqual(previousSymbol.TextHaloColor, currentSymbol.TextHaloColor)
                && StyleValuesEqual(previousSymbol.TextHaloWidth, currentSymbol.TextHaloWidth)
                && StyleValuesEqual(previousSymbol.TextOpacity, currentSymbol.TextOpacity)
                && StyleValuesEqual(previousSymbol.IconImage, currentSymbol.IconImage)
                && StyleValuesEqual(previousSymbol.IconSize, currentSymbol.IconSize)
                && StyleValuesEqual(previousSymbol.IconRotate, currentSymbol.IconRotate)
                && ValuesEqual(previousSymbol.IconOffset, currentSymbol.IconOffset)
                && StyleValuesEqual(previousSymbol.IconAnchor, currentSymbol.IconAnchor)
                && previousSymbol.IconAllowOverlap == currentSymbol.IconAllowOverlap
                && previousSymbol.IconTextFit == currentSymbol.IconTextFit
                && ValuesEqual(previousSymbol.IconTextFitPadding, currentSymbol.IconTextFitPadding)
                && previousSymbol.RotationAlignment == currentSymbol.RotationAlignment
                && StyleValuesEqual(previousSymbol.IconOpacity, currentSymbol.IconOpacity)
                && StyleValuesEqual(previousSymbol.IconColor, currentSymbol.IconColor)
                && previousSymbol.Placement == currentSymbol.Placement
                && previousSymbol.Spacing == currentSymbol.Spacing
                && StyleValuesEqual(previousSymbol.SymbolSortKey, currentSymbol.SymbolSortKey),
            _ => false,
        };

    private static bool StyleValuesEqual<T>(StyleValue<T>? previous, StyleValue<T>? current) =>
        (previous, current) switch
        {
            (null, null) => true,
            (null, _) or (_, null) => false,
            _ => ValuesEqual(previous.Value.ToSerializable(), current.Value.ToSerializable()),
        };

    private void ValidateUniqueDefinitionLayerIds(IReadOnlyList<MapLayerDefinition> layerDefinitions)
    {
        var duplicateLayerIds = layerDefinitions
            .Select(layerDefinition => layerDefinition.ResolveId(Id))
            .GroupBy(layerId => layerId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateLayerIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"GeoJsonSource '{Id}' has duplicate resolved layer id(s): {string.Join(", ", duplicateLayerIds)}. "
                    + "Generated cluster layers and parameter layer definitions must resolve to unique layer ids."
            );
        }
    }

    private static bool ClusterLayerEventRegistrationsEqual(
        IReadOnlyList<string> registeredLayerIds,
        IEnumerable<string> currentLayerIds
    ) => registeredLayerIds.SequenceEqual(currentLayerIds);

    private static bool SourceConfigurationsEqual(
        IReadOnlyDictionary<string, object?> previous,
        IReadOnlyDictionary<string, object?> current
    ) =>
        previous.Count == current.Count
        && previous.All(kv => current.TryGetValue(kv.Key, out var value) && ValuesEqual(kv.Value, value));

    private static Dictionary<string, object?> CloneSourceConfiguration(
        IReadOnlyDictionary<string, object?> sourceConfiguration
    ) => sourceConfiguration.ToDictionary(kv => kv.Key, kv => CloneValue(kv.Value), StringComparer.Ordinal);

    private static object? CloneValue(object? value) =>
        value switch
        {
            IReadOnlyDictionary<string, object?> dictionary => dictionary.ToDictionary(
                kv => kv.Key,
                kv => CloneValue(kv.Value),
                StringComparer.Ordinal
            ),
            IDictionary<string, object?> dictionary => dictionary.ToDictionary(
                kv => kv.Key,
                kv => CloneValue(kv.Value),
                StringComparer.Ordinal
            ),
            string => value,
            System.Collections.IEnumerable enumerable => enumerable.Cast<object?>().Select(CloneValue).ToArray(),
            _ => value,
        };

    private static bool ValuesEqual(object? previous, object? current) =>
        (previous, current) switch
        {
            (null, null) => true,
            (null, _) or (_, null) => false,
            (
                IReadOnlyDictionary<string, object?> previousDictionary,
                IReadOnlyDictionary<string, object?> currentDictionary
            ) => SourceObjectDictionariesEqual(previousDictionary, currentDictionary),
            (IDictionary<string, object?> previousDictionary, IDictionary<string, object?> currentDictionary) =>
                SourceObjectDictionariesEqual(previousDictionary, currentDictionary),
            (string, string) => Equals(previous, current),
            (System.Collections.IEnumerable previousEnumerable, System.Collections.IEnumerable currentEnumerable) =>
                EnumerablesEqual(previousEnumerable, currentEnumerable),
            _ when IsNumber(previous) && IsNumber(current) => Convert.ToDouble(previous, CultureInfo.InvariantCulture)
                == Convert.ToDouble(current, CultureInfo.InvariantCulture),
            _ => Equals(previous, current),
        };

    private static bool EnumerablesEqual(
        System.Collections.IEnumerable previous,
        System.Collections.IEnumerable current
    )
    {
        var previousValues = previous.Cast<object?>().ToArray();
        var currentValues = current.Cast<object?>().ToArray();

        return previousValues.Length == currentValues.Length
            && previousValues.Zip(currentValues).All(values => ValuesEqual(values.First, values.Second));
    }

    private static bool IsNumber(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static bool SourceObjectDictionariesEqual(
        IReadOnlyDictionary<string, object?> previous,
        IReadOnlyDictionary<string, object?> current
    ) =>
        previous.Count == current.Count
        && previous.All(kv => current.TryGetValue(kv.Key, out var value) && ValuesEqual(kv.Value, value));

    private static bool SourceObjectDictionariesEqual(
        IDictionary<string, object?> previous,
        IDictionary<string, object?> current
    ) =>
        previous.Count == current.Count
        && previous.All(kv => current.TryGetValue(kv.Key, out var value) && ValuesEqual(kv.Value, value));

    /// <summary>
    /// Gets the zoom level at which a cluster expands into its children.
    /// Use this with <see cref="BaseMap.FlyToAsync"/> to zoom into a cluster on click.
    /// </summary>
    /// <param name="clusterId">The cluster's ID (from the <c>cluster_id</c> feature property).</param>
    /// <returns>The zoom level at which the cluster expands.</returns>
    public async ValueTask<double> GetClusterExpansionZoomAsync(int clusterId)
    {
        return await _jsRuntime.InvokeAsync<double>(
            "Spillgebees.Map.mapFunctions.getClusterExpansionZoom",
            Map!.MapReference,
            Id,
            clusterId
        );
    }

    [JSInvokable("OnLayerClickAsync")]
    public async Task OnLayerClickAsync(double latitude, double longitude, JsonElement? properties)
    {
        if (Map is null || GetEffectiveClusterOptions().ClickBehavior != ClusterClickBehavior.ZoomToDissolve)
        {
            return;
        }

        if (!TryGetClusterId(properties, out var clusterId))
        {
            return;
        }

        var zoom = await GetClusterExpansionZoomAsync(clusterId);
        await Map.FlyToAsync(new Coordinate(latitude, longitude), (int)Math.Ceiling(zoom));
    }

    private static bool TryGetClusterId(JsonElement? properties, out int clusterId)
    {
        clusterId = 0;
        if (properties is null || !properties.Value.TryGetProperty("cluster_id", out var clusterIdProperty))
        {
            return false;
        }

        return clusterIdProperty.ValueKind switch
        {
            JsonValueKind.Number => clusterIdProperty.TryGetInt32(out clusterId),
            JsonValueKind.String => int.TryParse(clusterIdProperty.GetString(), out clusterId),
            _ => false,
        };
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_isInitialized && Map is not null)
        {
            try
            {
                await Map.SceneRegistry.UnregisterSourceAsync(Id);
            }
            catch (JSDisconnectedException) { }
            catch (ObjectDisposedException) { }
        }

        _dotNetRef?.Dispose();

        GC.SuppressFinalize(this);
    }
}
