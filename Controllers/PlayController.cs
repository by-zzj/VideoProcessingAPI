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

        // 更新路由模板：添加日期路径
        [HttpGet("{date}/{videoName}/{fileName}")]
        public async Task<IActionResult> GetVideoSegment(string date, string videoName, string fileName)
        {
            try
            {
                // 构建对象名称：hls/日期文件夹/视频名称/文件名
                var objectName = $"hls/{date}/{videoName}/{fileName}";

                // 获取公开URL
                var presignedUrl = _minioService.GetPublicUrl("video-hls", objectName);

                // 重定向到MinIO的公开URL
                return Redirect(presignedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取视频片段失败: {date}/{videoName}/{fileName}");
                return NotFound();
            }
        }

        // 更新视频信息路由模板：添加日期路径
        [HttpGet("{date}/{videoName}/info")]
        public async Task<IActionResult> GetVideoInfo(string date, string videoName)
        {
            try
            {
                // 这里可以添加从数据库或其他存储中获取视频信息的逻辑
                // 示例：获取视频的基本信息
                var videoInfo = new
                {
                    Date = date,
                    VideoName = videoName,
                    Status = "Available",
                    Message = "视频信息获取成功",
                    PlaybackUrl = $"{Request.Scheme}://{Request.Host}/api/play/{date}/{videoName}/index.m3u8"
                };

                return Ok(videoInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取视频信息失败: {date}/{videoName}");
                return NotFound();
            }
        }
    }
}