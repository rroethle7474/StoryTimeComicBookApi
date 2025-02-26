 using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;

namespace StoryTimeComicBookApi.Services.Interfaces;

public interface IVoiceModelTrainer
{
    StartRecordingResponse StartRecordingSessionAsync(StartRecordingRequest request);
    Task<string> TrainModelAsync(List<string> audioFilePaths, Guid modelId, string modelName);
    Task<string> SynthesizeSpeechAsync(string text, Guid id);
} 