using VideoProcessingAPI.Configuration;
using Microsoft.OpenApi.Models; // 添加 Swagger 命名空间
using System.Reflection;       // 添加 XML 注释需要的命名空间

var builder = WebApplication.CreateBuilder(args);

// 添加配置和服务
builder.Services.AddConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.ConfigureFileUpload(builder.Configuration);
builder.Services.ValidateConfiguration(builder.Configuration);

// 配置 Kestrel 选项
builder.WebHost.ConfigureKestrelOptions(builder.Configuration);

// 添加控制器
builder.Services.AddControllers();

// ============= 添加 Swagger 服务 =============
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Video Processing API",
        Version = "v1",
        Description = "API for video processing operations",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "contact@example.com"
        }
    });

    // 启用 XML 注释
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
// ============= Swagger 配置结束 =============

var app = builder.Build();

// ============= 配置 Swagger 中间件 =============
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VideoProcessingAPI v1");
        // 可选：将 Swagger UI 设置为根页面
        // c.RoutePrefix = string.Empty;
    });
}
// ============= Swagger 中间件结束 =============

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 添加控制器端点
app.MapControllers();

app.Run();