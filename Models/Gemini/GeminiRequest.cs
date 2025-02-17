namespace StoryTimeComicBookApi.Models.Gemini;

public class GeminiRequest
{
    public List<Content> Contents { get; set; } = new();
    public GenerationConfig? GenerationConfig { get; set; }
}

public class Content
{
    public List<Part> Parts { get; set; } = new();
}

public class Part
{
    public string Text { get; set; } = string.Empty;
}

public class GenerationConfig
{
    public double? Temperature { get; set; }
    public int? CandidateCount { get; set; }
    public int? MaxOutputTokens { get; set; }
} 