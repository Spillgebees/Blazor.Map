using Spillgebees.Blazor.Map.Models.TrackedEntities;

namespace Spillgebees.Blazor.Map.Models.TrackedData;

/// <summary>
/// Materializes raw tracked items into low-level tracked entities.
/// </summary>
public static class TrackedDataEntityMaterializer
{
    /// <summary>
    /// Converts raw items into tracked entities.
    /// </summary>
    public static IReadOnlyList<TrackedEntity<TItem>> Materialize<TItem>(
        IReadOnlyList<TItem> items,
        TrackedDataIdOptions<TItem> id,
        TrackedDataSymbolOptions<TItem> symbol,
        IReadOnlyList<TrackedDataDecorationOptions<TItem>>? decorations = null
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(symbol);

        return items.Select(item => MaterializeEntity(item, id, symbol, decorations ?? [])).ToArray();
    }

    private static TrackedEntity<TItem> MaterializeEntity<TItem>(
        TItem item,
        TrackedDataIdOptions<TItem> id,
        TrackedDataSymbolOptions<TItem> symbol,
        IReadOnlyList<TrackedDataDecorationOptions<TItem>> decorations
    )
    {
        var trackedDecorations = decorations
            .Select(decoration => MaterializeDecoration(item, decoration))
            .Where(decoration => decoration is not null)
            .Cast<TrackedEntityDecoration>()
            .ToArray();

        return new TrackedEntity<TItem>(
            id.GetId(item),
            symbol.GetPosition(item),
            new TrackedEntitySymbol(
                symbol.GetIconImage(item),
                symbol.GetSize(item),
                symbol.GetRotation(item),
                symbol.GetAnchor(item),
                symbol.GetOffset(item)
            ),
            color: NormalizeOptionalString(symbol.GetColor(item)),
            hover: symbol.GetHover(item),
            renderOrder: symbol.GetRenderOrder(item),
            decorations: trackedDecorations,
            item: item,
            properties: symbol.GetProperties(item)
        );
    }

    private static TrackedEntityDecoration? MaterializeDecoration<TItem>(
        TItem item,
        TrackedDataDecorationOptions<TItem> decoration
    )
    {
        var text = NormalizeOptionalString(decoration.GetText(item));
        var iconImage = NormalizeOptionalString(decoration.GetIconImage(item));

        if (text is null && iconImage is null)
        {
            return null;
        }

        return new TrackedEntityDecoration(
            decoration.Id,
            text,
            iconImage,
            decoration.Offset,
            decoration.Anchor,
            decoration.DisplayMode,
            decoration.GetColor(item),
            decoration.GetTextSize(item),
            decoration.GetIconSize(item),
            decoration.GetRotation(item),
            decoration.GetRenderOrder(item),
            decoration.GetHaloColor(item),
            decoration.GetHaloWidth(item),
            decoration.GetIconColor(item)
        );
    }

    private static string? NormalizeOptionalString(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
