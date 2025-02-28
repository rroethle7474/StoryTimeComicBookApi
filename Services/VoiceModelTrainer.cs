using StoryTimeComicBookApi.Data;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Clients;
using StoryTimeComicBookApi.Services.Interfaces;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using StoryTimeComicBookApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

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

    public async Task TrainModelAsync(List<string> audioFilePaths, Guid modelId, string voiceModelName, string replicateModelId = null)
    {
        try
        {
            _logger.LogInformation("Starting voice model training for model {ModelId}", modelId);

            if (replicateModelId == null)
            {
                throw new InvalidOperationException("No Replicate model ID provided for training");
            }

            string modelVersion = null;
            ReplicateModel replicateModel = null;

            Guid replicateModelGuid;
            if (Guid.TryParse(replicateModelId, out replicateModelGuid))
            {
                replicateModel = await _context.ReplicateModels.FindAsync(replicateModelGuid);
            }

            if (replicateModel == null)
                return;


            // Create a temp directory for training data
            string trainingDataFolder = Path.Combine(Path.GetTempPath(), $"training_data_{modelId}");
            Directory.CreateDirectory(trainingDataFolder);

            try
            {
                // Get the audio snippets from the provided audio file paths
                var audioSnippets = await _context.VoiceModelAudioSnippets
                    .Where(v => v.VoiceModelId == modelId)
                    .Include(v => v.AudioSnippet)
                    .Include(v => v.Step)
                    .ToListAsync();

                if (!audioSnippets.Any())
                {
                    throw new InvalidOperationException($"No audio snippets found for voice model with ID {modelId}");
                }

                // Prepare the training data in the required format using the voice model audio snippets
                await PrepareTrainingDataAsync(audioSnippets, trainingDataFolder);

                // Create a ZIP file of the training data
                string zipPath = Path.Combine(Path.GetTempPath(), $"training_data_{modelId}.zip");
                ZipFile.CreateFromDirectory(trainingDataFolder, zipPath);

                // Train the model using the prepared zip file with the proper training structure
                modelVersion = await _replicateClient.TrainCustomModelAsync(
                    replicateModel.ModelName,
                    audioFilePaths,  // Keep this as a fallback
                    zipPath);        // Pass the prepared zip file path

                // Save the model version
                if (!string.IsNullOrEmpty(modelVersion))
                {
                    if (replicateModel != null)
                    {
                        replicateModel.ReplicateModelIdentifier = modelVersion;
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("Model training completed. Model version: {Version}", modelVersion);
            }
            finally
            {
                // Clean up the temporary folder
                //if (Directory.Exists(trainingDataFolder))
                //{
                //    Directory.Delete(trainingDataFolder, true);
                //}

                // Clean up the ZIP file
                string zipPath = Path.Combine(Path.GetTempPath(), $"training_data_{modelId}.zip");
                //if (File.Exists(zipPath))
                //{
                //    File.Delete(zipPath);
                //}
            }
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

    private async Task PrepareTrainingDataAsync(List<VoiceModelAudioSnippet> audioSnippets, string outputFolder)
    {
        try
        {
            // Create 'wavs' subdirectory as required by StyleTTS2
            string wavsFolder = Path.Combine(outputFolder, "wavs");
            Directory.CreateDirectory(wavsFolder);

            // Create training and validation data files
            using (var trainWriter = new StreamWriter(Path.Combine(outputFolder, "train_data.txt")))
            using (var valWriter = new StreamWriter(Path.Combine(outputFolder, "validation_data.txt")))
            {
                int index = 0;
                foreach (var audioSnippet in audioSnippets)
                {
                    // Get the file name without path
                    string fileName = Path.GetFileName(audioSnippet.AudioSnippet.AudioFilePath);

                    // Full path to the source audio file (relative to wwwroot)
                    string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", audioSnippet.AudioSnippet.AudioFilePath.TrimStart('/'));

                    // Validate that the file exists and is a WAV file
                    if (!File.Exists(sourcePath))
                    {
                        _logger.LogWarning("Audio file not found: {FilePath}", sourcePath);
                        continue;
                    }

                    // Copy to wavs folder with a sequential name
                    string destFileName = $"audio_{index:D4}.wav";
                    string destPath = Path.Combine(wavsFolder, destFileName);
                    File.Copy(sourcePath, destPath, true);

                    // Get the transcript text from the step if available, otherwise use a default
                    string transcriptText = "This is a voice sample for training.";
                    if (audioSnippet.Step != null && !string.IsNullOrEmpty(audioSnippet.Step.TranscriptText))
                    {
                        transcriptText = audioSnippet.Step.TranscriptText;
                    }

                    // Add to training or validation data (80/20 split)
                    string entry = $"{destFileName}|{transcriptText}";
                    if (index % 2 == 0) // Every 5th file goes to validation
                    {
                        await valWriter.WriteLineAsync(entry);
                    }
                    else
                    {
                        await trainWriter.WriteLineAsync(entry);
                    }

                    index++;
                }
            }

            // Create empty OOD_data.txt file as required by some StyleTTS2 configurations
            using (var oodWriter = new StreamWriter(Path.Combine(outputFolder, "OOD_data.txt")))
            {
                // Leave it empty for now
            }

            _logger.LogInformation("Training data prepared successfully in {OutputFolder}", outputFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing training data");
            throw;
        }
    }
}