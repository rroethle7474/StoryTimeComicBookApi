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
        // search for a duplicate model name in the database (trimed and case insensitive)
        try
        {
            var duplicateModel = await _context.VoiceModels
                .Where(v => v.VoiceModelName.Trim().ToLower() == request.VoiceModelName.Trim().ToLower())
                .FirstOrDefaultAsync();

            if (duplicateModel != null)
            {
                throw new InvalidOperationException("A voice model with the same name already exists");
            }

            // Create a new voice model record
            var voiceModel = new VoiceModel
            {
                VoiceModelName = request.VoiceModelName,
                VoiceModelDescription = request.VoiceModelDescription
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
            throw new Exception(ex.Message);
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

    public async Task<SynthesizeSpeechResponse> SynthesizeSpeechAsync(SynthesizeSpeechRequest request)
    {
        try
        {
            // Get the latest trained voice model
            var latestModel = await _context.VoiceModels
                .OrderByDescending(v => v.TrainingDate)
                .FirstOrDefaultAsync();

            if (latestModel == null)
            {
                throw new InvalidOperationException("No trained voice model available");
            }

            // Use the model trainer to synthesize speech
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

    public async Task<AudioSnippetUploadResponse> UploadAudioSnippetAsync(AudioSnippetUploadRequest request)
    {
        try
        {
            // Save the audio file to storage
            var filePath = await _audioStorage.SaveAudioFileAsync(request.AudioFile);

            // Create a new audio snippet record
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
            // Get all available audio snippets
            var audioSnippets = await _context.AudioSnippets
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.AudioFilePath)
                .ToListAsync();

            if (!audioSnippets.Any())
            {
                throw new InvalidOperationException("No audio snippets available for training");
            }

            // Create a new voice model record
            var voiceModel = new VoiceModel
            {
                VoiceModelName = $"Model_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during model training for model {ModelId}", voiceModel.VoiceModelId);
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

    public StartRecordingResponse StartRecording()
    {
        try
        {
            // Generate a unique session ID for this recording
            var sessionId = Guid.NewGuid().ToString();

            // You might want to store the session information in a cache or database
            // depending on your requirements

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
} 