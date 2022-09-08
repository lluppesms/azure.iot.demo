namespace IoT.Dashboard.Models;

/// <summary>
/// Device File
/// </summary>
public class DeviceFile
{
    /// <summary>
    /// Row Id
    /// </summary>
    public int RowId { get; set; }

    /// <summary>
    /// DeviceId
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// Short File Name
    /// </summary>
    public string ShortFileName { get; set; }

    /// <summary>
    /// File Name
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// File Time
    /// </summary>
    public DateTime FileTime { get; set; }

    /// <summary>
    /// File Size
    /// </summary>
    public long FileSize { get; set; }
}
