using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Constants = IoT.Dashboard.Models.Constants;

namespace IoT.Dashboard.Helpers;

public class BlobStorageHelper
{
    #region Variables
    /// <summary>
    /// Configuration
    /// </summary>
    private AppSettings settings;

    /// <summary>
    /// Blob Service Client
    /// </summary>
    private static BlobServiceClient blobServiceClient = null;

    /// <summary>
    /// Blob Container Client
    /// </summary>
    private static BlobContainerClient blobContainerClient;

    /// <summary>
    /// Blob Storage Account
    /// </summary>
    private static CloudStorageAccount storageAccount = null;

    /// <summary>
    /// Blob Storage Client
    /// </summary>
    private static CloudBlobClient blobClient = null;

    /// <summary>
    /// Blob Storage Container
    /// </summary>
    private static CloudBlobContainer container = null;
    #endregion

    #region Initialization
    /// <summary>
    /// Constructor
    /// </summary>
    public BlobStorageHelper(AppSettings diSetting)
    {
        settings = diSetting;
    }
    #endregion
    
    /// <summary>
    /// List blobs for one device
    /// </summary>
    /// <returns></returns>
    public async Task<(List<DeviceFile>, string)> ListBlobs(string deviceId, int maxRows = 100)
    {
        var fileList = new List<DeviceFile>();
        var segmentSize = 25;
        var rowId = 0;
        var stopWatch = new Stopwatch();
        // see https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blobs-list
        try
        {
            stopWatch.Start();

            if (blobServiceClient == null)
            {
                blobServiceClient = new BlobServiceClient(settings.StorageConnectionString);
                blobContainerClient = blobServiceClient.GetBlobContainerClient(Constants.BlobStorage.FileUploads);
            }
            if (blobServiceClient == null) 
            { 
                var badDataMessage = $"Unable to create Storage Client!";
                MyLogger.LogError(badDataMessage, Constants.Source.StorageHelper);
                return (new List<DeviceFile>(), badDataMessage);
            }

            var blobs = blobContainerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, deviceId).AsPages(default, segmentSize);
            await foreach (var blobPage in blobs)
            {
                foreach (var blobItem in blobPage.Values)
                {
                    rowId++;
                    var fileName = blobItem.Name;
                    var startNdx = fileName.IndexOf("/") + 1;
                    if (startNdx > fileName.Length) { startNdx = 0; }
                    var thisFile = new DeviceFile
                    {
                        RowId = rowId,
                        DeviceId = deviceId,
                        FileTime = Utilities.ConvertFromDateTimeOffset(blobItem.Properties.LastModified.Value),
                        FileName = fileName,
                        ShortFileName = fileName[startNdx..],
                        FileSize = blobItem.Properties.ContentLength.Value
                    };
                    fileList.Add(thisFile);
                    if (rowId >= maxRows)
                    {
                        break;
                    }
                }
            }
            stopWatch.Stop();
            var msg = $"Found {fileList.Count} files for device {deviceId} in {stopWatch.ElapsedMilliseconds}ms";
            MyLogger.LogInfo(msg, Constants.Source.StorageHelper);
            return (fileList, msg);
        }
        catch (Exception ex)
        {
            var baseMsg = $"Error reading DeviceFiles from Storage Container!";
            var errorMsg = $"{baseMsg} {Constants.BlobStorage.FileUploads}. Error: {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, Constants.Source.StorageHelper);
            return (new List<DeviceFile>(), errorMsg);
        }
    }

    /// <summary>
    /// Read file stream from Storage Account Container
    /// </summary>
    public async Task<MemoryStream> GetBlobStreamFromStorageContainer(string fileName)
    {
        if (string.IsNullOrEmpty(settings.StorageConnectionString) || string.IsNullOrEmpty(fileName)) { return null; }
        try
        {
            (var blobExists, var blob) = await GetBlob(fileName);
            if (blobExists && blob != null)
            {
                var stream = new MemoryStream();
                await blob.DownloadToStreamAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
            return null;
        }
        catch (Exception ex)
        {
            MyLogger.LogError($"Error reading stream blob {fileName} in container {Constants.BlobStorage.FileUploads}: {Utilities.GetExceptionMessage(ex)}", Constants.Source.StorageHelper);
            return null;
        }
    }

    /// <summary>
    /// Read contents of file from Storage Account Container
    /// </summary>
    public async Task<string> GetBlobStringFromStorageContainer(string fileName)
    {
        var results = string.Empty;
        if (string.IsNullOrEmpty(settings.StorageConnectionString) || string.IsNullOrEmpty(fileName)) { return results; }
        try
        {
            (var blobExists, var blob) = await GetBlob(fileName);
            if (blobExists && blob != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(memoryStream);
                    memoryStream.Position = 0;
                    using (var reader = new StreamReader(memoryStream))
                    {
                        results = reader.ReadToEnd();
                    }
                }
            }
            return results;
        }
        catch (Exception ex)
        {
            MyLogger.LogError($"Error reading string contents of {fileName} in container {Constants.BlobStorage.FileUploads}: {Utilities.GetExceptionMessage(ex)}", Constants.Source.StorageHelper);
            return results;
        }
    }

    /// <summary>
    /// Read contents of file from Storage Account Container
    /// </summary>
    private async Task<(bool, CloudBlockBlob)> GetBlob(string fileName)
    {
        var results = string.Empty;
        if (string.IsNullOrEmpty(settings.StorageConnectionString) || string.IsNullOrEmpty(fileName)) { return (false, null); }
        try
        {
            if (storageAccount == null)
            {
                storageAccount = CloudStorageAccount.Parse(settings.StorageConnectionString);
                blobClient = storageAccount.CreateCloudBlobClient();
                container = blobClient.GetContainerReference(Constants.BlobStorage.FileUploads);
            }
            //////await container.CreateIfNotExistsAsync();  --> this throws a 409 error in App Insights... it's not really needed. 
            if (!await container.ExistsAsync()) { return (false, null); }
            
            var blob = container.GetBlockBlobReference(fileName);
            var blobExists = await blob.ExistsAsync();
            if (!blobExists)
            {
                if (fileName.StartsWith("/")) { fileName = fileName[1..]; }
                blob = container.GetBlockBlobReference(fileName);
                blobExists = await blob.ExistsAsync();
            }
            return (blobExists, blob);
        }
        catch (Exception ex)
        {
            MyLogger.LogError($"Error finding {fileName} in container {Constants.BlobStorage.FileUploads}: {Utilities.GetExceptionMessage(ex)}", Constants.Source.StorageHelper);
            return (false, null);
        }
    }

    /// <summary>
    /// Writes blob to Storage Account Container
    /// </summary>
    public async Task<bool> WriteBlobToStorageContainer(string containerName, string fileName, Stream dataStream)
    {
        try
        {
            if (storageAccount == null)
            {
                storageAccount = CloudStorageAccount.Parse(settings.StorageConnectionString);
                blobClient = storageAccount.CreateCloudBlobClient();
                container = blobClient.GetContainerReference(containerName);
                await container.CreateIfNotExistsAsync();
            }
            if (fileName.StartsWith("/")) { fileName = fileName[1..]; }
            var blob = container.GetBlockBlobReference(fileName);
            await blob.UploadFromStreamAsync(dataStream);
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error Writing to Blob {fileName} {Utilities.GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, Constants.Source.StorageHelper, null);
            return false;
        }
    }
}
