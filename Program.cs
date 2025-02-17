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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
