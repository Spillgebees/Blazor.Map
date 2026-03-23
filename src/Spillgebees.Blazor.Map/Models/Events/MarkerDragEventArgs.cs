namespace Spillgebees.Blazor.Map.Models.Events;

/// <summary>
/// Event arguments for a marker drag end event.
/// </summary>
/// <param name="MarkerId">The unique identifier of the dragged marker.</param>
/// <param name="Position">The new geographical coordinate of the marker after dragging.</param>
public record MarkerDragEventArgs(string MarkerId, Coordinate Position);
