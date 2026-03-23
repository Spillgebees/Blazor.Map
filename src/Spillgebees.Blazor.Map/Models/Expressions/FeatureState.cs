namespace Spillgebees.Blazor.Map.Models.Expressions;

/// <summary>
/// Factory for creating typed feature state keys.
/// Feature state keys are consumer-defined — they can be any name.
/// Define a key once, then use it consistently in both
/// <c>SetFeatureStateAsync</c> and layer style expressions.
/// </summary>
public static class FeatureState
{
    /// <summary>Creates a boolean feature state key.</summary>
    public static FeatureStateKey<bool> Bool(string name) => new(name);

    /// <summary>Creates a numeric feature state key.</summary>
    public static FeatureStateKey<double> Number(string name) => new(name);

    /// <summary>Creates a string feature state key.</summary>
    public static FeatureStateKey<string> String(string name) => new(name);
}

/// <summary>
/// A typed feature state key. Define once, use in both
/// <see cref="FeatureStateKey{T}.Set"/> (for <c>SetFeatureStateAsync</c>) and
/// <see cref="FeatureStateKey{T}.When{TResult}"/> (for style expressions).
/// </summary>
/// <typeparam name="T">The value type of this state key (bool, double, or string).</typeparam>
public sealed class FeatureStateKey<T>
{
    /// <summary>The state key name.</summary>
    public string Name { get; }

    internal FeatureStateKey(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Creates a state dictionary entry for use with <c>SetFeatureStateAsync</c>.
    /// </summary>
    public KeyValuePair<string, object> Set(T value) => new(Name, value!);

    /// <summary>
    /// Creates a MapLibre expression that reads this feature state and returns
    /// different values based on whether it's truthy.
    /// </summary>
    /// <typeparam name="TResult">The result type (inferred from the values).</typeparam>
    /// <param name="trueValue">Value when the state is truthy.</param>
    /// <param name="falseValue">Value when the state is falsy or unset.</param>
    /// <returns>A MapLibre expression array.</returns>
    public object[] When<TResult>(TResult trueValue, TResult falseValue)
    {
        return
        [
            "case",
            new object[] { "boolean", new object[] { "feature-state", Name }, false },
            trueValue!,
            falseValue!,
        ];
    }

    /// <summary>
    /// Creates a MapLibre expression that reads this feature state with a fallback.
    /// </summary>
    public object[] Read(T fallbackValue) => ["coalesce", new object[] { "feature-state", Name }, fallbackValue!];
}
