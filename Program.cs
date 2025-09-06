using VideoProcessingAPI.Configuration;
using Microsoft.OpenApi.Models; // ��� Swagger �����ռ�
using System.Reflection;       // ��� XML ע����Ҫ�������ռ�

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

// ============= ��� Swagger ���� =============
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

    // ���� XML ע��
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
// ============= Swagger ���ý��� =============

var app = builder.Build();

// ============= ���� Swagger �м�� =============
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VideoProcessingAPI v1");
        // ��ѡ���� Swagger UI ����Ϊ��ҳ��
        // c.RoutePrefix = string.Empty;
    });
}
// ============= Swagger �м������ =============

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