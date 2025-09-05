using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using VideoProcessingAPI.Services.Interfaces;
using VideoProcessingAPI.Services.Implementations;
using VideoProcessingAPI.Models.Config;
using Microsoft.AspNetCore.Http.Features;

namespace VideoProcessingAPI.Configuration
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // 注册所有服务
            services.AddSingleton<IMinioService, MinioService>();
            services.AddScoped<IVideoProcessingService, VideoProcessingService>();
            services.AddScoped<IFileService, FileService>();

            return services;
        }

        public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // 绑定配置
            services.Configure<MinioConfig>(configuration.GetSection("Minio"));
            services.Configure<FfmpegConfig>(configuration.GetSection("Ffmpeg"));
            services.Configure<UploadConfig>(configuration.GetSection("Upload"));

            return services;
        }

        public static IServiceCollection ConfigureFileUpload(this IServiceCollection services, IConfiguration configuration)
        {
            // 配置大文件上传
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = configuration.GetValue<long>("Upload:MaxFileSize");
            });

            return services;
        }

        public static IWebHostBuilder ConfigureKestrelOptions(this IWebHostBuilder hostBuilder, IConfiguration configuration)
        {
            hostBuilder.ConfigureKestrel(serverOptions =>
            {
                // 设置请求体最大大小
                serverOptions.Limits.MaxRequestBodySize = configuration.GetValue<long>("Upload:MaxFileSize"); ;

                // 添加其他 Kestrel 配置
                serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);       // 保持活动连接的超时时间
                serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);  // 请求头的超时时间
                serverOptions.Limits.MaxConcurrentConnections = 100;                   // 最大并发连接数
                serverOptions.Limits.MaxConcurrentUpgradedConnections = 100;           // 最大并发升级连接数（如 WebSocket）
            });

            return hostBuilder;
        }

        public static IServiceCollection ValidateConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // 验证必须的配置项是否存在
            var minioConfig = configuration.GetSection("Minio").Get<MinioConfig>();
            if (string.IsNullOrEmpty(minioConfig?.Endpoint))
                throw new Exception("Minio Endpoint configuration is missing");

            // 验证 FFmpeg 路径是否存在
            var ffmpegPath = configuration.GetValue<string>("Ffmpeg:Path");
            if (!File.Exists(ffmpegPath))
                throw new Exception($"FFmpeg executable not found at: {ffmpegPath}");

            // 验证临时目录是否可写
            var tempPath = configuration.GetValue<string>("Upload:TempPath");
            if (!Directory.Exists(tempPath))
            {
                try
                {
                    Directory.CreateDirectory(tempPath);
                }
                catch
                {
                    throw new Exception($"Failed to create temp directory: {tempPath}");
                }
            }

            return services;
        }
    }
}