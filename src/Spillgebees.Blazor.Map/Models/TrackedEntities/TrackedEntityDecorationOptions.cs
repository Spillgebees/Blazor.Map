using Spillgebees.Blazor.Map.Models.Options;
using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Models.TrackedEntities;

/// <summary>
/// Decoration selectors for high-level tracked entity items.
/// </summary>
/// <typeparam name="TItem">The raw app model type.</typeparam>
public sealed record TrackedEntityDecorationOptions<TItem>(
    string Id,
    Func<TItem, string?>? TextSelector = null,
    Func<TItem, string?>? IconImageSelector = null,
    Point? Offset = null,
    SymbolAnchor? Anchor = null,
    TrackedEntityDecorationDisplayMode DisplayMode = TrackedEntityDecorationDisplayMode.Always,
    Func<TItem, string?>? ColorSelector = null,
    Func<TItem, double?>? TextSizeSelector = null,
    Func<TItem, double?>? IconSizeSelector = null,
    Func<TItem, double?>? RotationSelector = null,
    Func<TItem, double?>? RenderOrderSelector = null,
    Func<TItem, string?>? HaloColorSelector = null,
    Func<TItem, double?>? HaloWidthSelector = null,
    Func<TItem, string?>? IconColorSelector = null,
    IconTextFit? IconTextFit = null,
    double[]? IconTextFitPadding = null,
    string[]? TextFont = null
)
{
    public string? GetText(TItem item) => TextSelector?.Invoke(item);

    public string? GetIconImage(TItem item) => IconImageSelector?.Invoke(item);

    public string? GetColor(TItem item) => ColorSelector?.Invoke(item);

    public double? GetTextSize(TItem item) => TextSizeSelector?.Invoke(item);

    public double? GetIconSize(TItem item) => IconSizeSelector?.Invoke(item);

    public double? GetRotation(TItem item) => RotationSelector?.Invoke(item);

    public double? GetRenderOrder(TItem item) => RenderOrderSelector?.Invoke(item);

    public string? GetHaloColor(TItem item) => HaloColorSelector?.Invoke(item);

    public double? GetHaloWidth(TItem item) => HaloWidthSelector?.Invoke(item);

    public string? GetIconColor(TItem item) => IconColorSelector?.Invoke(item);
}
