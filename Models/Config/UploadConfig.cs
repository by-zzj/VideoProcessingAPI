namespace VideoProcessingAPI.Models.Config
{
    public class UploadConfig
    {
        public long MaxFileSize { get; set; }
        public string[] AllowedExtensions { get; set; }
        public string TempPath { get; set; }
        public int MaxRetryCount { get; set; }
        public int RetryDelay { get; set; }
    }
}