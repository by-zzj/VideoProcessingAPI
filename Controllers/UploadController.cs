using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VideoProcessingAPI.Models.Config;
using VideoProcessingAPI.Models.DTO;
using VideoProcessingAPI.Services.Interfaces;

namespace VideoProcessingAPI.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IMinioService _minioService;
        private readonly IVideoProcessingService _videoProcessingService;
        private readonly IFileService _fileService;
        private readonly MinioConfig _minioConfig;

        public UploadController(
            ILogger<UploadController> logger,
            IMinioService minioService,
            IVideoProcessingService videoProcessingService,
            IFileService fileService,
            IOptions<MinioConfig> minioConfig)
        {
            _logger = logger;
            _minioService = minioService;
            _videoProcessingService = videoProcessingService;
            _fileService = fileService;
            _minioConfig = minioConfig.Value;
        }

        /// <summary>
        /// 视频文件上传
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <returns></returns>
        [HttpPost("video")]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            try
            {
                // 验证文件
                var validationResult = _fileService.ValidateFile(file);
                if (!validationResult.IsValid)
                    return BadRequest(validationResult.ErrorMessage);

                // 创建临时目录
                var tempDir = await _fileService.CreateTempDirectoryAsync();

                try
                {
                    // 保存上传文件
                    var tempFilePath = await _fileService.SaveUploadedFileAsync(file, tempDir);

                    // 获取当前日期作为文件夹名称 (格式: yyyy-MM-dd)
                    var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");

                    // 上传原始视频到MinIO
                    await _minioService.UploadFileAsync(
                        _minioConfig.RawBucket,
                        $"{dateFolder}/{file.FileName}",
                        tempFilePath);

                    // 处理视频并生成HLS切片
                    var processingResult = await _videoProcessingService.ProcessVideoAsync(
                        tempFilePath,
                        tempDir,
                        file.FileName,
                        $"{Request.Scheme}://{Request.Host}");

                    // 上传切片文件到MinIO
                    foreach (var hlsFile in processingResult.HlsFiles)
                    {
                        var objectName = $"hls/{dateFolder}/{Path.GetFileNameWithoutExtension(file.FileName)}/{Path.GetFileName(hlsFile)}";
                        await _minioService.UploadFileAsync(_minioConfig.HlsBucket, objectName, hlsFile);
                    }

                    return Ok(new UploadResponse
                    {
                        Message = "视频上传和处理成功",
                        PlaybackUrl = processingResult.PlaybackUrl,
                        OriginalFile = file.FileName,
                        HlsFilesCount = processingResult.HlsFiles.Length,
                        Duration = processingResult.Duration,
                        Resolution = processingResult.Resolution,
                        Codec = processingResult.Codec
                    });
                }
                finally
                {
                    // 清理临时目录
                    await _fileService.CleanupTempDirectoryAsync(tempDir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "视频上传和处理失败");
                return StatusCode(500, new { Message = "视频处理失败", Error = ex.Message });
            }
        }
    }
}