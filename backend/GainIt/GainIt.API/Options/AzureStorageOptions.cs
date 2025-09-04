namespace GainIt.API.Options
{
    public class AzureStorageOptions
    {
        public const string SectionName = "AzureStorage";
        
        public string ConnectionString { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountKey { get; set; } = string.Empty;
        public string ProfilePicturesContainerName { get; set; } = "profile-pictures";
        public string ProjectsContainerName { get; set; } = "projects";
        public string AllowedImageExtensions { get; set; } = ".jpg,.jpeg,.png,.gif,.webp";
        public int MaxFileSizeInMB { get; set; } = 10;
        public string AllowedMimeTypes { get; set; } = "image/jpeg,image/png,image/gif,image/webp";
    }
}
