using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StoryTimeComicBookApi.Models.Gemini;
using System.Net.Http;
using StoryTimeComicBookApi.Models.Huggingface;

namespace StoryTimeComicBookApi.Services.Clients;

public class HuggingFaceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HuggingFaceClient> _logger;
    private readonly string _apiKey;
    private readonly string _baseModelId = "coqui/xtts-v2";

    public HuggingFaceClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HuggingFaceClient> logger)
    {
        _apiKey = configuration["AI:HuggingFace:ApiKey"] ??
            throw new InvalidOperationException("HuggingFace API key not configured");
        _httpClient = httpClientFactory.CreateClient("HuggingFaceApi");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _logger = logger;
    }

    public async Task<string> CheckExistingModelAsync(string modelName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://huggingface.co/api/models/{modelName}");

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

        // Add speaker ID
        formContent.Add(new StringContent(speakerId), "speaker_id");

        // Add base model ID
        formContent.Add(new StringContent(_baseModelId), "base_model");

        var response = await _httpClient.PostAsync(
            $"https://api-inference.huggingface.co/models/{modelName}/speaker-embeddings",
            formContent);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to upload speaker embeddings: {errorContent}");
        }

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error uploading speaker embeddings for model: {ModelName}", modelName);
        throw;
    }
}

    public async Task<byte[]> SynthesizeSpeechAsync(string modelName, string text, string speakerId)
    {
        try
        {
            // Create a proper nested object structure
            var requestObj = new
            {
                inputs = new
                {
                    text = text,
                    speaker_id = speakerId,
                    language = "en" // Can be parameterized
                }
            };

            var json = JsonSerializer.Serialize(requestObj);
            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"https://api-inference.huggingface.co/models/{modelName}",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to synthesize speech: {errorContent}");
            }

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
            var response = await _httpClient.DeleteAsync($"https://huggingface.co/api/repos/delete?repo={modelName}");

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