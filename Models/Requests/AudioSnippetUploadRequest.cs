using Microsoft.AspNetCore.Http;

namespace StoryTimeComicBookApi.Models.Requests;

public class AudioSnippetUploadRequest
{
    public IFormFile AudioFile { get; set; } = null!;
} 