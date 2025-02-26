using StoryTimeComicBookApi.Models.Huggingface;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace StoryTimeComicBookApi.Services.Clients;

public class HuggingFaceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HuggingFaceClient> _logger;
    private readonly string _apiKey;
    private readonly string _baseModelId = "coqui/xtts-v2";
    private readonly string _huggingFaceUserName;

    public HuggingFaceClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HuggingFaceClient> logger)
    {
        _apiKey = configuration["AI:HuggingFace:ApiKey"] ??
            throw new InvalidOperationException("HuggingFace API key not configured");
        _huggingFaceUserName = configuration["AI:HuggingFace:Username"] ??
            throw new InvalidOperationException("HuggingFace username not configured");
        _httpClient = httpClientFactory.CreateClient("HuggingFaceApi");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _logger = logger;
    }

    public async Task<string> CheckExistingModelAsync(string modelName, string userName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://huggingface.co/api/models/{userName}/{modelName}");

            if (response.IsSuccessStatusCode)
            {
                return modelName; // Model exists
            }

            return null; // Model doesn't exist
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if model exists: {ModelName}", modelName);
            throw;
        }
    }

    public async Task<string> CreateModelAsync(string modelName, string description)
    {
        try
        {
            // Create a proper object with named properties
            var createModelRequest = new
            {
                name = modelName,
                @private = true, // Use @ prefix for C# keywords
                description = description
            };

            var json = JsonSerializer.Serialize(createModelRequest);
            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                "https://huggingface.co/api/repos/create",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create model: {errorContent}");
            }

            return modelName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating model: {ModelName}", modelName);
            throw;
        }
    }

    public async Task<bool> UploadSpeakerEmbeddingsAsync(string modelName, byte[] audioData, string speakerId)
    {
        try
        {
            // Create a multipart form content
            using var formContent = new MultipartFormDataContent();

            // Add the audio file
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            formContent.Add(audioContent, "audio", $"{speakerId}.wav");

            // Add text parameter (typically what you want to synthesize - just a placeholder for training)
            formContent.Add(new StringContent("This is a voice sample for training."), "text");

            // Add language parameter (optional)
            formContent.Add(new StringContent("en"), "language");

            // Add speaker ID
            formContent.Add(new StringContent(speakerId), "speaker_id");

            // Use the base XTTS-v2 model directly
            var response = await _httpClient.PostAsync(
                $"https://api-inference.huggingface.co/models/coqui/XTTS-v2",
                formContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to process voice sample: {errorContent}");
            }

            // Store the audio file in your repository
            var fileContent = new ByteArrayContent(audioData);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

            // Create a repository file
            var uploadResponse = await _httpClient.PutAsync(
                $"https://huggingface.co/api/repos/{_huggingFaceUserName}/{modelName}/add-file?path=speakers/{speakerId}.wav",
                fileContent);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to upload audio file to repository: {errorContent}");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing voice sample for model: {ModelName}", modelName);
            throw;
        }
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string modelName, string text, string speakerId)
    {
        try
        {
            // Create the request payload
            var requestContent = new MultipartFormDataContent();

            // Add the text to synthesize
            requestContent.Add(new StringContent(text), "text");

            // Add language parameter
            requestContent.Add(new StringContent("en"), "language");

            // Add speaker ID
            requestContent.Add(new StringContent(speakerId), "speaker_id");

            // Add reference audio path from your repository
            var referenceAudioUrl = $"https://huggingface.co/{_huggingFaceUserName}/{modelName}/resolve/main/speakers/{speakerId}.wav";
            requestContent.Add(new StringContent(referenceAudioUrl), "speaker_wav_url");

            // Call the XTTS-v2 model
            var response = await _httpClient.PostAsync(
                "https://api-inference.huggingface.co/models/coqui/XTTS-v2",
                requestContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to synthesize speech: {errorContent}");
            }

            // Return the audio bytes
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing speech with model: {ModelName}", modelName);
            throw;
        }
    }

    public async Task<List<HuggingFaceModelInfo>> GetUserModelsAsync(string username, string prefix = "voice-model-")
    {
        try
        {
            // Get all models for the user
            var response = await _httpClient.GetAsync($"https://huggingface.co/api/models?author={username}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to retrieve models: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var allModels = JsonSerializer.Deserialize<List<HuggingFaceModelInfo>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Filter models that match your voice model naming pattern
            var voiceModels = allModels
                .Where(m => m.ModelId.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return voiceModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models for user {Username}", username);
            throw;
        }
    }

    public async Task<bool> DeleteModelAsync(string modelName)
    {
        try
        {
            var fullModelName = $"{_huggingFaceUserName}/{modelName}";
            var response = await _httpClient.DeleteAsync($"https://huggingface.co/api/repos/delete?repo={fullModelName}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to delete model: {errorContent}");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting model: {ModelName}", modelName);
            throw;
        }
    }
}