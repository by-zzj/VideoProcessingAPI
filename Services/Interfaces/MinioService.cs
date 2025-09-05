using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;
using VideoProcessingAPI.Models.Config;
using VideoProcessingAPI.Services.Interfaces;
using MinioConfig = VideoProcessingAPI.Models.Config.MinioConfig;

namespace VideoProcessingAPI.Services.Implementations
{
    public class MinioService : IMinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly MinioConfig _config;
        private readonly ILogger<MinioService> _logger;

        public MinioService(IOptions<MinioConfig> config, ILogger<MinioService> logger)
        {
            _config = config.Value;
            _logger = logger;
            _minioClient = new MinioClient()
                .WithEndpoint(_config.Endpoint)
                .WithCredentials(_config.AccessKey, _config.SecretKey)
                .WithSSL(_config.UseSSL)
                .WithTimeout(_config.Timeout)
                .Build();
        }

        public async Task EnsureBucketExistsAsync(string bucketName)
        {
            try
            {
                bool found = await _minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(bucketName));

                if (!found)
                {
                    await _minioClient.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(bucketName));
                    _logger.LogInformation($"创建存储桶: {bucketName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"检查/创建存储桶失败: {bucketName}");
                throw;
            }
        }

        public async Task UploadFileAsync(string bucketName, string objectName, string filePath)
        {
            try
            {
                await EnsureBucketExistsAsync(bucketName);

                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithFileName(filePath));

                _logger.LogInformation($"已上传 {objectName} 到 {bucketName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"上传文件到MinIO失败: {objectName}");
                throw;
            }
        }

        public string GetPublicUrl(string bucketName, string objectName)
        {
            return $"http://{_config.Endpoint}/{bucketName}/{objectName}";
        }

        public async Task<bool> ObjectExistsAsync(string bucketName, string objectName)
        {
            try
            {
                await _minioClient.StatObjectAsync(new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}