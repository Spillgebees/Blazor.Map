namespace Spillgebees.Blazor.Map.Models.Expressions;

/// <summary>
/// Static helper for building MapLibre style expressions.
/// Expressions are serialized as JSON arrays for the JS interop.
/// </summary>
public static class Expr
{
    /// <summary>
    /// Access a feature property by name.
    /// </summary>
    public static object[] Get(string property) => ["get", property];

    /// <summary>
    /// Equality comparison: property == value.
    /// </summary>
    public static object[] Eq(string property, object value) => ["==", Get(property), value];

    /// <summary>
    /// Inequality comparison: property != value.
    /// </summary>
    public static object[] Neq(string property, object value) => ["!=", Get(property), value];

    /// <summary>
    /// Greater-than comparison: property &gt; value.
    /// </summary>
    public static object[] Gt(string property, object value) => [">", Get(property), value];

    /// <summary>
    /// Less-than comparison: property &lt; value.
    /// </summary>
    public static object[] Lt(string property, object value) => ["<", Get(property), value];

    /// <summary>
    /// Greater-than-or-equal comparison: property &gt;= value.
    /// </summary>
    public static object[] Gte(string property, object value) => [">=", Get(property), value];

    /// <summary>
    /// Less-than-or-equal comparison: property &lt;= value.
    /// </summary>
    public static object[] Lte(string property, object value) => ["<=", Get(property), value];

    /// <summary>
    /// Logical AND of all conditions.
    /// </summary>
    public static object[] All(params object[] conditions)
    {
        var result = new object[conditions.Length + 1];
        result[0] = "all";
        Array.Copy(conditions, 0, result, 1, conditions.Length);
        return result;
    }

    /// <summary>
    /// Logical OR of any condition.
    /// </summary>
    public static object[] Any(params object[] conditions)
    {
        var result = new object[conditions.Length + 1];
        result[0] = "any";
        Array.Copy(conditions, 0, result, 1, conditions.Length);
        return result;
    }

    /// <summary>
    /// Logical NOT of a condition.
    /// </summary>
    public static object[] Not(object condition) => ["!", condition];

    /// <summary>
    /// Check whether a feature has a property.
    /// </summary>
    public static object[] Has(string property) => ["has", property];

    /// <summary>
    /// Match a property value against cases. Pairs are: value1, result1, value2, result2, ..., fallback.
    /// Example: Expr.Match("type", "rail", "#333", "siding", "#999", "#ccc")
    /// </summary>
    public static object[] Match(string property, params object[] casesAndFallback)
    {
        var result = new object[casesAndFallback.Length + 2];
        result[0] = "match";
        result[1] = Get(property);
        Array.Copy(casesAndFallback, 0, result, 2, casesAndFallback.Length);
        return result;
    }

    /// <summary>
    /// Step function. Returns defaultValue when property &lt; first stop, then the value at each stop.
    /// Example: Expr.Step("point_count", 15, 10, 20, 30, 25) — default 15, &gt;=10→20, &gt;=30→25
    /// Stops are: threshold1, value1, threshold2, value2, ...
    /// </summary>
    public static object[] Step(string property, object defaultValue, params object[] stops)
    {
        var result = new object[stops.Length + 3];
        result[0] = "step";
        result[1] = Get(property);
        result[2] = defaultValue;
        Array.Copy(stops, 0, result, 3, stops.Length);
        return result;
    }

    /// <summary>
    /// Linear interpolation on a feature property. Stops are: value1, result1, value2, result2, ...
    /// </summary>
    public static object[] Interpolate(string property, params object[] stops)
    {
        var result = new object[stops.Length + 3];
        result[0] = "interpolate";
        result[1] = new object[] { "linear" };
        result[2] = Get(property);
        Array.Copy(stops, 0, result, 3, stops.Length);
        return result;
    }

    /// <summary>
    /// The current map zoom level. Use with <see cref="InterpolateZoom"/> for zoom-dependent styling.
    /// </summary>
    public static object[] Zoom => ["zoom"];

    /// <summary>
    /// Linear interpolation based on zoom level. Stops are: zoom1, value1, zoom2, value2, ...
    /// Example: <c>Expr.InterpolateZoom(8, 1.0, 14, 4.0)</c> — width 1 at z8, linearly scales to 4 at z14.
    /// </summary>
    public static object[] InterpolateZoom(params object[] stops)
    {
        var result = new object[stops.Length + 3];
        result[0] = "interpolate";
        result[1] = new object[] { "linear" };
        result[2] = Zoom;
        Array.Copy(stops, 0, result, 3, stops.Length);
        return result;
    }
}
