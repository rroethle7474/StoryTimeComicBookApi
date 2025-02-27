using Microsoft.EntityFrameworkCore;
using StoryTimeComicBookApi.Data;
using StoryTimeComicBookApi.Data.Entities;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Clients;
using StoryTimeComicBookApi.Services.Interfaces;

namespace StoryTimeComicBookApi.Services;

public class VoiceMimickingService : IVoiceMimickingService
{
    private readonly VoiceMimicDataContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VoiceMimickingService> _logger;
    private readonly IAudioStorageService _audioStorage;
    private readonly IVoiceModelTrainer _modelTrainer;
    private readonly HuggingFaceClient _huggingFaceClient;

    public VoiceMimickingService(
        VoiceMimicDataContext context,
        IConfiguration configuration,
        ILogger<VoiceMimickingService> logger,
        IAudioStorageService audioStorage,
        IVoiceModelTrainer modelTrainer,
        HuggingFaceClient huggingFaceClient)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _audioStorage = audioStorage;
        _modelTrainer = modelTrainer;
        _huggingFaceClient = huggingFaceClient;
    }

    public async Task<CreateVoiceModelResponse> CreateVoiceModelAsync(CreateVoiceModelRequest request)
    {
        try
        {
            var duplicateModel = await _context.VoiceModels
                .Where(v => v.VoiceModelName.Trim().ToLower() == request.VoiceModelName.Trim().ToLower())
                .FirstOrDefaultAsync();

            if (duplicateModel != null)
            {
                throw new InvalidOperationException("A voice model with the same name already exists");
            }

            var voiceModel = new VoiceModel
            {
                VoiceModelName = request.VoiceModelName,
                VoiceModelDescription = request.VoiceModelDescription,
                Status = "pending",
                TrainingDate = DateTime.UtcNow
            };

            _context.VoiceModels.Add(voiceModel);
            await _context.SaveChangesAsync();
            
            return new CreateVoiceModelResponse
            {
                VoiceModelId = voiceModel.VoiceModelId.ToString(),
                VoiceModelName = voiceModel.VoiceModelName,
                VoiceModelDescription = voiceModel.VoiceModelDescription
            };
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error creating voice model");
            throw;
        }
    }

    public async Task<IEnumerable<VoiceModelListResponse>> GetIncompleteVoiceModelsAsync()
    {
        var incompleteVoiceModels = await _context.VoiceModels
            .Where(vm => !vm.IsCompleted)
            .Select(vm => new VoiceModelListResponse
            {
                VoiceModelId = vm.VoiceModelId.ToString(),
                VoiceModelName = vm.VoiceModelName,
                VoiceModelDescription = vm.VoiceModelDescription,
                IsCompleted = vm.IsCompleted,
                TrainingDate = vm.TrainingDate
            })
            .ToListAsync();

        return incompleteVoiceModels;
    }

    public async Task<VoiceModelUpdateResponse> UpdateVoiceModelAsync(string voiceModelId, VoiceModelUpdateRequest request)
    {
        var voiceModel = await _context.VoiceModels.FindAsync(Guid.Parse(voiceModelId));
        if (voiceModel == null)
        {
            throw new KeyNotFoundException("Voice model not found");
        }

        if (!string.IsNullOrEmpty(request.VoiceModelName))
        {
            voiceModel.VoiceModelName = request.VoiceModelName;
        }

        if (!string.IsNullOrEmpty(request.VoiceModelDescription))
        {
            voiceModel.VoiceModelDescription = request.VoiceModelDescription;
        }

        if (request.IsCompleted.HasValue)
        {
            voiceModel.IsCompleted = request.IsCompleted.Value;
            voiceModel.Status = request.IsCompleted.Value ? "completed" : "pending";
        }

        await _context.SaveChangesAsync();

        return new VoiceModelUpdateResponse
        {
            VoiceModelId = voiceModel.VoiceModelId.ToString(),
            VoiceModelName = voiceModel.VoiceModelName,
            VoiceModelDescription = voiceModel.VoiceModelDescription,
            IsCompleted = voiceModel.IsCompleted,
            TrainingDate = voiceModel.TrainingDate
        };
    }

    public async Task<AudioSnippetUploadResponse> UploadAudioSnippetAsync(AudioSnippetUploadRequest request)
    {
        try
        {
            var filePath = await _audioStorage.SaveAudioFileAsync(request.AudioFile);

            var audioSnippet = new AudioSnippet
            {
                AudioFilePath = filePath,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AudioSnippets.Add(audioSnippet);
            await _context.SaveChangesAsync();

            return new AudioSnippetUploadResponse
            {
                Message = "Audio snippet uploaded successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading audio snippet");
            throw;
        }
    }

    public async Task<TrainModelResponse> TrainModelAsync(TrainModelRequest request)
    {
        try
        {
            var audioSnippets = await _context.AudioSnippets
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.AudioFilePath)
                .ToListAsync();

            if (!audioSnippets.Any())
            {
                throw new InvalidOperationException("No audio snippets available for training");
            }

            var voiceModel = new VoiceModel
            {
                VoiceModelName = $"Model_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                Status = "training",
                TrainingDate = DateTime.UtcNow
            };

            _context.VoiceModels.Add(voiceModel);
            await _context.SaveChangesAsync();

            // Start the training process asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    var huggingfaceModelName = await _modelTrainer.TrainModelAsync(audioSnippets, voiceModel.VoiceModelId, voiceModel.VoiceModelName);
                    
                    // Update model status after successful training
                    voiceModel.Status = "completed";
                    voiceModel.IsCompleted = true;
                    voiceModel.HuggingFaceModelName = huggingfaceModelName;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during model training for model {ModelId}", voiceModel.VoiceModelId);
                    voiceModel.Status = "failed";
                    await _context.SaveChangesAsync();
                }
            });

            return new TrainModelResponse
            {
                Message = "Model training initiated."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating model training");
            throw;
        }
    }

    public async Task<SynthesizeSpeechResponse> SynthesizeSpeechAsync(SynthesizeSpeechRequest request)
    {
        try
        {
            var latestModel = await _context.VoiceModels
                .Where(v => v.Status == "completed")
                .OrderByDescending(v => v.TrainingDate)
                .FirstOrDefaultAsync();

            if (latestModel == null)
            {
                throw new InvalidOperationException("No trained voice model available");
            }

            // This now calls the updated SynthesizeSpeechAsync that uses HuggingFace
            var audioUrl = await _modelTrainer.SynthesizeSpeechAsync(
                request.TextToSynthesize,
                latestModel.VoiceModelId);

            return new SynthesizeSpeechResponse
            {
                AudioUrl = audioUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing speech");
            throw;
        }
    }

    public async Task<SynthesizeSpeechResponse> SynthesizeSpeechForModelAsync(Guid modelId, string text)
    {
        try
        {
            var voiceModel = await _context.VoiceModels
                .FirstOrDefaultAsync(v => v.VoiceModelId == modelId);

            if (voiceModel == null)
            {
                throw new KeyNotFoundException($"Voice model with ID {modelId} not found");
            }

            if (voiceModel.Status != "completed")
            {
                throw new InvalidOperationException($"Voice model {modelId} is not ready (status: {voiceModel.Status})");
            }

            // Call the trainer to synthesize speech
            var audioUrl = await _modelTrainer.SynthesizeSpeechAsync(text, modelId);

            return new SynthesizeSpeechResponse
            {
                AudioUrl = audioUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing speech for model {ModelId}", modelId);
            throw;
        }
    }

    public StartRecordingResponse StartRecording()
    {
        try
        {
            var sessionId = Guid.NewGuid().ToString();
            return new StartRecordingResponse
            {
                RecordingSessionId = sessionId,
                Message = "Recording session started."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting recording session");
            throw;
        }
    }

    public async Task<AudioSnippetUploadResponse> AddAudioSnippetToModelAsync(
    string voiceModelId,
    AudioSnippetUploadRequest request)
    {
        try
        {
            // Save the audio file
            var filePath = await _audioStorage.SaveAudioFileAsync(request.AudioFile);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create audio snippet
                var audioSnippet = new AudioSnippet
                {
                    AudioFilePath = filePath,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.AudioSnippets.Add(audioSnippet);
                await _context.SaveChangesAsync();

                // Create association with voice model and step
                var voiceModelAudioSnippet = new VoiceModelAudioSnippet
                {
                    VoiceModelId = Guid.Parse(voiceModelId),
                    AudioSnippetId = audioSnippet.AudioSnippetId,
                    StepId = Guid.Parse(request.StepId!),
                    AddedAt = DateTime.UtcNow
                };

                _context.VoiceModelAudioSnippets.Add(voiceModelAudioSnippet);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AudioSnippetUploadResponse
                {
                    Message = "Audio snippet uploaded and associated with voice model successfully.",
                    AudioSnippetId = audioSnippet.AudioSnippetId.ToString()
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading audio snippet for voice model {VoiceModelId}", voiceModelId);
            throw;
        }
    }

    public async Task<bool> DeleteAudioSnippetAsync(string audioSnippetId)
    {
        try
        {
            var id = Guid.Parse(audioSnippetId);
            var audioSnippet = await _context.AudioSnippets
                .Include(a => a.VoiceModels)
                .FirstOrDefaultAsync(a => a.AudioSnippetId == id);

            if (audioSnippet == null)
            {
                return false;
            }

            // Construct the full path to the audio file
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(webRootPath, audioSnippet.AudioFilePath.TrimStart('/'));

            // Delete the physical file
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Remove from database (cascade delete will handle associations)
            _context.AudioSnippets.Remove(audioSnippet);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting audio snippet {AudioSnippetId}", audioSnippetId);
            throw;
        }
    }

    public async Task<IEnumerable<AudioSnippetResponse>> GetAudioSnippetsForModelAsync(string voiceModelId)
    {
        try
        {
            var id = Guid.Parse(voiceModelId);
            var snippets = await _context.VoiceModelAudioSnippets
                .Include(v => v.AudioSnippet)
                .Where(v => v.VoiceModelId == id)
                .Select(v => new AudioSnippetResponse
                {
                    AudioSnippetId = v.AudioSnippet.AudioSnippetId.ToString(),
                    AudioFilePath = v.AudioSnippet.AudioFilePath,
                    AddedAt = v.AddedAt
                })
                .ToListAsync();

            return snippets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audio snippets for voice model {VoiceModelId}", voiceModelId);
            throw;
        }
    }

    public async Task<TrainModelResponse> InitiateModelTrainingAsync(string voiceModelId)
    {
        try
        {
            var id = Guid.Parse(voiceModelId);
            var voiceModel = await _context.VoiceModels
                .Include(v => v.AudioSnippets)
                .ThenInclude(a => a.AudioSnippet)
                .FirstOrDefaultAsync(v => v.VoiceModelId == id);

            if (voiceModel == null)
            {
                throw new KeyNotFoundException($"Voice model {voiceModelId} not found");
            }

            if (!voiceModel.AudioSnippets.Any())
            {
                throw new InvalidOperationException("No audio snippets available for training");
            }

            // Update voice model status
            voiceModel.Status = "training";

            await _context.SaveChangesAsync();

            // Start training process asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    var audioFilePaths = voiceModel.AudioSnippets
                        .Select(a => a.AudioSnippet.AudioFilePath)
                        .ToList();

                    
                    var huggingFaceModelName = await _modelTrainer.TrainModelAsync(audioFilePaths, voiceModel.VoiceModelId, voiceModel.VoiceModelName);

                    // Update status after training
                    voiceModel.Status = "completed";
                    voiceModel.HuggingFaceModelName = huggingFaceModelName;
                    voiceModel.IsCompleted = true;

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during model training for voice model {VoiceModelId}", voiceModelId);
                    voiceModel.Status = "failed";
                    await _context.SaveChangesAsync();
                }
            });

            return new TrainModelResponse
            {
                Message = "Model training initiated successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating training for voice model {VoiceModelId}", voiceModelId);
            throw;
        }
    }

    public async Task<VoiceModelStepsProgress> GetVoiceModelProgressAsync(string voiceModelId)
    {
        try
        {
            var id = Guid.Parse(voiceModelId);

            // Get all steps and their recordings for this voice model
            var steps = await _context.VoiceRecordingSteps
                .OrderBy(s => s.StepNumber)
                .Select(s => new
                {
                    Step = s,
                    Recording = _context.VoiceModelAudioSnippets
                        .Include(v => v.AudioSnippet)
                        .FirstOrDefault(v => v.VoiceModelId == id && v.StepId == s.StepId)
                })
                .ToListAsync();

            var stepsWithRecordings = steps.Select(s => new StepWithRecordingResponse
            {
                StepId = s.Step.StepId.ToString(),
                StepNumber = s.Step.StepNumber,
                TranscriptText = s.Step.TranscriptText,
                Recording = s.Recording == null ? null : new AudioRecordingDetails
                {
                    AudioSnippetId = s.Recording.AudioSnippetId.ToString(),
                    AudioFilePath = s.Recording.AudioSnippet.AudioFilePath,
                    RecordedAt = s.Recording.AddedAt
                }
            }).ToList();

            return new VoiceModelStepsProgress
            {
                VoiceModelId = voiceModelId,
                TotalSteps = steps.Count,
                CompletedSteps = steps.Count(s => s.Recording != null),
                Steps = stepsWithRecordings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting voice model progress for {VoiceModelId}", voiceModelId);
            throw;
        }
    }

    public async Task<IEnumerable<StepResponse>> GetAllStepsAsync()
    {
        try
        {
            return await _context.VoiceRecordingSteps
                .OrderBy(s => s.StepNumber)
                .Select(s => new StepResponse
                {
                    StepId = s.StepId.ToString(),
                    StepNumber = s.StepNumber,
                    TranscriptText = s.TranscriptText,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all recording steps");
            throw;
        }
    }

    public async Task<AudioSnippetUploadResponse> AddAudioSnippetForStepAsync(
        string voiceModelId,
        string stepId,
        AudioSnippetUploadRequest request)
    {
        try
        {
            var modelId = Guid.Parse(voiceModelId);
            var parsedStepId = Guid.Parse(stepId);

            // Check if step exists
            var stepExists = await _context.VoiceRecordingSteps
                .AnyAsync(s => s.StepId == parsedStepId);

            if (!stepExists)
            {
                throw new KeyNotFoundException($"Step with ID {stepId} not found");
            }

            // Check if there's an existing recording for this step
            var existingRecording = await _context.VoiceModelAudioSnippets
                .FirstOrDefaultAsync(v => v.VoiceModelId == modelId && v.StepId == parsedStepId);

            if (existingRecording != null)
            {
                // Delete existing recording
                var existingAudioSnippet = await _context.AudioSnippets
                    .FindAsync(existingRecording.AudioSnippetId);

                if (existingAudioSnippet != null)
                {
                    // Delete physical file
                    if (File.Exists(existingAudioSnippet.AudioFilePath))
                    {
                        File.Delete(existingAudioSnippet.AudioFilePath);
                    }

                    _context.AudioSnippets.Remove(existingAudioSnippet);
                    await _context.SaveChangesAsync();
                }
            }

            // Save new audio file
            var filePath = await _audioStorage.SaveAudioFileAsync(request.AudioFile);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create audio snippet
                var audioSnippet = new AudioSnippet
                {
                    AudioFilePath = filePath,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.AudioSnippets.Add(audioSnippet);
                await _context.SaveChangesAsync();

                // Create association with voice model and step
                var voiceModelAudioSnippet = new VoiceModelAudioSnippet
                {
                    VoiceModelId = modelId,
                    AudioSnippetId = audioSnippet.AudioSnippetId,
                    StepId = parsedStepId,
                    AddedAt = DateTime.UtcNow
                };

                _context.VoiceModelAudioSnippets.Add(voiceModelAudioSnippet);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AudioSnippetUploadResponse
                {
                    Message = "Audio snippet uploaded and associated with step successfully."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding audio snippet for step {StepId} in voice model {VoiceModelId}",
                stepId, voiceModelId);
            throw;
        }
    }

    public async Task<IEnumerable<StepWithRecordingResponse>> GetStepRecordingsForModelAsync(string voiceModelId)
    {
        try
        {
            var id = Guid.Parse(voiceModelId);

            var recordings = await _context.VoiceRecordingSteps
                .OrderBy(s => s.StepNumber)
                .Select(s => new StepWithRecordingResponse
                {
                    StepId = s.StepId.ToString(),
                    StepNumber = s.StepNumber,
                    TranscriptText = s.TranscriptText,
                    Recording = s.VoiceModelAudioSnippets
                        .Where(v => v.VoiceModelId == id)
                        .Select(v => new AudioRecordingDetails
                        {
                            AudioSnippetId = v.AudioSnippetId.ToString(),
                            AudioFilePath = v.AudioSnippet.AudioFilePath,
                            RecordedAt = v.AddedAt
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return recordings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting step recordings for voice model {VoiceModelId}", voiceModelId);
            throw;
        }
    }

    public async Task<List<HuggingFaceModelResponse>> GetHuggingFaceModelsAsync()
    {
        try
        {
            // Get username from configuration
            var username = _configuration["AI:HuggingFace:Username"];
            if (string.IsNullOrEmpty(username))
            {
                throw new InvalidOperationException("HuggingFace username not configured");
            }

            // Get models from HuggingFace with your voice model prefix
            var models = await _huggingFaceClient.GetUserModelsAsync(username, "voice-model-");

            // Map to response objects
            var response = models.Select(m => new HuggingFaceModelResponse
            {
                ModelId = m.ModelId,
                Name = m.Name,
                Description = m.Description,
                LastModified = m.LastModified
            }).ToList();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving HuggingFace models");
            throw;
        }
    }

    public async Task<bool> DeleteHuggingFaceModelAsync(string modelName)
    {
        try
        {
            // First check if this model is in use by any of our voice models
            var voiceModel = await _context.VoiceModels
                .FirstOrDefaultAsync(v => v.HuggingFaceModelName == modelName);

            if (voiceModel != null)
            {
                throw new InvalidOperationException(
                    $"Cannot delete model {modelName} as it is associated with voice model {voiceModel.VoiceModelId}");
            }

            // Delete from HuggingFace
            return await _huggingFaceClient.DeleteModelAsync(modelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting HuggingFace model: {ModelName}", modelName);
            throw;
        }
    }
} 