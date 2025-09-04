using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VideoProcessingAPI.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly IMinioClient _minioClient;
        private readonly ILogger<UploadController> _logger;
        private readonly string _tempPath;

        // MinIO 配置
        private const string Endpoint = "localhost:9000";
        private const string AccessKey = "admin";
        private const string SecretKey = "Zzj154810";
        private const string RawBucket = "video-raw";
        private const string HlsBucket = "video-hls";

        public UploadController(ILogger<UploadController> logger)
        {
            _logger = logger;

            // 使用应用程序专用目录而不是系统临时目录
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _tempPath = Path.Combine(appDataPath, "VideoProcessingAPI", "temp-uploads");

            Directory.CreateDirectory(_tempPath); // 确保临时目录存在

            // 初始化 MinIO 客户端
            _minioClient = new MinioClient()
                .WithEndpoint(Endpoint)
                .WithCredentials(AccessKey, SecretKey)
                .WithSSL(false)  // 根据您的配置调整
                .WithTimeout(30000)  // 30秒超时
                .Build();
        }

        [HttpPost("video")]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("请上传有效的视频文件");
            }

            // 验证文件类型
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" }.Contains(extension))
            {
                return BadRequest("不支持的文件类型，请上传MP4、MOV、AVI、MKV或WEBM格式");
            }

            // 创建临时目录
            var tempDir = Path.Combine(_tempPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // 保存上传文件到临时目录
                var tempFilePath = Path.Combine(tempDir, file.FileName);
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 上传原始视频到MinIO
                await UploadToMinio(RawBucket, file.FileName, tempFilePath);

                // 获取视频时长
                var duration = await GetVideoDuration(tempFilePath);
                _videoDuration[file.FileName] = duration;
                _currentVideoId = file.FileName;

                // 构建baseUrl
                var baseUrl = $"{Request.Scheme}://{Request.Host}/api/play/{Path.GetFileNameWithoutExtension(file.FileName)}";

                // 处理视频并生成HLS切片
                var (hlsFiles, playlistPath) = await ProcessVideo(tempFilePath, tempDir, file.FileName, baseUrl);

                // 上传切片文件到MinIO
                foreach (var hlsFile in hlsFiles)
                {
                    var objectName = $"hls/{Path.GetFileNameWithoutExtension(file.FileName)}/{Path.GetFileName(hlsFile)}";
                    await UploadToMinio(HlsBucket, objectName, hlsFile);
                }

                // 返回播放URL
                var playbackUrl = $"{baseUrl}/index.m3u8";
                return Ok(new
                {
                    Message = "视频上传和处理成功",
                    PlaybackUrl = playbackUrl,
                    OriginalFile = file.FileName,
                    HlsFiles = hlsFiles.Length,
                    PlaylistPath = playlistPath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "视频上传和处理失败");
                return StatusCode(500, new { Message = "视频处理失败", Error = ex.Message });
            }
            finally
            {
                // 清理临时目录 - 增加重试机制
                bool cleanupSuccess = false;
                int retryCount = 0;

                while (!cleanupSuccess && retryCount < 5)
                {
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                        cleanupSuccess = true;
                    }
                    catch (IOException ex)
                    {
                        retryCount++;
                        _logger.LogWarning(ex, $"清理临时目录失败，尝试第 {retryCount} 次重试");
                        await Task.Delay(1000 * retryCount); // 指数退避策略
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "清理临时目录失败");
                        break;
                    }
                }
            }
        }


        private async Task UploadToMinio(string bucket, string objectName, string filePath)
        {
            try
            {
                // 确保存储桶存在
                bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
                if (!found)
                {
                    await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
                    _logger.LogInformation($"创建存储桶: {bucket}");
                }

                // 上传文件
                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectName)
                    .WithFileName(filePath));

                _logger.LogInformation($"已上传 {objectName} 到 {bucket}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"上传文件到MinIO失败: {objectName}");
                throw;
            }
        }

        private async Task<(string[] Files, string PlaylistPath)> ProcessVideo(
            string inputPath,
            string outputDir,
            string originalFileName,
            string baseUrl)
        {
            // 创建HLS输出目录
            var hlsOutputDir = Path.Combine(outputDir, "hls");
            Directory.CreateDirectory(hlsOutputDir);

            // 生成输出文件名
            var outputPlaylist = Path.Combine(hlsOutputDir, "index.m3u8");

            // 执行FFmpeg切片
            var ffmpegArgs = $"-i \"{inputPath}\" " +
                            "-codec: copy " +
                            "-start_number 0 " +
                            "-hls_time 10 " +
                            "-hls_list_size 0 " +
                            "-f hls " +
                            $"\"{outputPlaylist}\"";

            _logger.LogInformation($"执行FFmpeg: {ffmpegArgs}");

            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = @"D:\DevelopmentTool\ffmpeg-master-latest-win64-gpl-shared\bin\ffmpeg.exe",
                    Arguments = ffmpegArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                // 启动进程
                process.Start();

                // 异步读取输出流
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // 等待进程退出
                await process.WaitForExitAsync();

                // 确保所有输出已读取
                await Task.WhenAll(outputTask, errorTask);

                var output = await outputTask;
                var error = await errorTask;

                if (process.ExitCode != 0)
                {
                    throw new Exception($"FFmpeg处理失败，退出代码: {process.ExitCode}\n错误信息: {error}");
                }
            }

            // 等待一段时间，确保文件句柄被操作系统释放
            await Task.Delay(500);

            // 修改m3u8文件，将相对路径替换为MinIO公开URL
            var m3u8Content = await System.IO.File.ReadAllTextAsync(outputPlaylist);
            var videoName = Path.GetFileNameWithoutExtension(originalFileName);

            // 构建MinIO公开访问URL
            var minioPublicUrl = $"http://{Endpoint}/video-hls/hls/{videoName}";

            // 使用正则表达式替换所有.ts文件引用
            m3u8Content = Regex.Replace(
                m3u8Content,
                @"^([^#].*\.ts)$",
                match => $"{minioPublicUrl}/{match.Groups[1].Value}",
                RegexOptions.Multiline
            );

            // 写回修改后的m3u8文件
            await System.IO.File.WriteAllTextAsync(outputPlaylist, m3u8Content);

            // 返回所有切片文件
            var files = Directory.GetFiles(hlsOutputDir, "*", SearchOption.AllDirectories);
            return (files, outputPlaylist);
        }


        private Dictionary<string, double> _videoDuration = new Dictionary<string, double>();
        private string _currentVideoId = string.Empty;

        private async Task<double> GetVideoDuration(string filePath)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = @"D:\DevelopmentTool\ffmpeg-master-latest-win64-gpl-shared\bin\ffmpeg.exe",
                    Arguments = $"-i \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            var errorOutput = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // 解析时长信息
            var durationMatch = System.Text.RegularExpressions.Regex.Match(errorOutput, @"Duration: (\d+):(\d+):(\d+\.\d+)");
            if (durationMatch.Success)
            {
                var hours = int.Parse(durationMatch.Groups[1].Value);
                var minutes = int.Parse(durationMatch.Groups[2].Value);
                var seconds = double.Parse(durationMatch.Groups[3].Value);

                return hours * 3600 + minutes * 60 + seconds;
            }

            return 0;
        }
    }
}