using StoryTimeComicBookApi.Data;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Clients;
using StoryTimeComicBookApi.Services.Interfaces;
using System.Text.Json;

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

    public async Task<string> TrainModelAsync(List<string> audioFilePaths, Guid modelId)
    {
        try
        {
            _logger.LogInformation("Starting voice model training for model {ModelId}", modelId);

            // Generate a model name for HuggingFace based on the ID
            string modelName = $"{_huggingFaceUsername}/voice-model-{modelId}";

            // Check if model already exists
            string existingModel = await _huggingFaceClient.CheckExistingModelAsync(modelName);

            if (existingModel == null)
            {
                _logger.LogInformation("Creating new model: {ModelName}", modelName);

                // Create new model
                await _huggingFaceClient.CreateModelAsync(
                    modelName,
                    $"Voice model {modelId} created at {DateTime.UtcNow}");
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

            // Update the voice model in the database with the HuggingFace model name
            var voiceModel = await _context.VoiceModels.FindAsync(modelId);
            if (voiceModel != null)
            {
                voiceModel.HuggingFaceModelName = modelName;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Voice model training completed successfully. Model saved at: {ModelPath}", modelConfigPath);

            return modelConfigPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during voice model training for model {ModelId}", modelId);
            throw;
        }
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