namespace Spillgebees.Blazor.Map.Models.Layers;

/// <summary>
/// Represents a custom icon for a map marker.
/// </summary>
/// <param name="Url">The URL to the icon image. Can be absolute, relative, or a data URI (inline SVG).</param>
/// <param name="Size">Size of the icon in pixels (width, height). Default is <see langword="null"/> (auto-detect).</param>
/// <param name="Anchor">
/// The coordinates of the "tip" of the icon relative to its top-left corner (x, y).
/// Default is <see langword="null"/> (centered).
/// </param>
public record MarkerIcon(string Url, PixelPoint? Size = null, PixelPoint? Anchor = null);
