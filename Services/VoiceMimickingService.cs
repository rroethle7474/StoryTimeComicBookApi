using Microsoft.EntityFrameworkCore;
using StoryTimeComicBookApi.Data;
using StoryTimeComicBookApi.Data.Entities;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Interfaces;

namespace StoryTimeComicBookApi.Services;

public class VoiceMimickingService : IVoiceMimickingService
{
    private readonly VoiceMimicDataContext _context;
    private readonly ILogger<VoiceMimickingService> _logger;
    private readonly IAudioStorageService _audioStorage;
    private readonly IVoiceModelTrainer _modelTrainer;

    public VoiceMimickingService(
        VoiceMimicDataContext context,
        ILogger<VoiceMimickingService> logger,
        IAudioStorageService audioStorage,
        IVoiceModelTrainer modelTrainer)
    {
        _context = context;
        _logger = logger;
        _audioStorage = audioStorage;
        _modelTrainer = modelTrainer;
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
                    await _modelTrainer.TrainModelAsync(audioSnippets, voiceModel.VoiceModelId);
                    
                    // Update model status after successful training
                    voiceModel.Status = "completed";
                    voiceModel.IsCompleted = true;
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

                // Create association with voice model
                var voiceModelAudioSnippet = new VoiceModelAudioSnippet
                {
                    VoiceModelId = Guid.Parse(voiceModelId),
                    AudioSnippetId = audioSnippet.AudioSnippetId,
                    AddedAt = DateTime.UtcNow
                };

                _context.VoiceModelAudioSnippets.Add(voiceModelAudioSnippet);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AudioSnippetUploadResponse
                {
                    Message = "Audio snippet uploaded and associated with voice model successfully."
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

            // Delete the physical file
            if (File.Exists(audioSnippet.AudioFilePath))
            {
                File.Delete(audioSnippet.AudioFilePath);
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

            // Create replicate model record
            var replicateModel = await _context.ReplicateModels.FirstOrDefaultAsync(
                r => r.ModelName == "voice-cloning-model"); // Replace with actual model name

            if (replicateModel == null)
            {
                replicateModel = new ReplicateModel
                {
                    ModelName = "voice-cloning-model", // Replace with actual model name
                    ModelOwner = "replicate", // Replace with actual owner
                    ReplicateModelIdentifier = "owner/model-name", // Replace with actual identifier
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ReplicateModels.Add(replicateModel);
                await _context.SaveChangesAsync();
            }

            // Create version record
            var modelVersion = new ReplicateModelVersion
            {
                ReplicateModelId = replicateModel.ReplicateModelId,
                VoiceModelId = voiceModel.VoiceModelId,
                VersionIdentifier = $"training_{DateTime.UtcNow:yyyyMMddHHmmss}",
                Status = "training",
                TrainedAt = DateTime.UtcNow
            };

            _context.ReplicateModelVersions.Add(modelVersion);

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

                    await _modelTrainer.TrainModelAsync(audioFilePaths, voiceModel.VoiceModelId);

                    // Update status after training
                    modelVersion.Status = "completed";
                    voiceModel.Status = "completed";
                    voiceModel.IsCompleted = true;
                    voiceModel.ActiveReplicateVersionId = modelVersion.ReplicateVersionId;

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during model training for voice model {VoiceModelId}", voiceModelId);
                    modelVersion.Status = "failed";
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
} 