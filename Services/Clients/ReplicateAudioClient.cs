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
        _apiKey = configuration["AI:Replicate:AudioApiKey"] ??
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