using System;

namespace VideoProcessingAPI.Entities
{
    public class VideoInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OriginalFileName { get; set; }
        public string ProcessedFileName { get; set; }
        public double Duration { get; set; }
        public DateTime UploadTime { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Uploaded"; // Uploaded, Processing, Completed, Failed
        public string PlaybackUrl { get; set; }
        public int HlsSegmentsCount { get; set; }
        public long FileSize { get; set; }
        public string Resolution { get; set; }
        public string Codec { get; set; }

        // 构造函数
        public VideoInfo() { }

        public VideoInfo(string originalFileName, long fileSize)
        {
            OriginalFileName = originalFileName;
            FileSize = fileSize;
        }
    }
}