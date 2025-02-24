using StoryTimeComicBookApi.Services;
using StoryTimeComicBookApi.Services.Interfaces;
using StoryTimeComicBookApi.Services.Clients;
using StoryTimeComicBookApi.Services.AI;
using Microsoft.EntityFrameworkCore;
using StoryTimeComicBookApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add LLM client and story generator
builder.Services.AddLlmClient(builder.Configuration);
builder.Services.AddScoped<IAiStoryGenerator, AiStoryGenerator>();
builder.Services.AddHttpClient();

// Named HttpClient for Replicate API with a custom timeout
builder.Services.AddHttpClient("ReplicateApi", client => {
    client.Timeout = TimeSpan.FromMinutes(5); // Longer timeout for image generation
});

builder.Services.AddHttpClient("OpenAIApi", client => {
    client.Timeout = TimeSpan.FromMinutes(2);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("AnthropicApi", client => {
    client.Timeout = TimeSpan.FromMinutes(2);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register the image generation service
builder.Services.AddScoped<IImageGenerationService, ReplicateImageGenerationService>();

// Add service dependencies
builder.Services.AddScoped<IComicBookService, ComicBookService>();
builder.Services.AddScoped<IVoiceMimickingService, VoiceMimickingService>();

builder.Services.AddDbContext<ComicBookDataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ComicBookGeneratorDbConnection"))); // Use your connection string name

// If using separate DbContexts, register the second one as well, e.g.,
builder.Services.AddDbContext<VoiceMimicDataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ComicBookGeneratorDbConnection")));

// Add these lines after your existing service registrations
builder.Services.AddScoped<IAudioStorageService, AudioStorageService>();
builder.Services.AddScoped<IVoiceModelTrainer, VoiceModelTrainer>();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Content-Disposition"); // Useful for file downloads
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Add cache control headers
        ctx.Context.Response.Headers.Append(
            "Cache-Control", "public, max-age=600"); // Cache for 10 minutes

        // Add proper headers for audio files
        if (ctx.File.Name.EndsWith(".wav") || ctx.File.Name.EndsWith(".mp3"))
        {
            ctx.Context.Response.Headers.Append(
                "Accept-Ranges", "bytes");
        }
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS middleware
app.UseCors("AllowAllOrigins");

app.UseAuthorization();

app.MapControllers();

app.Run();
