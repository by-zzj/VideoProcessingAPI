namespace VideoProcessingAPI.Models.Config
{
    public class FfmpegConfig
    {
        public string Path { get; set; }
        public int HlsTime { get; set; }
        public int HlsListSize { get; set; }
    }
}