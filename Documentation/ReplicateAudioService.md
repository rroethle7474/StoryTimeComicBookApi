# Implementing Voice Cloning with Replicate's Tortoise TTS in .NET

This guide details how to implement voice cloning functionality in a .NET application using Replicate's Tortoise TTS model.

## Overview

Tortoise TTS is a high-quality voice cloning model that can analyze voice samples and generate speech in the same voice for any text input. This implementation:

1. Uses Replicate's API to upload voice samples
2. Stores references to these samples locally
3. Sends both text and voice sample references when generating speech

## Implementation Steps

### 1. Add Replicate API Key to Configuration

Add your Replicate API key to `appsettings.json`:

```json
{
  "AI": {
    "Replicate": {
      "ApiKey": "YOUR_REPLICATE_API_KEY"
    }
  }
}
```

### 2. Register Services in Program.cs

```csharp
// Add HttpClient for Replicate API
builder.Services.AddHttpClient("ReplicateAudioClient", client => {
    client.Timeout = TimeSpan.FromMinutes(5); // Longer timeout for audio generation
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Register clients and services
builder.Services.AddScoped<ReplicateAudioClient>();
builder.Services.AddScoped<IVoiceModelTrainer, VoiceModelTrainer>();
```

### 3. Create ReplicateAudioClient Class

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StoryTimeComicBookApi.Services.Clients;

public class ReplicateAudioClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReplicateAudioClient> _logger;
    private readonly string _apiKey;
    private const string BASE_URL = "https://api.replicate.com/v1";
    private const string TORTOISE_MODEL = "afiaka87/tortoise-tts";
    private const string MODEL_VERSION = "2ef373b6f2253fc83ee82ca2b3e959a8ed310ef2b7f45a481fe76d3bd25b8b23";

    public ReplicateAudioClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ReplicateAudioClient> logger)
    {
        _apiKey = configuration["AI:Replicate:ApiKey"] ??
            throw new InvalidOperationException("Replicate API key not configured");
        
        _httpClient = httpClientFactory.CreateClient("ReplicateAudioClient");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        _logger = logger;
    }

    /// <summary>
    /// Uploads a voice sample file to Replicate and returns the URL
    /// </summary>
    public async Task<string> UploadVoiceSampleAsync(byte[] audioData, string voiceId)
    {
        try
        {
            // For Replicate, we need to first upload the file to get a URL
            var uploadUrl = await GetUploadUrlAsync();
            
            // Upload the audio file
            var audioUrl = await UploadFileAsync(uploadUrl, audioData, $"{voiceId}.wav");
            
            return audioUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading voice sample");
            throw;
        }
    }

    private async Task<string> GetUploadUrlAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{BASE_URL}/uploads",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var uploadData = JsonSerializer.Deserialize<JsonElement>(content);
            
            return uploadData.GetProperty("upload_url").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload URL from Replicate");
            throw;
        }
    }

    private async Task<string> UploadFileAsync(string uploadUrl, byte[] fileData, string fileName)
    {
        try
        {
            // Create a temporary client without auth headers for the upload
            using var uploadClient = new HttpClient();
            
            var content = new ByteArrayContent(fileData);
            content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            
            var response = await uploadClient.PutAsync(uploadUrl, content);
            response.EnsureSuccessStatusCode();
            
            // Extract the URL from the upload URL
            var fileUrl = uploadUrl.Split('?')[0];
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Replicate");
            throw;
        }
    }

    /// <summary>
    /// Creates a speech synthesis prediction with Tortoise TTS
    /// </summary>
    /// <param name="text">The text to synthesize</param>
    /// <param name="voiceUrls">List of URLs to voice sample files</param>
    /// <param name="isTraining">Whether this is a training run (faster but lower quality)</param>
    /// <returns>The prediction ID</returns>
    public async Task<string> CreatePredictionAsync(string text, List<string> voiceUrls, bool isTraining = false)
    {
        try
        {
            // Create the request payload
            var payload = new
            {
                version = MODEL_VERSION,
                input = new
                {
                    text = text,
                    voice_samples = voiceUrls,
                    preset = isTraining ? "ultra_fast" : "standard", // Use ultra_fast for training, standard for final output
                    num_autoregressive_samples = isTraining ? 1 : 4,  // Lower for training
                    seed = 0, // Fixed seed for consistent results
                    temperature = 0.8,
                    diffusion_temperature = 1.0,
                    length_penalty = 1.0,
                    top_p = 0.8,
                    cond_free = false,
                    use_deterministic_seed = true,
                    k = 1
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{BASE_URL}/predictions", content);
            response.EnsureSuccessStatusCode();
            
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var prediction = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            
            return prediction.GetProperty("id").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prediction with Replicate");
            throw;
        }
    }

    /// <summary>
    /// Gets the result of a prediction once it's complete
    /// </summary>
    /// <param name="predictionId">The prediction ID</param>
    /// <returns>The audio data as bytes</returns>
    public async Task<byte[]> GetPredictionResultAsync(string predictionId)
    {
        try
        {
            // Poll until the prediction is complete
            string status = "starting";
            JsonElement prediction = new JsonElement();
            
            while (status != "succeeded" && status != "failed")
            {
                var response = await _httpClient.GetAsync($"{BASE_URL}/predictions/{predictionId}");
                response.EnsureSuccessStatusCode();
                
                var jsonResponse = await response.Content.ReadAsStringAsync();
                prediction = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                
                status = prediction.GetProperty("status").GetString();
                
                if (status == "failed")
                {
                    var error = prediction.GetProperty("error").GetString();
                    throw new Exception($"Prediction failed: {error}");
                }
                
                if (status != "succeeded")
                {
                    // Wait before polling again
                    await Task.Delay(2000);
                }
            }
            
            // Get the output URL
            var outputUrl = prediction.GetProperty("output").GetString();
            
            // Download the audio file
            using var client = new HttpClient();
            var audioData = await client.GetByteArrayAsync(outputUrl);
            
            return audioData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prediction result from Replicate");
            throw;
        }
    }
}
```

### 4. Update VoiceModelTrainer Class

```csharp
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

            // First, upload all the audio files to Replicate and get their URLs
            var audioUrls = new List<string>();
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

                _logger.LogInformation("Uploading audio sample {SpeakerId}", speakerId);

                string audioUrl = await _replicateClient.UploadVoiceSampleAsync(audioData, speakerId);
                audioUrls.Add(audioUrl);
            }

            if (!audioUrls.Any())
            {
                throw new InvalidOperationException("No valid audio files found for training");
            }

            // Test the voice model with a simple training sample
            var trainingText = "This is a test of the voice model for " + SanitizeModelName(voiceModelName);
            var predictionId = await _replicateClient.CreatePredictionAsync(trainingText, audioUrls, true);
            
            // Wait for the training test to complete
            await _replicateClient.GetPredictionResultAsync(predictionId);

            // Store model information in local storage
            var modelConfigPath = Path.Combine(_modelStoragePath, $"model_{modelId}.json");
            var modelConfig = new
            {
                ModelId = modelId.ToString(),
                VoiceUrls = audioUrls,
                SpeakerCount = audioUrls.Count,
                CreatedAt = DateTime.UtcNow,
                ModelName = voiceModelName
            };

            await File.WriteAllTextAsync(
                modelConfigPath,
                JsonSerializer.Serialize(modelConfig, new JsonSerializerOptions { WriteIndented = true }));

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

            var voiceUrls = modelConfig
                .GetProperty("VoiceUrls")
                .EnumerateArray()
                .Select(v => v.GetString())
                .ToList();

            if (!voiceUrls.Any())
            {
                throw new InvalidOperationException("No voice samples found in model configuration");
            }

            // Create a prediction with Replicate
            var predictionId = await _replicateClient.CreatePredictionAsync(text, voiceUrls, false);
            
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
```

## How It Works

### Voice Model Training Process

1. **Upload Voice Samples**:
   - Voice samples are uploaded to Replicate's servers
   - URLs to these samples are obtained and stored locally
   - A quick test synthesis is performed to ensure everything works

2. **Local Storage**:
   - Sample URLs and metadata are stored in a JSON file
   - No actual model weights are stored, just references to the samples

### Speech Synthesis Process

1. **Load Configuration**:
   - Load the voice sample URLs from storage
   
2. **Create Prediction**:
   - Send the text to be spoken and voice sample URLs to Replicate
   - The Tortoise TTS model uses the samples to clone the voice for new text
   
3. **Retrieve and Save Result**:
   - Poll until the synthesis is complete
   - Download the resulting audio file
   - Save it locally and return the path

## Important Notes

- Replicate charges per-prediction based on processing time
- Each speech synthesis request includes the voice samples and reanalyzes them
- Voice samples should be clear recordings of natural speech
- For best results, provide 3-5 diverse samples of your voice

## Customization Options

The Tortoise TTS model provides several parameters to customize the generation:

- `preset`: Controls generation speed/quality (ultra_fast, fast, standard, high_quality)
- `num_autoregressive_samples`: Higher values improve quality but take longer
- `temperature`: Controls randomness in the generation
- `diffusion_temperature`: Controls noise added during the diffusion process
- `length_penalty`: Adjusts timing/pacing of speech

These parameters can be adjusted in the `CreatePredictionAsync` method to balance quality vs. speed.