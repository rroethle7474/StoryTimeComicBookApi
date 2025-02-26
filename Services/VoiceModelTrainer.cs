using StoryTimeComicBookApi.Data;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Clients;
using StoryTimeComicBookApi.Services.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

public class VoiceModelTrainer : IVoiceModelTrainer
{
    private readonly VoiceMimicDataContext _context;
    private readonly ILogger<VoiceModelTrainer> _logger;
    private readonly IConfiguration _configuration;
    private readonly HuggingFaceClient _huggingFaceClient;
    private readonly string _modelStoragePath;
    private readonly string _huggingFaceUsername;

    public VoiceModelTrainer(
        VoiceMimicDataContext context,
        IConfiguration configuration,
        ILogger<VoiceModelTrainer> logger,
        HuggingFaceClient huggingFaceClient)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _huggingFaceClient = huggingFaceClient;

        _huggingFaceUsername = _configuration["AI:HuggingFace:Username"] ??
            throw new InvalidOperationException("HuggingFace username not configured");

        // Get model storage path from configuration, or use default
        _modelStoragePath = _configuration["VoiceModel:Path"] ??
            Path.Combine(Directory.GetCurrentDirectory(), "ModelStorage");

        // Ensure storage directory exists
        if (!Directory.Exists(_modelStoragePath))
        {
            Directory.CreateDirectory(_modelStoragePath);
        }
    }

    public StartRecordingResponse StartRecordingSessionAsync(StartRecordingRequest request)
    {
        // This method might not be needed if recording is handled client-side
        return new StartRecordingResponse
        {
            RecordingSessionId = Guid.NewGuid().ToString(),
            Message = "Recording session started."
        };
    }

    public async Task<string> TrainModelAsync(List<string> audioFilePaths, Guid modelId, string voiceModelName)
    {
        try
        {
            _logger.LogInformation("Starting voice model training for model {ModelId}", modelId);

            // Get the voice model from database to use its name
            //var voiceModel = await _context.VoiceModels.FindAsync(modelId);
            //if (voiceModel == null)
            //{
            //    throw new KeyNotFoundException($"Voice model with ID {modelId} not found");
            //}

            // Create a sanitized version of the model name
            string sanitizedModelName = SanitizeModelName(voiceModelName);

            // Add a short random suffix for uniqueness (6 characters)
            string shortRandomSuffix = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 6)
                .Replace("/", "_").Replace("+", "-");

            // Construct the full model name for HuggingFace
            string modelName = $"voice-model-{sanitizedModelName}";

            // Ensure the total length is under 96 characters
            if (modelName.Length > 96)
            {
                // Truncate the sanitized name part if needed
                int excessLength = modelName.Length - 96;
                sanitizedModelName = sanitizedModelName.Substring(0, Math.Max(sanitizedModelName.Length - excessLength, 10));
                modelName = $"{_huggingFaceUsername}/voice-model-{sanitizedModelName}";
            }

            // Check if model already exists
            string existingModel = await _huggingFaceClient.CheckExistingModelAsync(modelName, _huggingFaceUsername);

            if (existingModel == null)
            {
                _logger.LogInformation("Creating new model: {ModelName}", modelName);

                // Create new model
                await _huggingFaceClient.CreateModelAsync(
                    modelName,
                    $"Voice model for {voiceModelName} created at {DateTime.UtcNow}");
            }
            else
            {
                _logger.LogInformation("Using existing model: {ModelName}", modelName);
            }

            // Process each audio file
            int speakerCounter = 1;
            foreach (var audioPath in audioFilePaths)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", audioPath.TrimStart('/'));

                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("Audio file not found: {FilePath}", fullPath);
                    continue;
                }

                byte[] audioData = await File.ReadAllBytesAsync(fullPath);
                string speakerId = $"speaker_{speakerCounter++}";

                _logger.LogInformation("Uploading audio sample {SpeakerId} for model {ModelName}",
                    speakerId, modelName);

                await _huggingFaceClient.UploadSpeakerEmbeddingsAsync(modelName, audioData, speakerId);
            }

            // Store model reference locally
            var modelConfigPath = Path.Combine(_modelStoragePath, $"model_{modelId}.json");
            var modelConfig = new
            {
                ModelId = modelId.ToString(),
                HuggingFaceModel = modelName,
                SpeakerCount = speakerCounter - 1,
                CreatedAt = DateTime.UtcNow
            };

            await File.WriteAllTextAsync(
                modelConfigPath,
                JsonSerializer.Serialize(modelConfig, new JsonSerializerOptions { WriteIndented = true }));

            //if (voiceModel != null)
            //{
            //    voiceModel.HuggingFaceModelName = modelName;
            //    await _context.SaveChangesAsync();
            //}

            _logger.LogInformation("Voice model training completed successfully. Model saved at: {ModelPath}", modelConfigPath);

            return modelName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during voice model training for model {ModelId}", modelId);
            throw;
        }
    }

    private string SanitizeModelName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "unnamed";

        // Replace spaces and invalid characters with hyphens
        string sanitized = Regex.Replace(name, @"[^a-zA-Z0-9\-_\.]", "-");

        // Remove consecutive hyphens
        sanitized = Regex.Replace(sanitized, @"\-{2,}", "-");

        // Remove leading/trailing hyphens and dots
        sanitized = sanitized.Trim('-', '.');

        // Ensure it doesn't end with .git
        if (sanitized.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            sanitized = sanitized.Substring(0, sanitized.Length - 4);

        // If empty after sanitization, use a default
        if (string.IsNullOrEmpty(sanitized))
            return "model";

        return sanitized.ToLowerInvariant();
    }

    public async Task<string> SynthesizeSpeechAsync(string text, Guid modelId)
    {
        try
        {
            _logger.LogInformation("Synthesizing speech for model {ModelId}", modelId);

            // Load model configuration
            var modelConfigPath = Path.Combine(_modelStoragePath, $"model_{modelId}.json");

            if (!File.Exists(modelConfigPath))
            {
                throw new FileNotFoundException($"Model configuration not found for {modelId}");
            }

            var modelConfigJson = await File.ReadAllTextAsync(modelConfigPath);
            var modelConfig = JsonSerializer.Deserialize<JsonElement>(modelConfigJson);

            string huggingFaceModel = modelConfig.GetProperty("HuggingFaceModel").GetString();
            int speakerCount = modelConfig.GetProperty("SpeakerCount").GetInt32();

            if (speakerCount == 0)
            {
                throw new InvalidOperationException("No speakers available in this model");
            }

            // Use the first speaker by default
            string speakerId = "speaker_1";

            // Generate speech
            byte[] audioData = await _huggingFaceClient.SynthesizeSpeechAsync(
                huggingFaceModel,
                text,
                speakerId);

            // Save the audio file
            string outputFileName = $"synthesized_{Guid.NewGuid()}.wav";
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audio", "synthesized");

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string outputPath = Path.Combine(outputDirectory, outputFileName);
            await File.WriteAllBytesAsync(outputPath, audioData);

            // Return the web-accessible path
            return $"/audio/synthesized/{outputFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing speech for model {ModelId}", modelId);
            throw;
        }
    }
}