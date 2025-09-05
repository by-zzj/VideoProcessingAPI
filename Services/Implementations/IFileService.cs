using Microsoft.AspNetCore.Http;

namespace VideoProcessingAPI.Services.Interfaces
{
    public interface IFileService
    {
        (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file);
        Task<string> CreateTempDirectoryAsync();
        Task<string> SaveUploadedFileAsync(IFormFile file, string tempDir);
        Task CleanupTempDirectoryAsync(string tempDir);
        string GenerateUniqueFileName(string originalFileName);
    }
}