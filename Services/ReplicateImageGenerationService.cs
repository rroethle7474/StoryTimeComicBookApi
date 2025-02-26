// Services/ReplicateImageGenerationService.cs
using StoryTimeComicBookApi.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace StoryTimeComicBookApi.Services
{
    public class ReplicateImageGenerationService : IImageGenerationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReplicateImageGenerationService> _logger;

        public ReplicateImageGenerationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ReplicateImageGenerationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GenerateComicStyleImage(string imagePath, string userDescription, string outputFolder, string comicBookId)
        {
            var replicateApiKey = _configuration["AI:Replicate:ImageApiKey"];
            if (string.IsNullOrEmpty(replicateApiKey))
            {
                throw new InvalidOperationException("Replicate API key is not configured");
            }

            // Prepare the image data
            string imageUrl;
            // Check if the image path is a URL or a local file
            if (imagePath.StartsWith("http"))
            {
                imageUrl = imagePath;
            }
            else
            {
                // If it's a local path, we need to construct the full URL
                // This would be the absolute path for the image
                var fullImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));

                // Check if file exists
                if (!System.IO.File.Exists(fullImagePath))
                {
                    throw new FileNotFoundException($"Image file not found at {fullImagePath}");
                }

                // For Replicate, we need a publicly accessible URL, but for now we can use base64 encoding
                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(fullImagePath);
                string base64Image = Convert.ToBase64String(imageBytes);
                imageUrl = $"data:image/png;base64,{base64Image}";
            }

            // Create Replicate API request
            var apiUrl = "https://api.replicate.com/v1/predictions";

            var payload = new
            {
                version = "ac732df83cea7fff18b8472768c88ad041fa750ff7682a21affe81863cbe77e4", // Stable diffusion model version
                input = new
                {
                    prompt = $"{userDescription}, comic book style, vibrant colors, detailed, professional illustration",
                    image = imageUrl,
                    prompt_strength = 0.8, // Balance between original image and comic styling
                    num_inference_steps = 50,
                    guidance_scale = 7.5,
                }
            };

            // Send request to Replicate API
            var client = _httpClientFactory.CreateClient("ReplicateApi");
            client.DefaultRequestHeaders.Add("Authorization", $"Token {replicateApiKey}");

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);

            if (responseData == null || !responseData.ContainsKey("id"))
            {
                throw new Exception("Failed to initiate image generation");
            }

            // Get prediction ID
            var predictionId = responseData["id"].ToString();

            // Poll for completion
            var statusUrl = $"{apiUrl}/{predictionId}";
            bool completed = false;
            int maxRetries = 30;
            int retryCount = 0;
            string outputUrl = null;

            while (!completed && retryCount < maxRetries)
            {
                await Task.Delay(2000); // Wait 2 seconds between polls

                var statusResponse = await client.GetAsync(statusUrl);
                statusResponse.EnsureSuccessStatusCode();

                var statusJson = await statusResponse.Content.ReadAsStringAsync();
                var statusData = JsonSerializer.Deserialize<Dictionary<string, object>>(statusJson);

                if (statusData != null && statusData.ContainsKey("status"))
                {
                    var status = statusData["status"].ToString();

                    if (status == "succeeded")
                    {
                        completed = true;
                        var outputData = statusData["output"];
                        var outputDict = JsonSerializer.Deserialize<Dictionary<string, object>>(outputData.ToString());
                        outputUrl = outputDict?["image"].ToString();
                    }
                    else if (status == "failed")
                    {
                        throw new Exception("Image generation failed");
                    }
                }

                retryCount++;
            }

            if (string.IsNullOrEmpty(outputUrl))
            {
                throw new Exception("Failed to generate image or timed out");
            }

            // Download the generated image
            var fileName = $"{Guid.NewGuid()}.png";
            var localFilePath = Path.Combine(outputFolder, fileName);

            var imageClient = _httpClientFactory.CreateClient();
            var imageData = await imageClient.GetByteArrayAsync(outputUrl);
            await System.IO.File.WriteAllBytesAsync(localFilePath, imageData);

            // Return web-accessible path
            return $"/comics/{comicBookId}/{fileName}";
        }
    }
}