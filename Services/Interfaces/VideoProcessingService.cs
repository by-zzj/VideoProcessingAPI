using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using VideoProcessingAPI.Models.Config;
using VideoProcessingAPI.Models.DTO;
using VideoProcessingAPI.Services.Interfaces;
using System.Diagnostics;
using System.IO;
using VideoProcessingAPI.Utilities;

namespace VideoProcessingAPI.Services.Implementations
{
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly FfmpegConfig _ffmpegConfig;
        private readonly MinioConfig _minioConfig;
        private readonly ILogger<VideoProcessingService> _logger;

        public VideoProcessingService(
            IOptions<FfmpegConfig> ffmpegConfig,
            IOptions<MinioConfig> minioConfig,
            ILogger<VideoProcessingService> logger)
        {
            _ffmpegConfig = ffmpegConfig.Value;
            _minioConfig = minioConfig.Value;
            _logger = logger;
        }

        public async Task<ProcessingResult> ProcessVideoAsync(
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

            // 获取视频时长
            var duration = await GetVideoDuration(inputPath);

            // 执行FFmpeg切片
            var ffmpegArgs = $"-i \"{inputPath}\" " +
                            "-codec: copy " +
                            "-start_number 0 " +
                            $"-hls_time {_ffmpegConfig.HlsTime} " +
                            $"-hls_list_size {_ffmpegConfig.HlsListSize} " +
                            "-f hls " +
                            $"\"{outputPlaylist}\"";

            _logger.LogInformation($"执行FFmpeg: {ffmpegArgs}");

            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _ffmpegConfig.Path,
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

            // 检查输出文件是否存在
            if (!System.IO.File.Exists(outputPlaylist))
            {
                throw new Exception($"FFmpeg处理失败，未生成播放列表文件: {outputPlaylist}");
            }

            // 修改m3u8文件，将相对路径替换为MinIO公开URL
            var m3u8Content = await System.IO.File.ReadAllTextAsync(outputPlaylist);
            var videoName = Path.GetFileNameWithoutExtension(originalFileName);

            // 构建MinIO公开访问URL
            var minioPublicUrl = $"http://{_minioConfig.Endpoint}/{_minioConfig.HlsBucket}/hls/{videoName}";

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

            // 构建播放URL
            var playbackUrl = $"{baseUrl}/api/play/{videoName}/index.m3u8";

            return new ProcessingResult
            {
                HlsFiles = files,
                PlaybackUrl = playbackUrl,
                Duration = duration
            };
        }

        public async Task<double> GetVideoDuration(string filePath)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _ffmpegConfig.Path,
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
            var durationMatch = Regex.Match(errorOutput, @"Duration: (\d+):(\d+):(\d+\.\d+)");
            if (durationMatch.Success)
            {
                var hours = int.Parse(durationMatch.Groups[1].Value);
                var minutes = int.Parse(durationMatch.Groups[2].Value);
                var seconds = double.Parse(durationMatch.Groups[3].Value);

                return hours * 3600 + minutes * 60 + seconds;
            }

            return 0;
        }

        public async Task<string> GetVideoResolution(string filePath)
        {
            return await FfmpegHelper.GetVideoResolution(filePath, _ffmpegConfig.Path);
        }

        public async Task<string> GetVideoCodec(string filePath)
        {
            return await FfmpegHelper.GetVideoCodec(filePath, _ffmpegConfig.Path);
        }
    }
}
