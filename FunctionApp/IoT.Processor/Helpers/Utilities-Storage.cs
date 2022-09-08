namespace IoT.Processor.Helpers;

public static partial class Utilities
{
    /// <summary>
    /// List files in Storage Account Container
    /// </summary>
    public static async Task<List<IListBlobItem>> ListBlobsInStorageContainer(string containerName, string blobStorageConnectionString, string dataSource, StringBuilder sb)
    {
        var results = new List<IListBlobItem>();
        if (string.IsNullOrEmpty(blobStorageConnectionString) || string.IsNullOrEmpty(containerName)) { return results; }

        try
        {
            var storageAccount = CloudStorageAccount.Parse(blobStorageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            BlobContinuationToken continuationToken = null;
            do
            {
                var response = await container.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            }
            while (continuationToken != null);
            return results;
        }
        catch (Exception ex)
        {
            MyLogger.LogError($"Error listing container {containerName}: {GetExceptionMessage(ex)}", dataSource, string.Empty, sb);
            return results;
        }
    }

    /// <summary>
    /// Read file from Storage Account Container
    /// </summary>
    public static async Task<CloudBlockBlob> GetBlobFromStorageContainer(string fileName, string containerName, string blobStorageConnectionString, string dataSource, StringBuilder sb)
    {
        if (string.IsNullOrEmpty(blobStorageConnectionString) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(containerName)) { return null; }
        try
        {
            var storageAccount = CloudStorageAccount.Parse(blobStorageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            //////await container.CreateIfNotExistsAsync();  --> this throws a 409 error in App Insights... it's not really needed. 

            var blob = container.GetBlockBlobReference(fileName);
            var blobExists = await blob.ExistsAsync();
            if (!blobExists)
            {
                if (fileName.StartsWith("/")) { fileName = fileName[1..]; }
                blob = container.GetBlockBlobReference(fileName);
                blobExists = await blob.ExistsAsync();
            }

            return blobExists ? blob : null;
        }
        catch (Exception ex)
        {
            MyLogger.LogError($"Error finding blob {fileName} in blob container {containerName} using CS {blobStorageConnectionString}: {GetExceptionMessage(ex)}", dataSource, string.Empty, sb);
            return null;
        }
    }

    ///// <summary>
    ///// Read contents of file from Storage Account Container
    ///// </summary>
    //public static async Task<string> ReadBlobContentsFromStorageContainer(IListBlobItem blobItem, string containerName, string blobStorageConnectionString, string dataSource, StringBuilder sb)
    //{
    //    var results = string.Empty;
    //    var fileName = string.Empty;
    //    if (string.IsNullOrEmpty(blobStorageConnectionString) || string.IsNullOrEmpty(containerName)) { return results; }
    //    try
    //    {
    //        var storageAccount = CloudStorageAccount.Parse(blobStorageConnectionString);
    //        var blobClient = storageAccount.CreateCloudBlobClient();
    //        var container = blobClient.GetContainerReference(containerName);
    //        //////await container.CreateIfNotExistsAsync();  --> this throws a 409 error in App Insights... it's not really needed. 
    //        fileName = Path.GetFileName(blobItem.Uri.LocalPath);
    //        var blockBlob = container.GetBlockBlobReference(fileName);
    //        using (var memoryStream = new MemoryStream())
    //        {
    //            await blockBlob.DownloadToStreamAsync(memoryStream);
    //            memoryStream.Position = 0;
    //            using (var reader = new StreamReader(memoryStream))
    //            {
    //                results = reader.ReadToEnd();
    //            }
    //        }
    //        return results;
    //    }
    //    catch (Exception ex)
    //    {
    //        MyLogger.LogError($"Error reading contents of {fileName} from blob container {containerName}: {GetExceptionMessage(ex)}", dataSource, string.Empty, sb);
    //        return results;
    //    }
    //}

    /// <summary>
    /// Writes blob to Storage Account Container
    /// </summary>
    public static async Task<bool> WriteBlobToStorageContainer(string containerName, string fileName, Stream dataStream, string blobStorageConnectionString, string dataSource, StringBuilder sb)
    {
        try
        {
            var storageAccount = CloudStorageAccount.Parse(blobStorageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(containerName);
            await blobContainer.CreateIfNotExistsAsync();
            if (fileName.StartsWith("/")) { fileName = fileName[1..]; }
            var blob = blobContainer.GetBlockBlobReference(fileName);
            await blob.UploadFromStreamAsync(dataStream);
            return true;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error Writing to Blob {fileName} {GetExceptionMessage(ex)}";
            MyLogger.LogError(errorMsg, dataSource, string.Empty, sb);
            return false;
        }
    }

    /// <summary>
    /// Get uncompressed data from zip file
    /// </summary>
    public static byte[] GetUncompressedDataBytes(byte[] data)
    {
        using (var outputStream = new MemoryStream())
        {
            using (var inputStream = new MemoryStream(data))
            {
                using (var zipInputStream = new ZipInputStream(inputStream))
                {
                    zipInputStream.GetNextEntry();
                    zipInputStream.CopyTo(outputStream);
                }
                return outputStream.ToArray();
            }
        }
    }
    
    /// <summary>
    /// Get uncompressed data from zipped memory stream
    /// </summary>
    public static MemoryStream GetUncompressedDataStream(MemoryStream zippedStream)
    {
        zippedStream.Seek(0, SeekOrigin.Begin);
        var dataBytes = Utilities.GetUncompressedDataBytes(zippedStream.ToArray());
        var unzippedStream = new MemoryStream(dataBytes);
        unzippedStream.Seek(0, SeekOrigin.Begin);
        return unzippedStream;
    }
}
