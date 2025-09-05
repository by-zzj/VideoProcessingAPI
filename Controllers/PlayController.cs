using Microsoft.AspNetCore.Mvc;
using VideoProcessingAPI.Services.Interfaces;

namespace VideoProcessingAPI.Controllers
{
    [ApiController]
    [Route("api/play")]
    public class PlayController : ControllerBase
    {
        private readonly IMinioService _minioService;
        private readonly ILogger<PlayController> _logger;

        public PlayController(IMinioService minioService, ILogger<PlayController> logger)
        {
            _minioService = minioService;
            _logger = logger;
        }

        [HttpGet("{videoName}/{fileName}")]
        public async Task<IActionResult> GetVideoSegment(string videoName, string fileName)
        {
            try
            {
                var objectName = $"hls/{videoName}/{fileName}";
                var presignedUrl = _minioService.GetPublicUrl("video-hls", objectName);

                // 重定向到MinIO的公开URL
                return Redirect(presignedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取视频片段失败: {videoName}/{fileName}");
                return NotFound();
            }
        }

        [HttpGet("{videoName}/info")]
        public async Task<IActionResult> GetVideoInfo(string videoName)
        {
            try
            {
                // 这里可以添加从数据库或其他存储中获取视频信息的逻辑
                return Ok(new
                {
                    VideoName = videoName,
                    Status = "Available",
                    Message = "视频信息获取成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取视频信息失败: {videoName}");
                return NotFound();
            }
        }
    }
}