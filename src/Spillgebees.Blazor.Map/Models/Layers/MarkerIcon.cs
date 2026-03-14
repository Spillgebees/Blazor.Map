namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// Represents a custom icon for a map marker, based on an image URL.
/// See <a href="https://leafletjs.com/reference.html#icon">Leaflet Icon documentation</a>.
/// </summary>
/// <param name="IconUrl">The URL to the icon image (required). Can be absolute or relative.</param>
/// <param name="IconSize">
/// Size of the icon in pixels [width, height].
/// When <see langword="null" />, Leaflet auto-detects the size from the image.
/// </param>
/// <param name="IconAnchor">
/// The coordinates of the "tip" of the icon (relative to its top-left corner) [x, y].
/// When <see langword="null" />, the icon is centered if <paramref name="IconSize" /> is specified.
/// </param>
/// <param name="PopupAnchor">
/// The coordinates of the point from which popups will "open", relative to the icon anchor [x, y].
/// When <see langword="null" />, Leaflet defaults to [0, 0].
/// </param>
/// <param name="TooltipAnchor">
/// The coordinates of the point from which tooltips will "open", relative to the icon anchor [x, y].
/// When <see langword="null" />, Leaflet defaults to [0, 0].
/// </param>
/// <param name="ShadowUrl">
/// The URL to the icon shadow image.
/// When <see langword="null" />, no shadow is displayed.
/// </param>
/// <param name="ShadowSize">
/// Size of the shadow image in pixels [width, height].
/// When <see langword="null" />, Leaflet uses the same size as <paramref name="IconSize" />.
/// </param>
/// <param name="ShadowAnchor">
/// The coordinates of the "tip" of the shadow [x, y].
/// When <see langword="null" />, Leaflet uses the same value as <paramref name="IconAnchor" />.
/// </param>
/// <param name="ClassName">
/// A custom CSS class name to assign to both icon and shadow images.
/// When <see langword="null" />, Leaflet uses its default class names.
/// </param>
public record MarkerIcon(
    string IconUrl,
    int[]? IconSize = null,
    int[]? IconAnchor = null,
    int[]? PopupAnchor = null,
    int[]? TooltipAnchor = null,
    string? ShadowUrl = null,
    int[]? ShadowSize = null,
    int[]? ShadowAnchor = null,
    string? ClassName = null
)
{
    public virtual bool Equals(MarkerIcon? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return IconUrl == other.IconUrl
            && ArrayEquals(IconSize, other.IconSize)
            && ArrayEquals(IconAnchor, other.IconAnchor)
            && ArrayEquals(PopupAnchor, other.PopupAnchor)
            && ArrayEquals(TooltipAnchor, other.TooltipAnchor)
            && ShadowUrl == other.ShadowUrl
            && ArrayEquals(ShadowSize, other.ShadowSize)
            && ArrayEquals(ShadowAnchor, other.ShadowAnchor)
            && ClassName == other.ClassName;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IconUrl);
        AddArrayHash(ref hash, IconSize);
        AddArrayHash(ref hash, IconAnchor);
        AddArrayHash(ref hash, PopupAnchor);
        AddArrayHash(ref hash, TooltipAnchor);
        hash.Add(ShadowUrl);
        AddArrayHash(ref hash, ShadowSize);
        AddArrayHash(ref hash, ShadowAnchor);
        hash.Add(ClassName);
        return hash.ToHashCode();
    }

    private static bool ArrayEquals(int[]? a, int[]? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.AsSpan().SequenceEqual(b);
    }

    private static void AddArrayHash(ref HashCode hash, int[]? array)
    {
        if (array is null)
        {
            hash.Add(0);
            return;
        }

        foreach (var item in array)
        {
            hash.Add(item);
        }
    }
}
