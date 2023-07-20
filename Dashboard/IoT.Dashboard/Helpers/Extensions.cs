namespace IoT.Dashboard.Helpers;

public static class Extensions
{
    /// <summary>
    /// Converts object to string but doesn't crash if it's null
    /// </summary>
    public static string ToStringNullable(this object obj)
    {
        var str = (obj ?? string.Empty).ToString();
        return string.IsNullOrEmpty(str) ? string.Empty : str;
    }
    /// <summary>
    /// Converts object to string but doesn't crash if it's null
    /// </summary>
    public static string ToStringNullable(this object obj, string defaultValue)
    {
        var str = (obj ?? defaultValue).ToString();
        return string.IsNullOrEmpty(str) ? string.Empty : str;
    }
}
