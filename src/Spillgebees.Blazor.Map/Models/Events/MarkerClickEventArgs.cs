namespace Spillgebees.Blazor.Map;

/// <summary>
/// Event arguments for a marker click event.
/// </summary>
/// <param name="MarkerId">The unique identifier of the clicked marker.</param>
/// <param name="Position">The geographical coordinate of the marker.</param>
public record MarkerClickEventArgs(string MarkerId, Coordinate Position);
