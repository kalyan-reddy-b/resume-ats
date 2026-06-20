using ResumeAnalyzer.Api.Config;
using ResumeAnalyzer.Api.Repository;
using ResumeAnalyzer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure GroqSettings from appsettings.json and environment variables
builder.Services.Configure<GroqSettings>(builder.Configuration.GetSection("GroqSettings"));
builder.Services.PostConfigure<GroqSettings>(options =>
{
    var envApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
    if (!string.IsNullOrEmpty(envApiKey))
    {
        options.ApiKey = envApiKey;
    }
});

// Configure HTTP services and DI
builder.Services.AddHttpClient<IResumeAnalyzerService, ResumeAnalyzerService>();
builder.Services.AddSingleton<IAnalysisRepository, InMemoryAnalysisRepository>();

// Configure Swashbuckle Swagger support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
