using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VideoProcessingAPI.Utilities
{
    public static class FfmpegHelper
    {
        public static async Task<string> ExecuteFfmpegCommand(string arguments, string ffmpegPath)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();
                await Task.WhenAll(outputTask, errorTask);

                var output = await outputTask;
                var error = await errorTask;

                if (process.ExitCode != 0)
                {
                    throw new Exception($"FFmpeg处理失败，退出代码: {process.ExitCode}\n错误信息: {error}");
                }

                return output + error;
            }
        }

        public static async Task<double> GetVideoDuration(string filePath, string ffmpegPath)
        {
            var output = await ExecuteFfmpegCommand($"-i \"{filePath}\"", ffmpegPath);

            // 解析时长信息
            var durationMatch = Regex.Match(output, @"Duration: (\d+):(\d+):(\d+\.\d+)");
            if (durationMatch.Success)
            {
                var hours = int.Parse(durationMatch.Groups[1].Value);
                var minutes = int.Parse(durationMatch.Groups[2].Value);
                var seconds = double.Parse(durationMatch.Groups[3].Value);

                return hours * 3600 + minutes * 60 + seconds;
            }

            return 0;
        }

        public static async Task<string> GetVideoResolution(string filePath, string ffmpegPath)
        {
            var output = await ExecuteFfmpegCommand($"-i \"{filePath}\"", ffmpegPath);

            // 解析分辨率信息
            var resolutionMatch = Regex.Match(output, @"(\d{3,4}x\d{3,4})");
            if (resolutionMatch.Success)
            {
                return resolutionMatch.Groups[1].Value;
            }

            return "Unknown";
        }

        public static async Task<string> GetVideoCodec(string filePath, string ffmpegPath)
        {
            var output = await ExecuteFfmpegCommand($"-i \"{filePath}\"", ffmpegPath);

            // 解析编码格式信息
            var codecMatch = Regex.Match(output, @"Video: (\w+)");
            if (codecMatch.Success)
            {
                return codecMatch.Groups[1].Value;
            }

            return "Unknown";
        }
    }
}