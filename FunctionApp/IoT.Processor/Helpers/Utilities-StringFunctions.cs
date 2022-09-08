using System.Text.RegularExpressions;

namespace IoT.Processor.Helpers;

/// <summary>
/// Common Functions
/// </summary>
public static partial class Utilities
{
    /// <summary>
    /// Validates the device Id format.
    /// </summary>
    public static bool ValidateIoTDeviceId(string deviceId)
    {
        var r = new Regex("^[a-z0-9-]*$");
        if (!r.IsMatch(deviceId))
        {
            var msg = "Invalid deviceId: ID must be alphanumeric, lowercase, and it may contain hyphens.";
            Trace.WriteLine(msg);
            MyLogger.LogError(msg, Constants.TriggerSource.Unknown);
            return false;
            //throw new FormatException("Invalid deviceId: The ID must be alphanumeric, lowercase, and may contain hyphens");
        }
        return true;
    }

    /// <summary>
    /// Derive Serial Number From FileName
    /// </summary>
    public static string DeriveDeviceIdFromFileName(string fileName)
    {
        //// Examples:
        ////   "/mydevice/fileName123.json = mydevice
        ////   "/mydevice/Heartbeats.json" = mydevice
        ////   "mydevice/Heartbeats.json" = mydevice
        ////   "/mydevice/Heartbeats-1-2-3.json" = mydevice
        ////   "mydevice.json" = mydevice
        ////   "mydevice-1-2-3.json" = mydevice
        var deviceId = fileName;
        if (!string.IsNullOrEmpty(deviceId))
        {
            if (deviceId.StartsWith("/"))
            {
                deviceId = deviceId[1..];
            }
            var slashNdx = deviceId.IndexOf("/", 1);
            if (slashNdx > 0)
            {
                deviceId = deviceId[..slashNdx];
                return deviceId;
            }
            var hyphenNdx = deviceId.IndexOf("-");
            if (hyphenNdx > 0)
            {
                deviceId = deviceId[..hyphenNdx];
                return deviceId;
            }
            var extensionNdx = deviceId.IndexOf(".");
            if (extensionNdx > 0)
            {
                deviceId = deviceId[..extensionNdx];
                return deviceId;
            }
        }
        return deviceId;
    }

        /// <summary>
    /// Derive Serial Number From Blob Name
    /// </summary>
    public static string DeriveDeviceIdFromBlobName(string fileName)
    {
        //// Examples:
        ////   "/blobServices/default/containers.../mydevice/Heartbeats-2022-06-29.json" = mydevice
        ////   "/blobServices/.../blobs/mydevice/Heartbeats.json" = mydevice
        ////   "mydevice/Heartbeats.json" = mydevice
        ////   "mydevice.json" = mydevice
        var deviceId = fileName;
        if (!string.IsNullOrEmpty(deviceId))
        {
            var lastSlashNdx = deviceId.LastIndexOf("/");
            if (lastSlashNdx > 0)
            {
                deviceId = deviceId[..lastSlashNdx];
                lastSlashNdx = deviceId.LastIndexOf("/");
                if (lastSlashNdx > 0)
                {
                    lastSlashNdx++;
                    deviceId = deviceId[lastSlashNdx..];
                    return deviceId;
                }
                return deviceId;
            }
            var extensionNdx = deviceId.IndexOf(".");
            if (extensionNdx > 0)
            {
                deviceId = deviceId[..extensionNdx];
                return deviceId;
            }
        }
        return deviceId;
    }

    /// <summary>
    /// Returns digits - checks to see if string is all numbers, like isnumeric, but works better... commas and periods are ok
    /// </summary>
    public static int ReturnOnlyNumbers(string textToConvert)
    {
        const string Digits = "0123456789";
        var resultString = "0";
        var resultLength = 0;
        try
        {
            int x;
            for (x = 0; x <= textToConvert.Length - 1; x++)
            {
                var lowerCaseChar = textToConvert.Substring(x, 1);
                if (Digits.Contains(lowerCaseChar))
                {
                    resultString += lowerCaseChar;
                    resultLength += 1;
                    if (resultLength > 8)
                    {
                        break;
                    }
                }
            }
            return Convert.ToInt32(resultString);
        }
        catch (Exception ex)
        {
            var message = GetExceptionMessage(ex);
            Console.WriteLine("IsOnlyNumbers: " + message);
            return 9999;
        }
    }

    /// <summary>
    /// Validates that this string has only numbers
    /// </summary>
    public static string IsOnlyLetters(string input)
    {
        const string ValidChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return IsOnlyTheseCharacters(input, 999, ValidChars);
    }

    /// <summary>
    /// Validates that this string has only number or letters
    /// </summary>
    public static string IsOnlyNumbersOrLetters(string input, int maxLength)
    {
        const string ValidChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-.";
        return IsOnlyTheseCharacters(input, maxLength, ValidChars);
    }

    /// <summary>
    /// Validates that this string has only allowed characters
    /// </summary>
    public static string IsOnlyTheseCharacters(string input, int maxLength, string validCharacters)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            if (sb.Length < maxLength)
            {
                if (validCharacters.Contains(input[i]))
                {
                    sb.Append(input[i]);
                }
            }
            else
            {
                break;
            }
        }
        var newString = sb.ToString();
        return newString;
    }

    /// <summary>
    /// Combines all the inner exception messages into one string
    /// </summary>
    public static string GetExceptionMessage(Exception ex)
    {
        var message = string.Empty;
        if (ex == null)
        {
            return message;
        }
        if (ex.Message != null)
        {
            message += ex.Message;
        }
        if (ex.InnerException == null)
        {
            return message;
        }
        if (ex.InnerException.Message != null)
        {
            message += " " + ex.InnerException.Message;
        }
        if (ex.InnerException.InnerException == null)
        {
            return message;
        }
        if (ex.InnerException.InnerException.Message != null)
        {
            message += " " + ex.InnerException.InnerException.Message;
        }
        if (ex.InnerException.InnerException.InnerException == null)
        {
            return message;
        }
        if (ex.InnerException.InnerException.InnerException.Message != null)
        {
            message += " " + ex.InnerException.InnerException.InnerException.Message;
        }
        return message;
    }

    /// <summary>
    /// Sanitize connection string
    /// </summary>
    public static string GetSanitizedConnectionString(string connection)
    {
        //// "DeviceConnectionString": "HostName=iothub123.azure-devices.net;DeviceId=test1;SharedAccessKey=E5Z6******=",
        //// "SQLConnectionString": "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword";
        string noKey;
        if (string.IsNullOrEmpty(connection)) return string.Empty;
        var keyPos = connection.IndexOf("key=", StringComparison.OrdinalIgnoreCase);
        if (keyPos > 0)
        {
            noKey = string.Concat(connection.AsSpan(0, keyPos + 4), "...");
            return noKey;
        }
        keyPos = connection.IndexOf("pwd=", StringComparison.OrdinalIgnoreCase);
        if (keyPos > 0)
        {
            noKey = string.Concat(connection.AsSpan(0, keyPos + 4), "...");
            return noKey;
        }
        keyPos = connection.IndexOf("password=", StringComparison.OrdinalIgnoreCase);
        if (keyPos > 0)
        {
            noKey = string.Concat(connection.AsSpan(0, keyPos + 9), "...");
            return noKey;
        }
        return connection;
    }

    /// <summary>
    /// Get an environment variable
    /// </summary>
    public static string GetEnvironmentVariable(string name)
    {
        //return name + ": " + Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }

    /// <summary>
    /// Create a stream from a string
    /// </summary>
    public static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
