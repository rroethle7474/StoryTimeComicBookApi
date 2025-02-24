using System.Threading.Tasks;

namespace StoryTimeComicBookApi.Services.Interfaces
{
    public interface IImageGenerationService
    {
        Task<string> GenerateComicStyleImage(string imagePath, string userDescription, string outputFolder, string comicBookId);
    }
}