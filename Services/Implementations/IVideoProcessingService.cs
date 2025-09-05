using VideoProcessingAPI.Models.DTO;

namespace VideoProcessingAPI.Services.Interfaces
{
    public interface IVideoProcessingService
    {
        Task<ProcessingResult> ProcessVideoAsync(string inputPath, string outputDir, string originalFileName, string baseUrl);
        Task<double> GetVideoDuration(string filePath);
        Task<string> GetVideoResolution(string filePath);
        Task<string> GetVideoCodec(string filePath);
    }
}