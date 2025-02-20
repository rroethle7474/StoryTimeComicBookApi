using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;

namespace StoryTimeComicBookApi.Services.Interfaces;

public interface IVoiceMimickingService
{
    Task<CreateVoiceModelResponse> CreateVoiceModelAsync(CreateVoiceModelRequest request);
    Task<VoiceModelUpdateResponse> UpdateVoiceModelAsync(string voiceModelId, VoiceModelUpdateRequest request);
    Task<IEnumerable<VoiceModelListResponse>> GetIncompleteVoiceModelsAsync();
    Task<SynthesizeSpeechResponse> SynthesizeSpeechAsync(SynthesizeSpeechRequest request);
    Task<AudioSnippetUploadResponse> UploadAudioSnippetAsync(AudioSnippetUploadRequest request);
    Task<TrainModelResponse> TrainModelAsync(TrainModelRequest request);
    StartRecordingResponse StartRecording();
} 