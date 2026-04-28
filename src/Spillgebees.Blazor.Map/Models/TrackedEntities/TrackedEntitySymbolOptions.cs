using Spillgebees.Blazor.Map.Models.Options;
using Spillgebees.Blazor.Map.Models.Popups;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Primary symbol selectors for high-level tracked entity items, including optional popup configuration.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedEntitySymbolOptions<TItem>(
    Func<TItem, Coordinate> PositionSelector,
    Func<TItem, string> IconImageSelector,
    Func<TItem, double?>? SizeSelector = null,
    Func<TItem, double?>? RotationSelector = null,
    Func<TItem, SymbolAnchor?>? AnchorSelector = null,
    Func<TItem, PixelPoint?>? OffsetSelector = null,
    Func<TItem, string?>? ColorSelector = null,
    Func<TItem, TrackedEntityHoverIntent?>? HoverSelector = null,
    Func<TItem, double?>? RenderOrderSelector = null,
    Func<TItem, IReadOnlyDictionary<string, object?>?>? PropertiesSelector = null,
    Func<TItem, PopupOptions?>? PopupSelector = null
)
{
    public Coordinate GetPosition(TItem item) => PositionSelector(item);

    public string GetIconImage(TItem item) => IconImageSelector(item);

    public double? GetSize(TItem item) => SizeSelector?.Invoke(item);

    public double? GetRotation(TItem item) => RotationSelector?.Invoke(item);

    public SymbolAnchor? GetAnchor(TItem item) => AnchorSelector?.Invoke(item);

    public PixelPoint? GetOffset(TItem item) => OffsetSelector?.Invoke(item);

    public string? GetColor(TItem item) => ColorSelector?.Invoke(item);

    public TrackedEntityHoverIntent? GetHover(TItem item) => HoverSelector?.Invoke(item);

    public double? GetRenderOrder(TItem item) => RenderOrderSelector?.Invoke(item);

    public IReadOnlyDictionary<string, object?>? GetProperties(TItem item) => PropertiesSelector?.Invoke(item);

    public PopupOptions? GetPopup(TItem item) => PopupSelector?.Invoke(item);
}
