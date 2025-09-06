using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GainIt.API.Options;
using GainIt.API.Services.FileUpload.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace GainIt.API.Services.FileUpload.Implementations
{
    public class FileUploadService : IFileUploadService
    {
        private readonly BlobServiceClient _BlobServiceClient;
        private readonly AzureStorageOptions _Options;
        private readonly ILogger<FileUploadService> _Logger;
        private readonly string _ProfilePicturesContainerName;

        public FileUploadService(BlobServiceClient blobServiceClient, IOptions<AzureStorageOptions> options, ILogger<FileUploadService> logger)
        {
            _BlobServiceClient = blobServiceClient;
            _Options = options.Value;
            _Logger = logger;
            _ProfilePicturesContainerName = _Options.ProfilePicturesContainerName;
        }

        // Generic scalable methods for any container and file type
        public async Task<string> UploadFileAsync(IFormFile file, string containerName, string? subfolder = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is empty or null");
                }

                // Get container client for the specified container
                var containerClient = _BlobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                // Generate unique blob name
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var blobName = subfolder != null ? $"{subfolder}/{fileName}" : fileName;
                
                var blobClient = containerClient.GetBlobClient(blobName);

                // Upload file
                using var stream = file.OpenReadStream();
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType,
                    CacheControl = "public, max-age=31536000"
                };

                await blobClient.UploadAsync(stream, blobHttpHeaders);

                _Logger.LogInformation("File uploaded successfully: Container={Container}, BlobName={BlobName}, Size={Size}KB", 
                    containerName, blobName, file.Length / 1024);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Error uploading file: Container={Container}, FileName={FileName}", 
                    containerName, file?.FileName);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string blobUrl, string containerName)
        {
            try
            {
                if (string.IsNullOrEmpty(blobUrl))
                {
                    return false;
                }

                var containerClient = _BlobServiceClient.GetBlobContainerClient(containerName);
                var uri = new Uri(blobUrl);
                
                // Extract the full blob path (including subfolders) by removing the container name from the URL
                var containerSegment = $"/{containerName}/";
                var containerIndex = uri.AbsolutePath.IndexOf(containerSegment);
                if (containerIndex == -1)
                {
                    _Logger.LogWarning("Invalid blob URL format - container name not found: {BlobUrl}", blobUrl);
                    return false;
                }
                
                var blobName = uri.AbsolutePath.Substring(containerIndex + containerSegment.Length);
                
                _Logger.LogDebug("Blob path extraction: OriginalUrl={OriginalUrl}, ContainerName={ContainerName}, ExtractedBlobName={BlobName}", 
                    blobUrl, containerName, blobName);
                
                var blobClient = containerClient.GetBlobClient(blobName);
                
                _Logger.LogInformation("Attempting to delete blob: Container={Container}, BlobName={BlobName}, FullUrl={BlobUrl}", 
                    containerName, blobName, blobUrl);
                
                var response = await blobClient.DeleteIfExistsAsync();

                _Logger.LogInformation("File deleted: Container={Container}, BlobUrl={BlobUrl}, BlobName={BlobName}, Deleted={Deleted}", 
                    containerName, blobUrl, blobName, response.Value);

                return response.Value;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Error deleting file: Container={Container}, BlobUrl={BlobUrl}", containerName, blobUrl);
                return false;
            }
        }

        public async Task<bool> UpdateFileAsync(IFormFile newFile, string existingBlobUrl, string containerName, string? subfolder = null)
        {
            try
            {
                // Delete existing file
                if (!string.IsNullOrEmpty(existingBlobUrl))
                {
                    await DeleteFileAsync(existingBlobUrl, containerName);
                }

                // Upload new file
                var newBlobUrl = await UploadFileAsync(newFile, containerName, subfolder);

                _Logger.LogInformation("File updated successfully: Container={Container}, OldUrl={OldUrl}, NewUrl={NewUrl}", 
                    containerName, existingBlobUrl, newBlobUrl);

                return true;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Error updating file: Container={Container}", containerName);
                return false;
            }
        }

        public async Task<BlobDownloadInfo?> GetFileAsync(string blobUrl, string containerName)
        {
            try
            {
                if (string.IsNullOrEmpty(blobUrl))
                {
                    return null;
                }

                var containerClient = _BlobServiceClient.GetBlobContainerClient(containerName);
                var uri = new Uri(blobUrl);
                
                // Extract the full blob path (including subfolders) by removing the container name from the URL
                var containerSegment = $"/{containerName}/";
                var containerIndex = uri.AbsolutePath.IndexOf(containerSegment);
                if (containerIndex == -1)
                {
                    _Logger.LogWarning("Invalid blob URL format - container name not found: {BlobUrl}", blobUrl);
                    return null;
                }
                
                var blobName = uri.AbsolutePath.Substring(containerIndex + containerSegment.Length);
                
                _Logger.LogDebug("Blob path extraction: OriginalUrl={OriginalUrl}, ContainerName={ContainerName}, ExtractedBlobName={BlobName}", 
                    blobUrl, containerName, blobName);
                
                var blobClient = containerClient.GetBlobClient(blobName);
                
                if (!await blobClient.ExistsAsync())
                {
                    _Logger.LogWarning("File blob not found: Container={Container}, BlobUrl={BlobUrl}", containerName, blobUrl);
                    return null;
                }

                var downloadResult = await blobClient.DownloadAsync();
                
                _Logger.LogDebug("File retrieved successfully: Container={Container}, BlobUrl={BlobUrl}", containerName, blobUrl);
                return downloadResult.Value;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Error retrieving file: Container={Container}, BlobUrl={BlobUrl}", containerName, blobUrl);
                return null;
            }
        }

        public bool IsValidFile(IFormFile file, string[] allowedExtensions, int maxSizeInMB)
        {
            if (file == null)
                return false;

            // Check file size
            if (file.Length > maxSizeInMB * 1024 * 1024)
            {
                _Logger.LogWarning("File size exceeds limit: {FileSize}MB, MaxAllowed: {MaxAllowed}MB", 
                    file.Length / (1024 * 1024), maxSizeInMB);
                return false;
            }

            // Check file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension.ToLowerInvariant()))
            {
                _Logger.LogWarning("File extension not allowed: {Extension}, Allowed: {Allowed}", 
                    fileExtension, string.Join(", ", allowedExtensions));
                return false;
            }

            return true;
        }

        // Public method for image validation (includes MIME type checking)
        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null)
                return false;

            // Check file size
            if (file.Length > _Options.MaxFileSizeInMB * 1024 * 1024)
            {
                _Logger.LogWarning("File size exceeds limit: {FileSize}MB, MaxAllowed: {MaxAllowed}MB", 
                    file.Length / (1024 * 1024), _Options.MaxFileSizeInMB);
                return false;
            }

            // Check file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = _Options.AllowedImageExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.Trim().ToLowerInvariant());

            if (!allowedExtensions.Contains(fileExtension))
            {
                _Logger.LogWarning("File extension not allowed: {Extension}, Allowed: {Allowed}", 
                    fileExtension, string.Join(", ", allowedExtensions));
                return false;
            }

            // Check MIME type
            var allowedMimeTypes = _Options.AllowedMimeTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(mime => mime.Trim().ToLowerInvariant());

            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                _Logger.LogWarning("MIME type not allowed: {MimeType}, Allowed: {Allowed}", 
                    file.ContentType, string.Join(", ", allowedExtensions));
                return false;
            }

            return true;
        }
    }
}
