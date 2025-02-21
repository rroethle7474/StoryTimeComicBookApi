using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StoryTimeComicBookApi.Models.Requests;

public class AudioSnippetUploadRequest
{
    public IFormFile AudioFile { get; set; } = null!;
    // Add StepId as form field
    [FromForm(Name = "stepId")]
    public string? StepId { get; set; }

} 