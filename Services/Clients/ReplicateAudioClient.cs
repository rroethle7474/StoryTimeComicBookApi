using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace StoryTimeComicBookApi.Services.Clients;

public class ReplicateAudioClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReplicateAudioClient> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;
    private const string BASE_URL = "https://api.replicate.com/v1";
    
    // The custom model in the format "username/model-name"
    private readonly string _customModel;
    // This will be populated once the model is pushed to Replicate
    private string _modelVersion;

    public ReplicateAudioClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ReplicateAudioClient> logger)
    {
        _configuration = configuration;
        _apiKey = configuration["AI:Replicate:AudioApiKey"] ??
            throw new InvalidOperationException("Replicate API key not configured");

        _httpClient = httpClientFactory.CreateClient("ReplicateAudioClient");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Get the username from configuration
        string username = configuration["AI:HuggingFace:Username"] ?? "rroethle7474";
        
        // Set the custom model name
        string modelName = configuration["AI:Replicate:CustomModelName"] ?? "voice-model-01";
        
        // Create the custom model identifier
        _customModel = $"{username}/{modelName}";
        
        // Get the model version from configuration if available
        _modelVersion = configuration["AI:Replicate:ModelVersion"] ?? "";

        _logger = logger;
        
        _logger.LogInformation("Using custom model: {CustomModel}", _customModel);
    }

    /// <summary>
    /// Uploads a single audio file to Replicate and returns the URL
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file</param>
    /// <returns>URL to the uploaded audio file</returns>
    public async Task<string> UploadAudioFileAsync(string audioFilePath)
    {
        try
        {
            _logger.LogInformation("Uploading audio file to Replicate: {FilePath}", audioFilePath);
            
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", audioFilePath.TrimStart('/'));
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Audio file not found: {fullPath}");
            }
            
            // Read the audio file
            byte[] audioData = await File.ReadAllBytesAsync(fullPath);
            
            // Create upload request
            var uploadRequest = new
            {
                content_type = "audio/wav"
            };
            
            var uploadRequestJson = JsonSerializer.Serialize(uploadRequest);
            var content = new StringContent(uploadRequestJson, Encoding.UTF8, "application/json");
            
            // Get upload URL from Replicate
            var response = await _httpClient.PostAsync($"{BASE_URL}/uploads", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error response from Replicate: {ErrorContent}", errorContent);
                throw new HttpRequestException($"Error creating upload: {response.StatusCode} - {errorContent}");
            }
            
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var uploadInfo = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            
            // Get the upload URL and serving URL
            var uploadUrl = uploadInfo.GetProperty("upload_url").GetString();
            var servingUrl = uploadInfo.GetProperty("serving_url").GetString();
            
            // Upload the file to the provided URL
            using var uploadClient = new HttpClient();
            var fileContent = new ByteArrayContent(audioData);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            
            var uploadResponse = await uploadClient.PutAsync(uploadUrl, fileContent);
            
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                _logger.LogError("Error uploading file to Replicate: {ErrorContent}", errorContent);
                throw new HttpRequestException($"Error uploading file: {uploadResponse.StatusCode} - {errorContent}");
            }
            
            _logger.LogInformation("Audio file uploaded successfully. Serving URL: {Url}", servingUrl);
            
            return servingUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading audio file");
            throw;
        }
    }

    /// <summary>
    /// Prepares voice samples for use with StyleTTS2 by uploading individual files
    /// </summary>
    /// <param name="audioFilePaths">List of audio file paths</param>
    /// <returns>List of URLs to the uploaded audio files</returns>
    public async Task<List<string>> PrepareVoiceSamplesAsync(List<string> audioFilePaths)
    {
        try
        {
            _logger.LogInformation("Preparing voice samples for StyleTTS2 using HTTP upload");
            
            var audioUrls = new List<string>();
            
            foreach (var audioPath in audioFilePaths)
            {
                var audioUrl = await UploadAudioFileAsync(audioPath);
                audioUrls.Add(audioUrl);
                _logger.LogDebug("Added audio URL to collection: {Url}", audioUrl);
            }
            
            if (audioUrls.Count == 0)
            {
                throw new InvalidOperationException("No valid audio files were uploaded");
            }
            
            _logger.LogInformation("Successfully uploaded {Count} audio files", audioUrls.Count);
            
            return audioUrls;
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
            _logger.LogInformation("Getting latest version for model {Model}", _customModel);
            
            // Get the model versions
            var response = await _httpClient.GetAsync($"{BASE_URL}/models/{_customModel}/versions");
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
    /// Creates a speech synthesis prediction with StyleTTS2 using individual audio files
    /// </summary>
    /// <param name="text">The text to synthesize</param>
    /// <param name="voiceSampleUrls">List of URLs to the voice sample files</param>
    /// <returns>The prediction ID</returns>
    public async Task<string> CreatePredictionAsync(string text, List<string> voiceSampleUrls)
    {
        try
        {
            // Get the model version if we don't have it yet
            if (string.IsNullOrEmpty(_modelVersion))
            {
                _modelVersion = await GetModelVersionAsync();
            }
            
            _logger.LogInformation("Creating prediction with model {Model} version {Version}", _customModel, _modelVersion);
            
            // Create the request payload for StyleTTS2
            var payload = new
            {
                version = _modelVersion,
                input = new
                {
                    text = text,
                    voice_samples = voiceSampleUrls,
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
    /// Creates a speech synthesis prediction with StyleTTS2 using a ZIP file
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
            
            _logger.LogInformation("Creating prediction with model {Model} version {Version} using ZIP file", _customModel, _modelVersion);
            
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

    /// <summary>
    /// Prepares voice samples for use with StyleTTS2 by creating a zip file
    /// </summary>
    /// <param name="audioFilePaths">List of audio file paths</param>
    /// <returns>Data URI of the ZIP file containing voice samples</returns>
    public async Task<string> PrepareVoiceSamplesAsZipAsync(List<string> audioFilePaths)
    {
        try
        {
            _logger.LogInformation("Preparing voice samples for StyleTTS2 as ZIP file");

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
            _logger.LogError(ex, "Error preparing voice samples as ZIP");
            throw;
        }
    }

    /// <summary>
    /// Trains a custom StyleTTS2 model on Replicate using the provided voice samples
    /// </summary>
    /// <param name="voiceSampleUrls">List of URLs to the voice sample files</param>
    /// <param name="modelName">Name for the custom model</param>
    /// <param name="audioFilePaths">Original audio file paths (used for ZIP creation if needed)</param>
    /// <returns>The trained model version ID</returns>
    public async Task<string> TrainCustomModelAsync(string modelName, List<string> audioFilePaths = null)
    {
        try
        {
            _logger.LogInformation("Training custom StyleTTS2 model: {ModelName}", modelName);


            // follow this format for how to structure the zip file for training
            //https://replicate.com/adirik/styletts2/train
            // Get the username from configuration
            string username = _configuration["AI:HuggingFace:Username"] ?? "rroethle7474";
            
            // Create the destination in the format "username/model-name"
            string destination = $"{username}/{modelName}";
            
            _logger.LogInformation("Using destination model: {Destination}", destination);
            
            // Determine if we should use ZIP file approach or individual URLs
            object voiceSamplesInput = null;
            
            if (audioFilePaths != null && audioFilePaths.Count > 0)
            {
                // Use ZIP file approach for training
                _logger.LogInformation("Using ZIP file approach for training");
                string zipDataUri = await PrepareVoiceSamplesAsZipAsync(audioFilePaths);
                voiceSamplesInput = zipDataUri;
            }
            
            // Create the request payload for training
            var payload = new
            {
                // Specify the destination model
                destination = destination,
                
                // Training parameters
                input = new
                {
                    voice_samples = voiceSamplesInput,
                    // Add any other training parameters here
                    epochs = 10,
                    batch_size = 4,
                    learning_rate = 0.0001
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            _logger.LogDebug("Training payload: {Payload}", jsonPayload);
            
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Use the specific StyleTTS2 model version for training
            string styleTTS2Version = "989cb5ea6d2401314eb30685740cb9f6fd1c9001b8940659b406f952837ab5ac";
            var response = await _httpClient.PostAsync(
                $"{BASE_URL}/models/adirik/styletts2/versions/{styleTTS2Version}/trainings", 
                content);
            
            // If the response is not successful, log the response content for debugging
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error response from Replicate: {ErrorContent}", errorContent);
                throw new HttpRequestException($"Error creating training: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Training response: {Response}", jsonResponse);
            
            var training = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            var trainingId = training.GetProperty("id").GetString();
            
            _logger.LogInformation("Training started with ID: {TrainingId}", trainingId);
            
            // Poll until the training is complete
            string status = "starting";
            JsonElement trainingStatus = new JsonElement();

            while (status != "succeeded" && status != "failed")
            {
                var statusResponse = await _httpClient.GetAsync($"{BASE_URL}/trainings/{trainingId}");
                statusResponse.EnsureSuccessStatusCode();

                var statusJsonResponse = await statusResponse.Content.ReadAsStringAsync();
                trainingStatus = JsonSerializer.Deserialize<JsonElement>(statusJsonResponse);

                status = trainingStatus.GetProperty("status").GetString();
                
                _logger.LogInformation("Training status: {Status}", status);

                if (status == "failed")
                {
                    var error = trainingStatus.TryGetProperty("error", out var errorElement) 
                        ? errorElement.GetString() 
                        : "Unknown error";
                    throw new Exception($"Training failed: {error}");
                }

                if (status != "succeeded")
                {
                    // Wait before polling again (training can take a long time)
                    await Task.Delay(30000); // 30 seconds
                }
            }

            // After successful training, get the latest version of the custom model
            var modelResponse = await _httpClient.GetAsync($"{BASE_URL}/models/{destination}/versions");
            modelResponse.EnsureSuccessStatusCode();
            
            var modelContent = await modelResponse.Content.ReadAsStringAsync();
            var modelJson = JsonSerializer.Deserialize<JsonElement>(modelContent);
            
            // Get the first (latest) version
            if (modelJson.TryGetProperty("results", out var resultsElement) && 
                resultsElement.GetArrayLength() > 0)
            {
                var latestVersion = resultsElement[0];
                if (latestVersion.TryGetProperty("id", out var idElement))
                {
                    var modelVersion = idElement.GetString();
                    
                    // Update the model version and custom model name in memory
                    _modelVersion = modelVersion;
                    
                    return modelVersion;
                }
            }
            
            throw new InvalidOperationException("Training completed but no model version found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training custom model");
            throw;
        }
    }
    
    private string SanitizeModelName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "unnamed-model";

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

    /// <summary>
    /// Creates a speech synthesis prediction with a specific model version
    /// </summary>
    /// <param name="text">The text to synthesize</param>
    /// <param name="modelVersion">The specific model version to use</param>
    /// <returns>The prediction ID</returns>
    public async Task<string> CreatePredictionWithModelVersionAsync(string text, string modelVersion)
    {
        try
        {
            _logger.LogInformation("Creating prediction with specific model version: {Version}", modelVersion);
            
            // Create the request payload
            var payload = new
            {
                version = modelVersion,
                input = new
                {
                    text = text,
                    // Add any other parameters here
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
            _logger.LogError(ex, "Error creating prediction with specific model version");
            throw;
        }
    }
}