using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace VideoProcessingAPI.Controllers
{
    [ApiController]
    [Route("api/play")]
    public class PlayController : ControllerBase
    {
        private readonly IMinioClient _minioClient;
        private readonly ILogger<PlayController> _logger;

        // MinIO 配置
        private const string Endpoint = "localhost:9000";
        private const string AccessKey = "admin";
        private const string SecretKey = "Zzj154810";
        private const string HlsBucket = "video-hls";

        public PlayController(ILogger<PlayController> logger)
        {
            _logger = logger;

            // 初始化 MinIO 客户端
            _minioClient = new MinioClient()
                .WithEndpoint(Endpoint)
                .WithCredentials(AccessKey, SecretKey)
                .WithSSL(false)  // 根据您的配置调整
                .WithTimeout(30000)  // 30秒超时
                .Build();
        }

        [HttpGet("{videoName}/{fileName}")]
        public async Task<IActionResult> GetVideoSegment(string videoName, string fileName)
        {
            var objectName = $"hls/{videoName}/{fileName}";
            _logger.LogInformation($"请求视频切片: {objectName}");

            try
            {
                // 检查文件是否存在
                try
                {
                    await _minioClient.StatObjectAsync(new StatObjectArgs()
                        .WithBucket(HlsBucket)
                        .WithObject(objectName));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"文件不存在或访问失败: {objectName}, 错误: {ex.Message}");
                    return NotFound($"文件不存在: {objectName}");
                }

                // 使用内存流缓存文件内容
                var memoryStream = new MemoryStream();

                try
                {
                    // 创建超时控制
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    // 下载文件到内存流
                    await _minioClient.GetObjectAsync(new GetObjectArgs()
                        .WithBucket(HlsBucket)
                        .WithObject(objectName)
                        .WithCallbackStream(async stream =>
                        {
                            try
                            {
                                await stream.CopyToAsync(memoryStream, 81920, cts.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                _logger.LogWarning("下载操作已取消或超时");
                                throw;
                            }
                        }), cts.Token);

                    memoryStream.Position = 0;

                    // 设置内容类型
                    var contentType = GetContentType(fileName);

                    // 返回文件流
                    return File(memoryStream, contentType);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError("下载视频切片超时");
                    return StatusCode(408, "请求超时");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "下载视频切片过程中发生错误");
                    return StatusCode(500, "获取视频切片失败");
                }
            }
            catch (MinioException ex)
            {
                _logger.LogError(ex, "MinIO操作失败");
                return StatusCode(503, "存储服务暂时不可用");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取视频切片失败");
                return StatusCode(500, "获取视频切片失败");
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".m3u8" => "application/vnd.apple.mpegurl",
                ".ts" => "video/mp2t",
                _ => "application/octet-stream"
            };
        }
    }
}