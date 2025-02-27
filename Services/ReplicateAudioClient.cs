using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System.IO.Compression;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public class ReplicateAudioClient
    {
        private readonly ILogger<ReplicateAudioClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _username;
        private readonly IConfiguration _configuration;

        // Constants for the StyleTTS2 model
        private readonly string STYLETTS2_MODEL = "lucataco/styletts-2:684bc3855b37866c0c65add2ff38c3ad55249415b21ca7a388f5634f66ae052a";
        private string CUSTOM_MODEL => _configuration.GetValue<string>("AI:Replicate:CustomModelName", "voice-model-01");

        public ReplicateAudioClient(ILogger<ReplicateAudioClient> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
            _username = configuration.GetValue<string>("AI:Replicate:Username");
        }

        /// <summary>
        /// Prepares a ZIP file containing voice samples and returns it as a data URI
        /// </summary>
        /// <param name="audioFilePaths">List of paths to audio files</param>
        /// <returns>Data URI of the ZIP file</returns>
        public async Task<string> PrepareVoiceSamplesAsZipAsync(List<string> audioFilePaths)
        {
            _logger.LogInformation("Preparing ZIP file with {Count} voice samples", audioFilePaths.Count);
            
            // Create a temporary file for the ZIP
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"voice_samples_{Guid.NewGuid()}.zip");
            
            try
            {
                // Create a new ZIP file
                using (var zipArchive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
                {
                    foreach (var audioFilePath in audioFilePaths)
                    {
                        if (File.Exists(audioFilePath))
                        {
                            // Add the file to the ZIP archive with just the filename (no path)
                            string fileName = Path.GetFileName(audioFilePath);
                            zipArchive.CreateEntryFromFile(audioFilePath, fileName);
                            _logger.LogDebug("Added file to ZIP: {FileName}", fileName);
                        }
                        else
                        {
                            _logger.LogWarning("Audio file not found: {FilePath}", audioFilePath);
                        }
                    }
                }
                
                // Read the ZIP file as bytes
                byte[] zipBytes = await File.ReadAllBytesAsync(tempZipPath);
                
                // Convert to base64 data URI
                string base64Data = Convert.ToBase64String(zipBytes);
                string dataUri = $"data:application/zip;base64,{base64Data}";
                
                _logger.LogInformation("ZIP file created successfully: {Size} bytes", zipBytes.Length);
                
                return dataUri;
            }
            finally
            {
                // Clean up the temporary file
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                    _logger.LogDebug("Temporary ZIP file deleted: {Path}", tempZipPath);
                }
            }
        }

        /// <summary>
        /// Trains a custom StyleTTS2 model using voice samples
        /// </summary>
        /// <param name="voiceSampleUrls">List of URLs to voice samples</param>
        /// <param name="customModelName">Name for the custom model</param>
        /// <param name="audioFilePaths">Optional list of paths to audio files for ZIP approach</param>
        /// <returns>The version ID of the trained model</returns>
        public async Task<string> TrainCustomModelAsync(List<string> voiceSampleUrls, string customModelName, List<string> audioFilePaths = null)
        {
            _logger.LogInformation("Training custom StyleTTS2 model: {ModelName}", customModelName);
            
            // Determine if we should use the ZIP approach (if audio file paths are provided)
            bool useZipApproach = audioFilePaths != null && audioFilePaths.Count > 0;
            
            // Prepare the request payload
            var payload = new JObject();
            
            if (useZipApproach)
            {
                _logger.LogInformation("Using ZIP approach for training with {Count} audio files", audioFilePaths.Count);
                
                // Prepare the ZIP file and get the data URI
                string zipDataUri = await PrepareVoiceSamplesAsZipAsync(audioFilePaths);
                
                // Set up the payload with the ZIP data URI
                payload = new JObject
                {
                    ["destination"] = $"{_username}/{customModelName}",
                    ["input"] = new JObject
                    {
                        ["voice_samples"] = zipDataUri
                    }
                };
            }
            else
            {
                _logger.LogInformation("Using URL approach for training with {Count} voice sample URLs", voiceSampleUrls.Count);
                
                // Set up the payload with voice sample URLs
                payload = new JObject
                {
                    ["destination"] = $"{_username}/{customModelName}",
                    ["input"] = new JObject
                    {
                        ["voice_samples"] = new JArray(voiceSampleUrls)
                    }
                };
            }
            
            // Make the API request to train the model
            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(
                $"https://api.replicate.com/v1/models/{STYLETTS2_MODEL}/versions/latest/trainings",
                content);
            
            // Check if the request was successful
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to train custom model. Status: {Status}, Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new Exception($"Failed to train custom model: {response.StatusCode} - {errorContent}");
            }
            
            // Parse the response to get the training ID
            var responseContent = await response.Content.ReadAsStringAsync();
            var trainingData = JObject.Parse(responseContent);
            var trainingId = trainingData["id"]?.ToString();
            
            if (string.IsNullOrEmpty(trainingId))
            {
                _logger.LogError("Failed to get training ID from response: {Response}", responseContent);
                throw new Exception("Failed to get training ID from response");
            }
            
            _logger.LogInformation("Training initiated. Training ID: {TrainingId}", trainingId);
            
            // Poll for training completion
            bool isCompleted = false;
            string modelVersion = null;
            int maxAttempts = 60; // 30 minutes (30 seconds * 60)
            int attempt = 0;
            
            while (!isCompleted && attempt < maxAttempts)
            {
                attempt++;
                
                // Wait before checking status
                await Task.Delay(TimeSpan.FromSeconds(30));
                
                // Check training status
                var statusResponse = await _httpClient.GetAsync($"https://api.replicate.com/v1/trainings/{trainingId}");
                
                if (!statusResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to check training status. Status: {Status}", statusResponse.StatusCode);
                    continue;
                }
                
                var statusContent = await statusResponse.Content.ReadAsStringAsync();
                var statusData = JObject.Parse(statusContent);
                var status = statusData["status"]?.ToString();
                
                _logger.LogInformation("Training status: {Status} (Attempt {Attempt}/{MaxAttempts})", 
                    status, attempt, maxAttempts);
                
                if (status == "succeeded")
                {
                    isCompleted = true;
                    modelVersion = statusData["version"]?.ToString();
                    
                    if (string.IsNullOrEmpty(modelVersion))
                    {
                        _logger.LogWarning("Training succeeded but model version is missing: {Response}", statusContent);
                    }
                    else
                    {
                        _logger.LogInformation("Training completed successfully. Model version: {Version}", modelVersion);
                    }
                }
                else if (status == "failed" || status == "canceled")
                {
                    var error = statusData["error"]?.ToString();
                    _logger.LogError("Training failed: {Error}", error);
                    throw new Exception($"Training failed: {error}");
                }
            }
            
            if (!isCompleted)
            {
                _logger.LogError("Training timed out after {Attempts} attempts", attempt);
                throw new Exception("Training timed out");
            }
            
            return modelVersion;
        }

        /// <summary>
        /// Prepares voice samples by uploading them to Replicate
        /// </summary>
        /// <param name="audioFilePaths">List of paths to audio files</param>
        /// <returns>List of URLs to the uploaded voice samples</returns>
        public async Task<List<string>> PrepareVoiceSamplesAsync(List<string> audioFilePaths)
        {
            _logger.LogInformation("Preparing voice samples: {Count} files", audioFilePaths.Count);
            
            var voiceSampleUrls = new List<string>();
            
            foreach (var audioFilePath in audioFilePaths)
            {
                try
                {
                    if (File.Exists(audioFilePath))
                    {
                        var url = await UploadAudioFileAsync(audioFilePath);
                        voiceSampleUrls.Add(url);
                        _logger.LogInformation("Uploaded voice sample: {FilePath} -> {Url}", audioFilePath, url);
                    }
                    else
                    {
                        _logger.LogWarning("Audio file not found: {FilePath}", audioFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading voice sample: {FilePath}", audioFilePath);
                    throw;
                }
            }
            
            return voiceSampleUrls;
        }
        
        /// <summary>
        /// Uploads an audio file to Replicate
        /// </summary>
        /// <param name="audioFilePath">Path to the audio file</param>
        /// <returns>URL to the uploaded audio file</returns>
        public async Task<string> UploadAudioFileAsync(string audioFilePath)
        {
            _logger.LogInformation("Uploading audio file: {FilePath}", audioFilePath);
            
            try
            {
                // Read the audio file as bytes
                byte[] audioBytes = await File.ReadAllBytesAsync(audioFilePath);
                
                // Convert to base64 data URI
                string base64Data = Convert.ToBase64String(audioBytes);
                string mimeType = Path.GetExtension(audioFilePath).ToLower() switch
                {
                    ".wav" => "audio/wav",
                    ".mp3" => "audio/mpeg",
                    ".ogg" => "audio/ogg",
                    ".flac" => "audio/flac",
                    _ => "application/octet-stream"
                };
                
                string dataUri = $"data:{mimeType};base64,{base64Data}";
                
                // Create the upload request
                var payload = new JObject
                {
                    ["input"] = new JObject
                    {
                        ["audio"] = dataUri
                    }
                };
                
                // Make the API request to upload the file
                var response = await _httpClient.PostAsync(
                    $"https://api.replicate.com/v1/models/{STYLETTS2_MODEL}/predictions",
                    new StringContent(payload.ToString(), Encoding.UTF8, "application/json"));
                
                // Check if the request was successful
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to upload audio file. Status: {Status}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    throw new Exception($"Failed to upload audio file: {response.StatusCode} - {errorContent}");
                }
                
                // Parse the response to get the prediction ID
                var responseContent = await response.Content.ReadAsStringAsync();
                var predictionData = JObject.Parse(responseContent);
                var predictionId = predictionData["id"]?.ToString();
                
                if (string.IsNullOrEmpty(predictionId))
                {
                    _logger.LogError("Failed to get prediction ID from response: {Response}", responseContent);
                    throw new Exception("Failed to get prediction ID from response");
                }
                
                _logger.LogInformation("Audio file uploaded. Prediction ID: {PredictionId}", predictionId);
                
                // Poll for prediction completion
                bool isCompleted = false;
                string audioUrl = null;
                int maxAttempts = 30; // 5 minutes (10 seconds * 30)
                int attempt = 0;
                
                while (!isCompleted && attempt < maxAttempts)
                {
                    attempt++;
                    
                    // Wait before checking status
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    
                    // Check prediction status
                    var statusResponse = await _httpClient.GetAsync($"https://api.replicate.com/v1/predictions/{predictionId}");
                    
                    if (!statusResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to check prediction status. Status: {Status}", statusResponse.StatusCode);
                        continue;
                    }
                    
                    var statusContent = await statusResponse.Content.ReadAsStringAsync();
                    var statusData = JObject.Parse(statusContent);
                    var status = statusData["status"]?.ToString();
                    
                    _logger.LogInformation("Prediction status: {Status} (Attempt {Attempt}/{MaxAttempts})", 
                        status, attempt, maxAttempts);
                    
                    if (status == "succeeded")
                    {
                        isCompleted = true;
                        
                        // Get the audio URL from the output
                        var output = statusData["output"];
                        if (output != null && output["audio"] != null)
                        {
                            audioUrl = output["audio"].ToString();
                            _logger.LogInformation("Audio file uploaded successfully. URL: {Url}", audioUrl);
                        }
                        else
                        {
                            _logger.LogWarning("Prediction succeeded but audio URL is missing: {Response}", statusContent);
                            throw new Exception("Prediction succeeded but audio URL is missing");
                        }
                    }
                    else if (status == "failed" || status == "canceled")
                    {
                        var error = statusData["error"]?.ToString();
                        _logger.LogError("Prediction failed: {Error}", error);
                        throw new Exception($"Prediction failed: {error}");
                    }
                }
                
                if (!isCompleted)
                {
                    _logger.LogError("Prediction timed out after {Attempts} attempts", attempt);
                    throw new Exception("Prediction timed out");
                }
                
                return audioUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading audio file: {FilePath}", audioFilePath);
                throw;
            }
        }

        /// <summary>
        /// Creates a prediction with the StyleTTS2 model
        /// </summary>
        /// <param name="text">Text to synthesize</param>
        /// <param name="voiceSampleUrls">List of URLs to voice samples</param>
        /// <param name="useCustomModel">Whether to use the custom model</param>
        /// <returns>The prediction ID</returns>
        public async Task<string> CreatePredictionAsync(string text, List<string> voiceSampleUrls, bool useCustomModel = false)
        {
            _logger.LogInformation("Creating prediction with text: {Text}", text);
            
            // Determine which model to use
            string modelId = useCustomModel ? $"{_username}/{CUSTOM_MODEL}" : STYLETTS2_MODEL;
            
            _logger.LogInformation("Using model: {ModelId}", modelId);
            
            // Create the prediction request
            var payload = new JObject
            {
                ["version"] = useCustomModel ? null : "684bc3855b37866c0c65add2ff38c3ad55249415b21ca7a388f5634f66ae052a",
                ["input"] = new JObject
                {
                    ["text"] = text,
                    ["voice_samples"] = new JArray(voiceSampleUrls)
                }
            };
            
            // Make the API request to create the prediction
            var response = await _httpClient.PostAsync(
                $"https://api.replicate.com/v1/predictions",
                new StringContent(payload.ToString(), Encoding.UTF8, "application/json"));
            
            // Check if the request was successful
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create prediction. Status: {Status}, Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new Exception($"Failed to create prediction: {response.StatusCode} - {errorContent}");
            }
            
            // Parse the response to get the prediction ID
            var responseContent = await response.Content.ReadAsStringAsync();
            var predictionData = JObject.Parse(responseContent);
            var predictionId = predictionData["id"]?.ToString();
            
            if (string.IsNullOrEmpty(predictionId))
            {
                _logger.LogError("Failed to get prediction ID from response: {Response}", responseContent);
                throw new Exception("Failed to get prediction ID from response");
            }
            
            _logger.LogInformation("Prediction created. Prediction ID: {PredictionId}", predictionId);
            
            return predictionId;
        }
        
        /// <summary>
        /// Gets the result of a prediction
        /// </summary>
        /// <param name="predictionId">The prediction ID</param>
        /// <returns>The audio data as bytes</returns>
        public async Task<byte[]> GetPredictionResultAsync(string predictionId)
        {
            _logger.LogInformation("Getting prediction result for ID: {PredictionId}", predictionId);
            
            // Poll for prediction completion
            bool isCompleted = false;
            string audioUrl = null;
            int maxAttempts = 30; // 5 minutes (10 seconds * 30)
            int attempt = 0;
            
            while (!isCompleted && attempt < maxAttempts)
            {
                attempt++;
                
                // Wait before checking status
                await Task.Delay(TimeSpan.FromSeconds(10));
                
                // Check prediction status
                var statusResponse = await _httpClient.GetAsync($"https://api.replicate.com/v1/predictions/{predictionId}");
                
                if (!statusResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to check prediction status. Status: {Status}", statusResponse.StatusCode);
                    continue;
                }
                
                var statusContent = await statusResponse.Content.ReadAsStringAsync();
                var statusData = JObject.Parse(statusContent);
                var status = statusData["status"]?.ToString();
                
                _logger.LogInformation("Prediction status: {Status} (Attempt {Attempt}/{MaxAttempts})", 
                    status, attempt, maxAttempts);
                
                if (status == "succeeded")
                {
                    isCompleted = true;
                    
                    // Get the audio URL from the output
                    var output = statusData["output"];
                    if (output != null && output["audio"] != null)
                    {
                        audioUrl = output["audio"].ToString();
                        _logger.LogInformation("Prediction completed successfully. Audio URL: {Url}", audioUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Prediction succeeded but audio URL is missing: {Response}", statusContent);
                        throw new Exception("Prediction succeeded but audio URL is missing");
                    }
                }
                else if (status == "failed" || status == "canceled")
                {
                    var error = statusData["error"]?.ToString();
                    _logger.LogError("Prediction failed: {Error}", error);
                    throw new Exception($"Prediction failed: {error}");
                }
            }
            
            if (!isCompleted)
            {
                _logger.LogError("Prediction timed out after {Attempts} attempts", attempt);
                throw new Exception("Prediction timed out");
            }
            
            // Download the audio file
            _logger.LogInformation("Downloading audio file from URL: {Url}", audioUrl);
            
            var audioResponse = await _httpClient.GetAsync(audioUrl);
            
            if (!audioResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to download audio file. Status: {Status}", audioResponse.StatusCode);
                throw new Exception($"Failed to download audio file: {audioResponse.StatusCode}");
            }
            
            var audioData = await audioResponse.Content.ReadAsByteArrayAsync();
            
            _logger.LogInformation("Audio file downloaded successfully. Size: {Size} bytes", audioData.Length);
            
            return audioData;
        }
    }
} 