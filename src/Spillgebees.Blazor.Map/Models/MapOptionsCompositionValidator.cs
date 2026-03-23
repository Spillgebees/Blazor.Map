namespace Spillgebees.Blazor.Map.Models;

internal static class MapOptionsCompositionValidator
{
    internal static void Validate(MapOptions mapOptions)
    {
        if (mapOptions.Styles is null)
        {
            return;
        }

        var styleIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var style in mapOptions.Styles)
        {
            if (string.IsNullOrWhiteSpace(style.Id))
            {
                throw new ArgumentException(
                    "Composed map styles must define a non-empty style ID.",
                    nameof(mapOptions)
                );
            }

            if (!styleIds.Add(style.Id))
            {
                throw new ArgumentException(
                    $"Composed map styles must use unique IDs. Duplicate ID '{style.Id}' was found.",
                    nameof(mapOptions)
                );
            }
        }
    }
}
