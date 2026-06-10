using Microsoft.AspNetCore.Components;

namespace Spillgebees.Blazor.Map;

/// <summary>
/// Controls when a tracked entity decoration is visible. Implemented with MapLibre
/// feature-state expressions — visibility changes never cross the interop boundary.
/// </summary>
public enum EntityDecorationDisplayMode
{
    /// <summary>The decoration is always visible.</summary>
    Always,

    /// <summary>The decoration is visible only while its entity is hovered.</summary>
    OnHover,

    /// <summary>The decoration is visible while its entity is hovered or selected.</summary>
    OnHoverOrSelect,
}

/// <summary>
/// A decoration (label and/or icon) that follows each entity of a
/// <see cref="TrackedEntityLayer{TItem}"/>. Declare as a child of the layer:
/// <code>
/// &lt;TrackedEntityLayer ...&gt;
///     &lt;EntityDecoration Id="label" Text="@(v =&gt; v.Name)" DisplayMode="OnHover" /&gt;
/// &lt;/TrackedEntityLayer&gt;
/// </code>
/// Configuration is captured once when the layer initializes; per-item values flow
/// through the <see cref="Text"/>, <see cref="Icon"/>, and <see cref="Color"/> selectors.
/// </summary>
public sealed class EntityDecoration<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter]
    internal TrackedEntityLayer<TItem>? Layer { get; set; }

    /// <summary>Stable decoration id, unique within the layer.</summary>
    [Parameter, EditorRequired]
    public string Id { get; set; } = "";

    /// <summary>Extracts the per-item label text rendered next to the entity.</summary>
    [Parameter]
    public Func<TItem, string?>? Text { get; set; }

    /// <summary>Per-item icon image id (registered via map images).</summary>
    [Parameter]
    public Func<TItem, string?>? Icon { get; set; }

    /// <summary>Part of the decoration placed closest to the entity position. Defaults to <see cref="SymbolAnchor.Center"/>.</summary>
    [Parameter]
    public SymbolAnchor Anchor { get; set; } = SymbolAnchor.Center;

    /// <summary>Offset from the entity position, in CSS pixels.</summary>
    [Parameter]
    public PixelPoint? Offset { get; set; }

    /// <summary>When the decoration is visible. Defaults to <see cref="EntityDecorationDisplayMode.Always"/>.</summary>
    [Parameter]
    public EntityDecorationDisplayMode DisplayMode { get; set; } = EntityDecorationDisplayMode.Always;

    /// <summary>Text size in pixels. Defaults to 11.</summary>
    [Parameter]
    public double TextSize { get; set; } = 11;

    /// <summary>Font stack for the decoration text (style glyph names).</summary>
    [Parameter]
    public string[]? TextFont { get; set; }

    /// <summary>Per-item text/icon color; falls back to the entity color, then black.</summary>
    [Parameter]
    public Func<TItem, string?>? Color { get; set; }

    /// <summary>Halo color drawn around the decoration text (MapLibre <c>text-halo-color</c>).</summary>
    [Parameter]
    public string? HaloColor { get; set; }

    /// <summary>Halo width in pixels; applies when <see cref="HaloColor"/> is set, defaulting to 1.</summary>
    [Parameter]
    public double? HaloWidth { get; set; }

    /// <summary>Icon scale factor relative to the image's native size. Defaults to 1.0.</summary>
    [Parameter]
    public double IconSize { get; set; } = 1.0;

    /// <summary>Registers the decoration with its parent <see cref="TrackedEntityLayer{TItem}"/>.</summary>
    protected override void OnInitialized()
    {
        if (Layer is null)
        {
            throw new InvalidOperationException(
                $"{nameof(EntityDecoration<>)} must be nested inside a TrackedEntityLayer."
            );
        }

        Layer.RegisterDecoration(this);
    }

    /// <summary>Unregisters the decoration from its parent layer.</summary>
    public void Dispose() => Layer?.UnregisterDecoration(this);
}
