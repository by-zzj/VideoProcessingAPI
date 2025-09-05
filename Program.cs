using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoProcessingAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ������úͷ���
builder.Services.AddConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.ConfigureFileUpload(builder.Configuration);
builder.Services.ValidateConfiguration(builder.Configuration);

// ���� Kestrel ѡ��
builder.WebHost.ConfigureKestrelOptions(builder.Configuration);

// ��ӿ�����
builder.Services.AddControllers();

var app = builder.Build();

// ����HTTP����ܵ�
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

// ��ӿ������˵�
app.MapControllers();

app.Run();