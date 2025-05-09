
using DeliCheck;
using DeliCheck.Api.Services.Implementaions;
using DeliCheck.Services;
using DeliCheck.Utils;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Reflection;

using (var db = new DatabaseContext())
    db.Database.EnsureCreated();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<StartService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IAuthProvider, AuthProvider>();
builder.Services.AddSingleton<IImagePreprocessingService, ImagePreprocessingService>();
builder.Services.AddSingleton<IAvatarService, AvatarService>();
builder.Services.AddSingleton<IVkApi, VkApi>();
builder.Services.AddSingleton<IFnsParser, FnsParser>();
builder.Services.AddSingleton<IQrCodeReader, QrCodeReader>();

if (builder.Configuration["OCREngine"] == "ocr.space")
{
    builder.Services.AddSingleton<IOcrService, OcrSpaceService>();
    builder.Services.AddSingleton<IParsingService, OcrSpaceParsingService>();
}
else
{
    builder.Services.AddSingleton<IOcrService, TesseractOcrService>();
    builder.Services.AddSingleton<IParsingService, TesseractParsingService>();
}

builder.Services.AddControllers(options => options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer())));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ДелиЧек API",
        Version = "1.0.0",
        Description = ""
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
builder.WebHost.UseKestrel(options =>
{
    options.Listen(IPAddress.Parse(builder.Configuration["BindIP"]), 443, listenOptions =>
    {
        listenOptions.UseHttps("certificate.pfx");
    });
});

var app = builder.Build();

//if (app.Environment.IsDevelopment())
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(builder => builder
         .SetIsOriginAllowed(callerHost
                => true//callerHost.Contains("http://localhost:5208") || 
                //callerHost.Contains("http://192.168.1.128:8888") || 
                  //  callerHost.Contains("https://deli-check.ru")
                )
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials()
     );

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
