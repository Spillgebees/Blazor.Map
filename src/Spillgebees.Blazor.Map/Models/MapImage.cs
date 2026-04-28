namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// Declares a map image that should be registered before dependent layers are rendered.
/// Supports regular image URLs, base64/data URLs, and SVG URLs/data URLs.
/// </summary>
public sealed record MapImage
{
    /// <summary>
    /// Creates a new map image definition.
    /// </summary>
    /// <param name="id">Unique image identifier used by layer <c>icon-image</c> expressions.</param>
    /// <param name="url">Image URL or data URI.</param>
    /// <param name="width">Image width in CSS pixels.</param>
    /// <param name="height">Image height in CSS pixels.</param>
    /// <param name="pixelRatio">Image pixel ratio. Default is 1.</param>
    /// <param name="isSdf">Whether the image should be treated as SDF for runtime tinting.</param>
    public MapImage(string id, string url, int width, int height, double pixelRatio = 1, bool isSdf = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        if (!double.IsFinite(pixelRatio) || pixelRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelRatio), "Pixel ratio must be greater than zero.");
        }

        Id = id;
        Url = url;
        Width = width;
        Height = height;
        PixelRatio = pixelRatio;
        IsSdf = isSdf;
    }

    public string Id { get; }

    public string Url { get; }

    public int Width { get; }

    public int Height { get; }

    public double PixelRatio { get; }

    public bool IsSdf { get; }
}
