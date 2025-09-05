namespace VideoProcessingAPI.Models.DTO
{
    public class ProcessingResult
    {
        public string[] HlsFiles { get; set; }
        public string PlaybackUrl { get; set; }
        public double Duration { get; set; }
        public string Resolution { get; set; }
        public string Codec { get; set; }
    }
}