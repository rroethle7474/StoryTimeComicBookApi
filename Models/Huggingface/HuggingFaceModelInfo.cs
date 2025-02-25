namespace StoryTimeComicBookApi.Models.Huggingface
{
    public class HuggingFaceModelInfo
    {
        public string ModelId { get; set; }
        public string Name { get; set; }
        public DateTime LastModified { get; set; }
        public string Description { get; set; }
        public string[] Tags { get; set; }
        public string Private { get; set; }
    }
}
