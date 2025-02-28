 using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;

namespace StoryTimeComicBookApi.Services.Interfaces;

public interface IVoiceModelTrainer
{
    StartRecordingResponse StartRecordingSessionAsync(StartRecordingRequest request);
    Task TrainModelAsync(List<string> audioFilePaths, Guid modelId, string modelName, string replicateModelId = null);
    Task<string> SynthesizeSpeechAsync(string text, Guid id);
} 