namespace IoT.Dashboard.Helpers;

public static class Extensions
{
    /// <summary>
    /// Converts object to string but doesn't crash if it's null
    /// </summary>
    public static string ToStringNullable(this object obj)
    {
        return (obj ?? "").ToString();
    }
    /// <summary>
    /// Converts object to string but doesn't crash if it's null
    /// </summary>
    public static string ToStringNullable(this object obj, string defaultValue)
    {
        return (obj ?? defaultValue).ToString();
    }
}
