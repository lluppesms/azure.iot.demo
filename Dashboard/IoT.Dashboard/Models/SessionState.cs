namespace IoT.Dashboard.Models;

/// <summary>
/// Session Variables
/// </summary>
public class SessionState
{
    /// <summary>
    /// Is Authenticated
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Last Device
    /// </summary>
    public string LastDevice { get; set; }
}
