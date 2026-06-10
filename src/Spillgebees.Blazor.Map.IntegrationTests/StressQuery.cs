namespace Spillgebees.Blazor.Map.IntegrationTests;

/// <summary>
/// Minimal query string parser so stress/benchmark pages can be configured by Playwright runs.
/// </summary>
public sealed class StressQuery
{
    private readonly Dictionary<string, string> _values;

    private StressQuery(Dictionary<string, string> values)
    {
        _values = values;
    }

    public static StressQuery Parse(string uri)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var queryStart = uri.IndexOf('?', StringComparison.Ordinal);
        if (queryStart < 0 || queryStart == uri.Length - 1)
        {
            return new StressQuery(values);
        }

        foreach (var pair in uri[(queryStart + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=', StringComparison.Ordinal);
            if (separator < 0)
            {
                values[Uri.UnescapeDataString(pair)] = "true";
                continue;
            }

            values[Uri.UnescapeDataString(pair[..separator])] = Uri.UnescapeDataString(pair[(separator + 1)..]);
        }

        return new StressQuery(values);
    }

    public int GetInt(string name, int fallback) =>
        _values.TryGetValue(name, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

    public bool GetBool(string name, bool fallback) =>
        _values.TryGetValue(name, out var value)
            ? value is "1" or "true" or "True" or "yes"
            : fallback;

    public bool TryGetEnum<TEnum>(string name, out TEnum result)
        where TEnum : struct =>
        Enum.TryParse(_values.TryGetValue(name, out var value) ? value : null, ignoreCase: true, out result);
}
