using System.Text.Json;

namespace Spillgebees.Blazor.Map.Models.Events;

/// <summary>
/// Event arguments for interactions with features in a map layer.
/// </summary>
/// <param name="LayerId">The ID of the layer that was interacted with.</param>
/// <param name="Position">The geographic coordinate of the interaction.</param>
/// <param name="Properties">The feature's properties as a JSON object.</param>
public record LayerFeatureEventArgs(string LayerId, Coordinate Position, JsonElement? Properties);
