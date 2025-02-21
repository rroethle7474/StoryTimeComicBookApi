using Microsoft.AspNetCore.Http;
using StoryTimeComicBookApi.Services.Interfaces;

namespace StoryTimeComicBookApi.Services;

public class AudioStorageService : IAudioStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AudioStorageService> _logger;
    private readonly string _audioStoragePath;
    private readonly string _webPath;

    public AudioStorageService(
        IConfiguration configuration,
        ILogger<AudioStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Get storage path from configuration, or use default
        _audioStoragePath = _configuration["AudioStorage:Path"] ?? "wwwroot/uploads/audio";
        _webPath = "/uploads/audio"; // Web-accessible path
        // Ensure storage directory exists
        // Ensure storage directory exists - handle both absolute and relative paths
        var fullPath = Path.IsPathRooted(_audioStoragePath)
            ? _audioStoragePath
            : Path.Combine(Directory.GetCurrentDirectory(), _audioStoragePath);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
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

            // Get full storage path for saving file
            var fullPath = Path.IsPathRooted(_audioStoragePath)
                ? Path.Combine(_audioStoragePath, fileName)
                : Path.Combine(Directory.GetCurrentDirectory(), _audioStoragePath, fileName);

            // Save file
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            // Return web-accessible path
            var webPath = $"{_webPath}/{fileName}";

            _logger.LogInformation("Audio file saved successfully. Web path: {WebPath}", webPath);
            return webPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving audio file");
            throw;
        }
    }
} 