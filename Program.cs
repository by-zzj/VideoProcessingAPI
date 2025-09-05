using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoProcessingAPI.Configuration;

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

var app = builder.Build();

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