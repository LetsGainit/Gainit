namespace GainIt.API.DTOs.ViewModels.Users
{
    public class ProfilePictureResponseViewModel
    {
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
        public long FileSizeInBytes { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }
}
