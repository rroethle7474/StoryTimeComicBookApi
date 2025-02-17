namespace StoryTimeComicBookApi.Models.Gemini;

public class GeminiResponse
{
    public List<Candidate> Candidates { get; set; } = new();
    public PromptFeedback? PromptFeedback { get; set; }
}

public class Candidate
{
    public Content Content { get; set; } = new();
    public string FinishReason { get; set; } = string.Empty;
    public int Index { get; set; }
}

public class PromptFeedback
{
    public SafetyRating[] SafetyRatings { get; set; } = Array.Empty<SafetyRating>();
}

public class SafetyRating
{
    public string Category { get; set; } = string.Empty;
    public string Probability { get; set; } = string.Empty;
} 