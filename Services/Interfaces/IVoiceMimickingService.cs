using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;

namespace StoryTimeComicBookApi.Services.Interfaces;

public interface IVoiceMimickingService
{
    Task<CreateVoiceModelResponse> CreateVoiceModelAsync(CreateVoiceModelRequest request);
    Task<VoiceModelUpdateResponse> UpdateVoiceModelAsync(string voiceModelId, VoiceModelUpdateRequest request);
    Task<IEnumerable<VoiceModelListResponse>> GetIncompleteVoiceModelsAsync();
    Task<IEnumerable<ReplicateModelListResponse>> GetAvailableReplicateModels(string existingReplicateId = null);
    Task<SynthesizeSpeechResponse> SynthesizeSpeechAsync(SynthesizeSpeechRequest request);
    Task<AudioSnippetUploadResponse> UploadAudioSnippetAsync(AudioSnippetUploadRequest request);
    //Task<TrainModelResponse> TrainModelAsync(TrainModelRequest request);
    StartRecordingResponse StartRecording();
    
    // New methods
    Task<AudioSnippetUploadResponse> AddAudioSnippetToModelAsync(string voiceModelId, AudioSnippetUploadRequest request);
    Task<bool> DeleteAudioSnippetAsync(string audioSnippetId);
    Task<IEnumerable<AudioSnippetResponse>> GetAudioSnippetsForModelAsync(string voiceModelId);
    Task<TrainModelResponse> InitiateModelTrainingAsync(string voiceModelId);

    Task<VoiceModelStepsProgress> GetVoiceModelProgressAsync(string voiceModelId);
    Task<IEnumerable<StepResponse>> GetAllStepsAsync();
    Task<AudioSnippetUploadResponse> AddAudioSnippetForStepAsync(string voiceModelId, string stepId, AudioSnippetUploadRequest request);
    Task<IEnumerable<StepWithRecordingResponse>> GetStepRecordingsForModelAsync(string voiceModelId);
    Task<SynthesizeSpeechResponse> SynthesizeSpeechForModelAsync(Guid modelId, string text);
    Task<bool> DeleteHuggingFaceModelAsync(string modelName);
    Task<List<HuggingFaceModelResponse>> GetHuggingFaceModelsAsync();
} 