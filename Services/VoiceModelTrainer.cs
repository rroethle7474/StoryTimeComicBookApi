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
    private readonly ReplicateAudioClient _replicateClient;
    private readonly string _modelStoragePath;

    public VoiceModelTrainer(
        VoiceMimicDataContext context,
        IConfiguration configuration,
        ILogger<VoiceModelTrainer> logger,
        ReplicateAudioClient replicateClient)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _replicateClient = replicateClient;

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

            // Check if we should perform actual model training or just use reference-based generation
            // remove this check as we will always use the perform training
            // need to get steps as well for this to match them up for creating zip file.
            bool performModelTraining = _configuration.GetValue<bool>("VoiceModel:PerformTraining", false);
            string modelVersion = null;
            List<string> voiceSampleUrls = null;
            
            // If we're not doing actual training, we need to upload individual files for reference-based generation
            if (!performModelTraining)
            {
                // With StyleTTS2, we don't need to "train" a model in the traditional sense
                // Instead, we prepare the voice samples by uploading them to Replicate
                
                // Upload each audio file and get their URLs
                voiceSampleUrls = await _replicateClient.PrepareVoiceSamplesAsync(audioFilePaths);
                
                _logger.LogInformation("Voice samples prepared and uploaded: {Count} files", voiceSampleUrls.Count);
                _logger.LogInformation("Skipping actual model training, using reference-based generation");
            }
            else
            {
                _logger.LogInformation("Starting actual model training for {ModelName}", voiceModelName);
                
                // Create a unique model name for this training session
                // This allows multiple models to be trained without conflicts
                string customModelName = $"voice-model-{modelId.ToString().Substring(0, 8)}";
                
                // Update the configuration with the custom model name for this session
                // This ensures that the ReplicateAudioClient will use this model for predictions
                string configKey = "AI:Replicate:CustomModelName";
                if (_configuration is IConfigurationRoot configRoot)
                {
                    // Create a memory configuration provider to override the setting
                    var memoryConfig = new Dictionary<string, string>
                    {
                        { configKey, customModelName }
                    };
                    
                    // Add the memory provider to the configuration
                    configRoot.GetSection("AI:Replicate")["CustomModelName"] = customModelName;
                    
                    _logger.LogInformation("Set custom model name for this session: {ModelName}", customModelName);
                }
                
                // For training, we'll use the ZIP approach since the uploads URL might not exist
                // We'll still need URLs for testing and future reference-based generation
                //voiceSampleUrls = await _replicateClient.PrepareVoiceSamplesAsync(audioFilePaths);
                
                //_logger.LogInformation("Voice samples prepared and uploaded: {Count} files", voiceSampleUrls.Count);
                
                // Train a custom model using the ZIP approach with original audio file paths
                modelVersion = await _replicateClient.TrainCustomModelAsync(
                    customModelName,
                    audioFilePaths);  // Pass the original file paths for ZIP creation
                
                _logger.LogInformation("Model training completed. Model version: {Version}", modelVersion);
            }

            // Test the voice model with a simple sample text
            var testText = "This is a test of the voice model for " + SanitizeModelName(voiceModelName);
            
            // Create a prediction with the voice samples
            //var predictionId = await _replicateClient.CreatePredictionAsync(testText, voiceSampleUrls);
            
            // Wait for the test prediction to complete
            //var audioData = await _replicateClient.GetPredictionResultAsync(predictionId);
            
            // Save the test audio
            string testOutputFileName = $"test_{modelId}.wav";
            string testOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audio", "tests");
            
            if (!Directory.Exists(testOutputDirectory))
            {
                Directory.CreateDirectory(testOutputDirectory);
            }
            
            string testOutputPath = Path.Combine(testOutputDirectory, testOutputFileName);
            //await File.WriteAllBytesAsync(testOutputPath, audioData);

            // Store model information in local storage
            var modelConfigPath = Path.Combine(_modelStoragePath, $"model_{modelId}.json");
            //var modelConfig = new
            //{
            //    ModelId = modelId.ToString(),
            //    VoiceSampleUrls = voiceSampleUrls,
            //    CreatedAt = DateTime.UtcNow,
            //    ModelName = voiceModelName,
            //    TestAudioPath = $"/audio/tests/{testOutputFileName}",
            //    IsTrainedModel = performModelTraining,
            //    ModelVersion = modelVersion,
            //    CustomModelName = performModelTraining ? $"voice-model-{modelId.ToString().Substring(0, 8)}" : null
            //};

            //await File.WriteAllTextAsync(
            //    modelConfigPath,
            //    JsonSerializer.Serialize(modelConfig, new JsonSerializerOptions { WriteIndented = true }));

            //_logger.LogInformation("Voice model setup completed successfully. Model config saved at: {ModelPath}", modelConfigPath);

            return modelConfigPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during voice model setup for model {ModelId}", modelId);
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

            // Check if this is a trained model
            bool isTrainedModel = modelConfig.TryGetProperty("IsTrainedModel", out var isTrainedElement) && 
                                 isTrainedElement.GetBoolean();
            
            string modelVersion = null;
            string customModelName = null;
            
            if (isTrainedModel)
            {
                // Get the model version
                if (modelConfig.TryGetProperty("ModelVersion", out var versionElement))
                {
                    modelVersion = versionElement.GetString();
                    _logger.LogInformation("Using trained model version: {Version}", modelVersion);
                }
                
                // Get the custom model name
                if (modelConfig.TryGetProperty("CustomModelName", out var modelNameElement))
                {
                    customModelName = modelNameElement.GetString();
                    _logger.LogInformation("Using custom model name: {ModelName}", customModelName);
                    
                    // Update the configuration with the custom model name for this session
                    // This ensures that the ReplicateAudioClient will use this model for predictions
                    if (_configuration is IConfigurationRoot configRoot && !string.IsNullOrEmpty(customModelName))
                    {
                        configRoot.GetSection("AI:Replicate")["CustomModelName"] = customModelName;
                        _logger.LogInformation("Set custom model name for this session: {ModelName}", customModelName);
                    }
                }
            }

            // Check if we have individual voice sample URLs (new method) or a single URL (old method)
            List<string> voiceSampleUrls;
            
            if (modelConfig.TryGetProperty("VoiceSampleUrls", out var urlsElement))
            {
                // New method with individual URLs
                voiceSampleUrls = urlsElement.EnumerateArray()
                    .Select(url => url.GetString())
                    .ToList();
                
                _logger.LogInformation("Using {Count} individual voice sample URLs", voiceSampleUrls.Count);
                
                // If we have a trained model, use it directly
                if (isTrainedModel && !string.IsNullOrEmpty(modelVersion))
                {
                    // Create a prediction with the trained model
                    var trainedPredictionId = await _replicateClient.CreatePredictionWithModelVersionAsync(
                        text, modelVersion);
                    
                    // Get the prediction result (audio data)
                    var audioData = await _replicateClient.GetPredictionResultAsync(trainedPredictionId);
                    
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
                
                // Otherwise, use reference-based generation with the voice samples
                var newPredictionId = await _replicateClient.CreatePredictionAsync(text, voiceSampleUrls);
                
                // Get the prediction result (audio data)
                var newAudioData = await _replicateClient.GetPredictionResultAsync(newPredictionId);
                
                // Save the audio file
                string newOutputFileName = $"synthesized_{Guid.NewGuid()}.wav";
                string newOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audio", "synthesized");
                
                if (!Directory.Exists(newOutputDirectory))
                {
                    Directory.CreateDirectory(newOutputDirectory);
                }
                
                string newOutputPath = Path.Combine(newOutputDirectory, newOutputFileName);
                await File.WriteAllBytesAsync(newOutputPath, newAudioData);
                
                // Return the web-accessible path
                return $"/audio/synthesized/{newOutputFileName}";
            }
            else if (modelConfig.TryGetProperty("VoiceSamplesUrl", out var urlElement))
            {
                // Old method with a single URL (ZIP file or data URI)
                var voiceSamplesUrl = urlElement.GetString();
                
                if (string.IsNullOrEmpty(voiceSamplesUrl))
                {
                    throw new InvalidOperationException("No voice samples URL found in model configuration");
                }
                
                _logger.LogInformation("Using legacy voice samples URL (ZIP file or data URI)");
                
                // Create a prediction with Replicate using the old method
                var predictionId = await _replicateClient.CreatePredictionAsync(text, voiceSamplesUrl);
                
                // Get the prediction result (audio data)
                var audioData = await _replicateClient.GetPredictionResultAsync(predictionId);
                
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
            else
            {
                throw new InvalidOperationException("No voice samples information found in model configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing speech for model {ModelId}", modelId);
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

        // If empty after sanitization, use a default
        if (string.IsNullOrEmpty(sanitized))
            return "model";

        return sanitized.ToLowerInvariant();
    }
}