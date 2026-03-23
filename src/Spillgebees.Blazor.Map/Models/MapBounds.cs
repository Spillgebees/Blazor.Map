namespace Spillgebees.Blazor.Map.Models;

/// <summary>
/// A geographic bounding box defined by its southwest and northeast corners.
/// Used to constrain the map viewport via <see cref="MapOptions.MaxBounds"/>.
/// </summary>
/// <param name="Southwest">The southwest corner of the bounds.</param>
/// <param name="Northeast">The northeast corner of the bounds.</param>
public record MapBounds(Coordinate Southwest, Coordinate Northeast);
