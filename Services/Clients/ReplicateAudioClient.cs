using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.IO.Compression;

namespace StoryTimeComicBookApi.Services.Clients;

public class ReplicateAudioClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReplicateAudioClient> _logger;
    private readonly string _apiKey;
    private const string BASE_URL = "https://api.replicate.com/v1";
    
    // Update to use the user's custom model
    private const string CUSTOM_MODEL = "rroethle7474/voice-model-01";
    // This will be populated once the model is pushed to Replicate
    private string _modelVersion;

    public ReplicateAudioClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ReplicateAudioClient> logger)
    {
        _apiKey = configuration["AI:Replicate:AudioApiKey"] ??
            throw new InvalidOperationException("Replicate API key not configured");

        _httpClient = httpClientFactory.CreateClient("ReplicateAudioClient");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Get the model version from configuration if available
        _modelVersion = configuration["AI:Replicate:ModelVersion"] ?? "";

        _logger = logger;
    }

    /// <summary>
    /// Prepares voice samples for use with StyleTTS2 by creating a zip file
    /// </summary>
    /// <param name="audioFiles">List of audio file paths</param>
    /// <returns>URL to the uploaded zip file</returns>
    public async Task<string> PrepareVoiceSamplesAsync(List<string> audioFilePaths)
    {
        try
        {
            _logger.LogInformation("Preparing voice samples for StyleTTS2");

            // Create a temporary zip file
            string zipPath = Path.Combine(Path.GetTempPath(), $"voice_samples_{Guid.NewGuid()}.zip");
            
            try
            {
                // Create the zip file containing all audio samples
                using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    int fileCounter = 1;
                    foreach (var audioPath in audioFilePaths)
                    {
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", audioPath.TrimStart('/'));
                        
                        if (!File.Exists(fullPath))
                        {
                            _logger.LogWarning("Audio file not found: {FilePath}", fullPath);
                            continue;
                        }
                        
                        // Add the file to the zip archive with a simple numbered name
                        var entryName = $"sample_{fileCounter++}.wav";
                        zipArchive.CreateEntryFromFile(fullPath, entryName);
                        _logger.LogDebug("Added {FileName} to zip archive", entryName);
                    }
                }
                
                // Read the zip file
                byte[] zipData = await File.ReadAllBytesAsync(zipPath);
                
                // Convert the zip file to a data URI
                string base64Data = Convert.ToBase64String(zipData);
                string dataUri = $"data:application/zip;base64,{base64Data}";
                
                _logger.LogInformation("Created data URI for voice samples (size: {Size} bytes)", zipData.Length);
                
                return dataUri;
            }
            finally
            {
                // Clean up the temporary zip file
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing voice samples");
            throw;
        }
    }

    /// <summary>
    /// Gets the latest version of the custom model
    /// </summary>
    /// <returns>The model version ID</returns>
    public async Task<string> GetModelVersionAsync()
    {
        // If we already have a version, return it
        if (!string.IsNullOrEmpty(_modelVersion))
        {
            return _modelVersion;
        }

        try
        {
            _logger.LogInformation("Getting latest version for model {Model}", CUSTOM_MODEL);
            
            // Get the model versions
            var response = await _httpClient.GetAsync($"{BASE_URL}/models/{CUSTOM_MODEL}/versions");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Get the first (latest) version
            if (responseJson.TryGetProperty("results", out var resultsElement) && 
                resultsElement.GetArrayLength() > 0)
            {
                var latestVersion = resultsElement[0];
                if (latestVersion.TryGetProperty("id", out var idElement))
                {
                    _modelVersion = idElement.GetString();
                    _logger.LogInformation("Found model version: {Version}", _modelVersion);
                    return _modelVersion;
                }
            }
            
            throw new InvalidOperationException("No versions found for the model. Please push a version to Replicate first.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model version");
            throw;
        }
    }

    /// <summary>
    /// Creates a speech synthesis prediction with StyleTTS2
    /// </summary>
    /// <param name="text">The text to synthesize</param>
    /// <param name="voiceSamplesUrl">URL or data URI to the zip file containing voice samples</param>
    /// <returns>The prediction ID</returns>
    public async Task<string> CreatePredictionAsync(string text, string voiceSamplesUrl)
    {
        try
        {
            // Get the model version if we don't have it yet
            if (string.IsNullOrEmpty(_modelVersion))
            {
                _modelVersion = await GetModelVersionAsync();
            }
            
            _logger.LogInformation("Creating prediction with model {Model} version {Version}", CUSTOM_MODEL, _modelVersion);
            
            // Create the request payload for StyleTTS2
            var payload = new
            {
                version = _modelVersion,
                input = new
                {
                    text = text,
                    voice_samples = voiceSamplesUrl,
                    // Add any other StyleTTS2 parameters here
                    speed = 1.0,
                    noise_scale = 0.667,
                    noise_scale_w = 0.8,
                    length_scale = 1.0
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            _logger.LogDebug("Prediction payload: {Payload}", jsonPayload);
            
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BASE_URL}/predictions", content);
            
            // If the response is not successful, log the response content for debugging
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error response from Replicate: {ErrorContent}", errorContent);
                throw new HttpRequestException($"Error creating prediction: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Prediction response: {Response}", jsonResponse);
            
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