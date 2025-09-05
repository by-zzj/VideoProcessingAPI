using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using VideoProcessingAPI.Models.Config;
using VideoProcessingAPI.Services.Interfaces;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VideoProcessingAPI.Utilities;

namespace VideoProcessingAPI.Services.Implementations
{
    public class FileService : IFileService
    {
        private readonly UploadConfig _uploadConfig;
        private readonly ILogger<FileService> _logger;

        public FileService(IOptions<UploadConfig> uploadConfig, ILogger<FileService> logger)
        {
            _uploadConfig = uploadConfig.Value;
            _logger = logger;
        }

        public (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "请上传有效的视频文件");
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!_uploadConfig.AllowedExtensions.Contains(extension))
            {
                return (false, $"不支持的文件类型，请上传{string.Join("、", _uploadConfig.AllowedExtensions)}格式");
            }

            if (file.Length > _uploadConfig.MaxFileSize)
            {
                return (false, $"文件太大，最大支持{_uploadConfig.MaxFileSize / 1024 / 1024}MB");
            }

            return (true, null);
        }

        public async Task<string> CreateTempDirectoryAsync()
        {
            var tempDir = Path.Combine(_uploadConfig.TempPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        public async Task<string> SaveUploadedFileAsync(IFormFile file, string tempDir)
        {
            var tempFilePath = Path.Combine(tempDir, file.FileName);
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return tempFilePath;
        }

        public async Task CleanupTempDirectoryAsync(string tempDir)
        {
            if (!Directory.Exists(tempDir))
                return;

            bool cleanupSuccess = false;
            int retryCount = 0;

            while (!cleanupSuccess && retryCount < _uploadConfig.MaxRetryCount)
            {
                try
                {
                    // 先删除目录中的所有文件
                    string[] files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"删除文件失败: {file}");
                        }
                    }

                    // 再删除目录
                    Directory.Delete(tempDir, true);
                    cleanupSuccess = true;
                }
                catch (IOException ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, $"清理临时目录失败，尝试第 {retryCount} 次重试");
                    await Task.Delay(_uploadConfig.RetryDelay * retryCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "清理临时目录失败");
                    break;
                }
            }
        }

        public string GenerateUniqueFileName(string originalFileName)
        {
            return FileValidator.GenerateUniqueFileName(originalFileName);
        }
    }
}
