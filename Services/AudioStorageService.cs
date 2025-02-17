using Microsoft.AspNetCore.Http;
using StoryTimeComicBookApi.Services.Interfaces;

namespace StoryTimeComicBookApi.Services;

public class AudioStorageService : IAudioStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AudioStorageService> _logger;
    private readonly string _audioStoragePath;

    public AudioStorageService(
        IConfiguration configuration,
        ILogger<AudioStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Get storage path from configuration, or use default
        _audioStoragePath = _configuration["AudioStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "AudioStorage");
        
        // Ensure storage directory exists
        if (!Directory.Exists(_audioStoragePath))
        {
            Directory.CreateDirectory(_audioStoragePath);
        }
    }

    public async Task<string> SaveAudioFileAsync(IFormFile audioFile)
    {
        try
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                throw new ArgumentException("No audio file provided");
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(audioFile.FileName)}";
            var filePath = Path.Combine(_audioStoragePath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            _logger.LogInformation("Audio file saved successfully: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving audio file");
            throw;
        }
    }
} 