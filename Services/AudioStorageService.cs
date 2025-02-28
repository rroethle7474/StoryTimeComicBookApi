using StoryTimeComicBookApi.Services.Interfaces;
using System.Diagnostics;

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

        // Ensure storage directory exists - handle both absolute and relative paths
        var fullPath = Path.IsPathRooted(_audioStoragePath)
            ? _audioStoragePath
            : Path.Combine(Directory.GetCurrentDirectory(), _audioStoragePath);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
    }

    public async Task<string> SaveAudioFileAsync(IFormFile audioFile, string targetSampleRate = null)
    {
        try
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                throw new ArgumentException("No audio file provided");
            }

            // Generate unique filename with GUID
            var audioId = Guid.NewGuid().ToString();
            var tempFileName = $"{audioId}_temp{Path.GetExtension(audioFile.FileName)}";
            var outputFileName = $"{audioId}_recording.wav";

            // Get full storage paths
            var tempPath = Path.IsPathRooted(_audioStoragePath)
                ? Path.Combine(_audioStoragePath, tempFileName)
                : Path.Combine(Directory.GetCurrentDirectory(), _audioStoragePath, tempFileName);

            var outputPath = Path.IsPathRooted(_audioStoragePath)
                ? Path.Combine(_audioStoragePath, outputFileName)
                : Path.Combine(Directory.GetCurrentDirectory(), _audioStoragePath, outputFileName);

            // Save the uploaded file temporarily
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            // If targetSampleRate is not provided, use default
            if (string.IsNullOrEmpty(targetSampleRate))
            {
                targetSampleRate = "24000"; // Default for StyleTTS2
            }

            _logger.LogInformation("Using sample rate: {SampleRate} Hz", targetSampleRate);

            // Convert WebM to WAV with FFmpeg with 24kHz sample rate
            await ConvertToWavAsync(tempPath, outputPath, targetSampleRate);

            // Delete temporary file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            // Return web-accessible path
            var webPath = $"{_webPath}/{outputFileName}";

            _logger.LogInformation("Audio file saved successfully. Web path: {WebPath}", webPath);
            return webPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving audio file");
            throw;
        }
    }

    private async Task ConvertToWavAsync(string inputPath, string outputPath, string targetSampleRate)
    {
        try
        {
            // Use FFmpeg to convert to WAV with specific parameters for StyleTTS2
            var ffmpegPath = _configuration["FFmpeg:Path"] ?? "ffmpeg"; // Get ffmpeg path from config or use command if in PATH

            var processStartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{inputPath}\" -ar {targetSampleRate} -ac 1 -c:a pcm_s16le \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg conversion failed: {Error}", error);
                throw new Exception($"FFmpeg conversion failed with exit code {process.ExitCode}: {error}");
            }

            _logger.LogInformation("Audio conversion successful: {InputPath} -> {OutputPath}", inputPath, outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting audio file");
            throw;
        }
    }
}