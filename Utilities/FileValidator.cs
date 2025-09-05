using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace VideoProcessingAPI.Utilities
{
    public static class FileValidator
    {
        public static (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file, string[] allowedExtensions, long maxFileSize)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "请上传有效的视频文件");
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return (false, $"不支持的文件类型，请上传{string.Join("、", allowedExtensions)}格式");
            }

            if (file.Length > maxFileSize)
            {
                var maxSizeMB = maxFileSize / 1024 / 1024;
                return (false, $"文件太大，最大支持{maxSizeMB}MB");
            }

            return (true, null);
        }

        public static bool IsVideoFile(string extension)
        {
            var videoExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".flv", ".wmv", ".m4v", ".3gp" };
            return videoExtensions.Contains(extension.ToLower());
        }

        public static string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileName = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{fileName}_{timestamp}{extension}";
        }

        public static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars));
        }
    }
}