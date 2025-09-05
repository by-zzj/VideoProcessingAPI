namespace VideoProcessingAPI.Models.Config
{
    public class MinioConfig
    {
        public string Endpoint { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public bool UseSSL { get; set; }
        public int Timeout { get; set; }
        public string RawBucket { get; set; }
        public string HlsBucket { get; set; }
    }
}