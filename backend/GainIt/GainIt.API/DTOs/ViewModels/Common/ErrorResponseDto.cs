namespace GainIt.API.DTOs.ViewModels.Common
{
    /// <summary>
    /// Generic error response DTO
    /// </summary>
    public class ErrorResponseDto
    {
        /// <summary>
        /// Error message describing what went wrong
        /// </summary>
        public string Error { get; set; } = string.Empty;
    }
}
