using Microsoft.AspNetCore.Http;

namespace StoryTimeComicBookApi.Services.Interfaces;

public interface IAudioStorageService
{
    Task<string> SaveAudioFileAsync(IFormFile audioFile, string targetSampleRate = null);
} 