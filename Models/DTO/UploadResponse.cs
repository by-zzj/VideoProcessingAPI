namespace VideoProcessingAPI.Models.DTO
{
    public class UploadResponse
    {
        public string Message { get; set; }
        public string PlaybackUrl { get; set; }
        public string OriginalFile { get; set; }
        public int HlsFilesCount { get; set; }
        public double Duration { get; set; }
        public string Resolution { get; set; }
        public string Codec { get; set; }
    }
}