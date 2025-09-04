using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs.Models;

namespace GainIt.API.Services.FileUpload.Interfaces
{
    public interface IFileUploadService
    {
        // Generic scalable methods for any container and file type
        Task<string> UploadFileAsync(IFormFile file, string containerName, string? subfolder = null);
        Task<bool> DeleteFileAsync(string blobUrl, string containerName);
        Task<bool> UpdateFileAsync(IFormFile newFile, string existingBlobUrl, string containerName, string? subfolder = null);
        Task<BlobDownloadInfo?> GetFileAsync(string blobUrl, string containerName);
        bool IsValidFile(IFormFile file, string[] allowedExtensions, int maxSizeInMB);
        
        // Image-specific validation (includes MIME type checking)
        bool IsValidImageFile(IFormFile file);
    }
}
