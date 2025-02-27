using Microsoft.EntityFrameworkCore;
using StoryTimeComicBookApi.Data;
using StoryTimeComicBookApi.Data.Entities;
using StoryTimeComicBookApi.Data.Enums;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Interfaces;
using System.Text.Json;
using System.Text;
using StoryTimeComicBookApi.Services.Clients.Interfaces;
using System.Web;

namespace StoryTimeComicBookApi.Services;

public class ComicBookService : IComicBookService
{
    private readonly IAiStoryGenerator _storyGenerator;
    private readonly ComicBookDataContext _context;
    private readonly ILogger<ComicBookService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IImageGenerationService _imageGenerationService;

    public ComicBookService(
        IAiStoryGenerator storyGenerator,
        ComicBookDataContext context,
        IConfiguration configuraiton,
        IServiceProvider serviceProvider,
        IImageGenerationService imageGenerationService,
        ILogger<ComicBookService> logger)
    {
        _storyGenerator = storyGenerator;
        _context = context;
        _configuration = configuraiton;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _imageGenerationService = imageGenerationService;
    }

    public async Task<ComicBookCreateResponse> CreateComicBookAsync(ComicBookCreateRequest request)
    {
        // TODO: Implement comic book creation logic
        var comicBook = new ComicBook
        {
            Title = request.Title,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        _context.ComicBooks.Add(comicBook);
        try
        {
            await _context.SaveChangesAsync(); // Save changes to the database
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comic book");
            throw;
        }

        return new ComicBookCreateResponse { ComicBookId = comicBook.ComicBookId.ToString(), Title = comicBook.Title };
    }

    public async Task<ComicBookGetResponse> GetComicBookAsync(string comicBookId)
    {
        var id = Guid.Parse(comicBookId);
        var comicBook = await _context.ComicBooks
            .Include(cb => cb.Scenes)
            .FirstOrDefaultAsync(cb => cb.ComicBookId == id);

        if (comicBook == null)
        {
            throw new KeyNotFoundException($"Comic book with ID {comicBookId} not found");
        }

        return new ComicBookGetResponse
        {
            ComicBookId = comicBook.ComicBookId.ToString(),
            Title = comicBook.Title,
            Description = comicBook.Description,
            IsCompleted = comicBook.IsCompleted,
            Scenes = comicBook.Scenes
                .OrderBy(s => s.SceneOrder)
                .Select(s => new SceneGetResponse
                {
                    SceneId = s.SceneId.ToString(),
                    SceneOrder = s.SceneOrder,
                    ImagePath = s.ImagePath,
                    UserDescription = s.UserDescription,
                    AiGeneratedStory = s.AiGeneratedStory
                }).ToList()
        };
    }

    public async Task<ComicBookUpdateResponse> UpdateComicBookAsync(string comicBookId, ComicBookUpdateRequest request)
    {
        var id = Guid.Parse(comicBookId);
        var comicBook = await _context.ComicBooks.FindAsync(id);

        if (comicBook == null)
        {
            throw new KeyNotFoundException($"Comic book with ID {comicBookId} not found");
        }

        if (request.Title != null) comicBook.Title = request.Title;
        if (request.Description != null) comicBook.Description = request.Description;
        if (request.AdditionalDetails != null) comicBook.AdditionalDetails = request.AdditionalDetails;
        if (request.FinalComicBookPath != null) comicBook.FinalComicBookPath = request.FinalComicBookPath;
        if (request.GenerationStatus != null) comicBook.GenerationStatus = request.GenerationStatus;
        if (request.IsCompleted != null) comicBook.IsCompleted = request.IsCompleted.Value;

        comicBook.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ComicBookUpdateResponse
        {
            ComicBookId = comicBook.ComicBookId.ToString(),
            Title = comicBook.Title,
            Description = comicBook.Description,
            AdditionalDetails = comicBook.AdditionalDetails,
            FinalComicBookPath = comicBook.FinalComicBookPath,
            GenerationStatus = comicBook.GenerationStatus,
            IsCompleted = comicBook.IsCompleted
        };
    }

    public async Task<ComicBookDeleteResponse> DeleteComicBookAsync(string comicBookId)
    {
        var id = Guid.Parse(comicBookId);

        // Load comic book with scenes and assets
        var comicBook = await _context.ComicBooks
            .Include(cb => cb.Scenes)
            .Include(cb => cb.Assets)
            .FirstOrDefaultAsync(cb => cb.ComicBookId == id);

        if (comicBook == null)
        {
            throw new KeyNotFoundException($"Comic book with ID {comicBookId} not found");
        }

        try
        {
            // Start a transaction to ensure data consistency
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Delete physical files for scenes
                foreach (var scene in comicBook.Scenes)
                {
                    if (!string.IsNullOrEmpty(scene.ImagePath))
                    {
                        DeleteFileIfExists(scene.ImagePath);
                    }

                    if (!string.IsNullOrEmpty(scene.StyledImagePath))
                    {
                        DeleteFileIfExists(scene.StyledImagePath);
                    }
                }

                // 2. Delete physical files for assets
                foreach (var asset in comicBook.Assets)
                {
                    if (!string.IsNullOrEmpty(asset.FilePath))
                    {
                        // Delete file (PDF, image, etc)
                        DeleteFileIfExists(asset.FilePath);
                    }
                }

                // 3. Delete comic book (will cascade delete scenes and assets)
                _context.ComicBooks.Remove(comicBook);
                await _context.SaveChangesAsync();

                // 4. Commit the transaction
                await transaction.CommitAsync();

                _logger.LogInformation($"Comic book {comicBookId} and all associated resources deleted successfully");

                return new ComicBookDeleteResponse
                {
                    ComicBookId = comicBookId,
                    IsDeleted = true
                };
            }
            catch (Exception ex)
            {
                // Roll back the transaction if an error occurs
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error deleting comic book {comicBookId}");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting comic book {comicBookId}");
            throw;
        }
    }

    // Helper method to safely delete files
    private void DeleteFileIfExists(string relativePath)
    {
        try
        {
            // Remove leading '/' if present
            relativePath = relativePath.TrimStart('/');

            // Construct full physical path
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation($"File deleted: {fullPath}");
            }
            else
            {
                _logger.LogWarning($"File not found for deletion: {fullPath}");
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - we want to continue with the database deletion even if file deletion fails
            _logger.LogError(ex, $"Error deleting file {relativePath}");
        }
    }

    public async Task<SceneCreateResponse> CreateSceneAsync(SceneCreateRequest request)
    {
        var comicBookId = Guid.Parse(request.ComicBookId);
        var comicBook = await _context.ComicBooks.FindAsync(comicBookId);

        if (comicBook == null)
        {
            throw new KeyNotFoundException($"Comic book with ID {request.ComicBookId} not found");
        }

        var scene = new Scene
        {
            ComicBookId = comicBookId,
            SceneOrder = request.SceneOrder,
            ImagePath = request.ImagePath,
            StyledImagePath = request.StyledImagePath,
            UserDescription = request.UserDescription,
            DialogueText = request.DialogueText,
            TransitionNotes = request.TransitionNotes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Scenes.Add(scene);
        await _context.SaveChangesAsync();

        return new SceneCreateResponse
        {
            SceneId = scene.SceneId.ToString(),
            SceneOrder = scene.SceneOrder,
            ImagePath = scene.ImagePath,
            StyledImagePath = scene.StyledImagePath,
            UserDescription = scene.UserDescription,
            DialogueText = scene.DialogueText,
            TransitionNotes = scene.TransitionNotes
        };
    }

    public async Task<SceneGetResponse> GetSceneAsync(string sceneId)
    {
        var id = Guid.Parse(sceneId);
        var scene = await _context.Scenes.FindAsync(id);

        if (scene == null)
        {
            throw new KeyNotFoundException($"Scene with ID {sceneId} not found");
        }

        return new SceneGetResponse
        {
            SceneId = scene.SceneId.ToString(),
            SceneOrder = scene.SceneOrder,
            ImagePath = scene.ImagePath,
            UserDescription = scene.UserDescription,
            AiGeneratedStory = scene.AiGeneratedStory
        };
    }

    public async Task<SceneUpdateResponse> UpdateSceneAsync(string sceneId, SceneUpdateRequest request)
    {
        var id = Guid.Parse(sceneId);
        var scene = await _context.Scenes.FindAsync(id);

        if (scene == null)
        {
            throw new KeyNotFoundException($"Scene with ID {sceneId} not found");
        }

        if (request.ImagePath != null) scene.ImagePath = request.ImagePath;
        if (request.StyledImagePath != null) scene.StyledImagePath = request.StyledImagePath;
        if (request.UserDescription != null) scene.UserDescription = request.UserDescription;
        if (request.DialogueText != null) scene.DialogueText = request.DialogueText;
        if (request.TransitionNotes != null) scene.TransitionNotes = request.TransitionNotes;
        if (request.AiGeneratedStory != null) scene.AiGeneratedStory = request.AiGeneratedStory;
        if (request.SceneOrder != null) scene.SceneOrder = request.SceneOrder.Value;

        scene.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new SceneUpdateResponse
        {
            SceneId = scene.SceneId.ToString(),
            SceneOrder = scene.SceneOrder,
            ImagePath = scene.ImagePath,
            StyledImagePath = scene.StyledImagePath,
            UserDescription = scene.UserDescription,
            DialogueText = scene.DialogueText,
            TransitionNotes = scene.TransitionNotes,
            AiGeneratedStory = scene.AiGeneratedStory
        };
    }

    public async Task<SceneDeleteResponse> DeleteSceneAsync(string sceneId)
    {
        var id = Guid.Parse(sceneId);
        var scene = await _context.Scenes.FindAsync(id);

        if (scene == null)
        {
            throw new KeyNotFoundException($"Scene with ID {sceneId} not found");
        }

        _context.Scenes.Remove(scene);
        await _context.SaveChangesAsync();

        return new SceneDeleteResponse
        {
            SceneId = sceneId,
            IsDeleted = true
        };
    }

    public async IAsyncEnumerable<GenerateStoryResponse> GenerateSceneStoryAsync(GenerateStoryRequest request)
    {
        var buffer = new List<string>();
        
        try
        {
            await foreach (var chunk in _storyGenerator.GenerateStoryAsync(request.UserDescription))
            {
                buffer.Add(chunk);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating story for scene {SceneId}", request.SceneId);
            throw;
        }

        foreach (var chunk in buffer)
        {
            yield return new GenerateStoryResponse
            {
                SceneId = request.SceneId,
                StoryTextChunk = chunk,
                IsComplete = false
            };
        }

        // Signal completion
        yield return new GenerateStoryResponse
        {
            SceneId = request.SceneId,
            StoryTextChunk = string.Empty,
            IsComplete = true
        };
    }

    public async Task<IEnumerable<ComicBookListResponse>> GetIncompleteComicBooksAsync()
    {
        var incompleteComics = await _context.ComicBooks
            .Where(cb => !cb.IsCompleted)
            .Select(cb => new ComicBookListResponse
            {
                ComicBookId = cb.ComicBookId.ToString(),
                Title = cb.Title,
                Description = cb.Description,
                AdditionalDetails = cb.AdditionalDetails,
                IsCompleted = cb.IsCompleted,
                CreatedAt = cb.CreatedAt,
                UpdatedAt = cb.UpdatedAt
            })
            .ToListAsync();

        return incompleteComics;
    }

    public async Task<IEnumerable<SceneGetResponse>> GetScenesAsync(string comicBookId)
    {
        var id = Guid.Parse(comicBookId);
        var comicBook = await _context.ComicBooks
            .Include(cb => cb.Scenes)
            .FirstOrDefaultAsync(cb => cb.ComicBookId == id);

        if (comicBook == null)
        {
            throw new KeyNotFoundException($"Comic book with ID {comicBookId} not found");
        }

        return comicBook.Scenes
            .OrderBy(s => s.SceneOrder)
            .Select(s => new SceneGetResponse
            {
                SceneId = s.SceneId.ToString(),
                SceneOrder = s.SceneOrder,
                ImagePath = s.ImagePath,
                StyledImagePath = s.StyledImagePath,
                UserDescription = s.UserDescription,
                DialogueText = s.DialogueText,
                TransitionNotes = s.TransitionNotes,
                AiGeneratedStory = s.AiGeneratedStory
            });
    }

    public async Task<AssetResponse> CreateAssetAsync(AssetCreateRequest request)
    {
        var comicBookId = Guid.Parse(request.ComicBookId);
        var comicBook = await _context.ComicBooks.FindAsync(comicBookId);

        if (comicBook == null)
        {
            throw new KeyNotFoundException($"Comic book with ID {request.ComicBookId} not found");
        }

        var asset = new ComicBookAsset
        {
            ComicBookId = comicBookId,
            AssetType = request.AssetType,
            FilePath = request.FilePath,
            FullStoryText = request.FullStoryText,
            Status = request.Status,
            PageNumber = request.PageNumber,
            CreatedAt = DateTime.UtcNow
        };

        _context.ComicBookAssets.Add(asset);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error creating asset for comic book {ComicBookId}. AssetType: {AssetType}", 
                request.ComicBookId, 
                request.AssetType);
            throw;
        }

        return new AssetResponse
        {
            AssetId = asset.AssetId.ToString(),
            ComicBookId = asset.ComicBookId.ToString(),
            AssetType = asset.AssetType,
            FilePath = asset.FilePath,
            FullStoryText = asset.FullStoryText,
            Status = asset.Status,
            PageNumber = asset.PageNumber,
            CreatedAt = asset.CreatedAt
        };
    }

    public async Task<AssetResponse> GetAssetAsync(string assetId)
    {
        var id = Guid.Parse(assetId);
        var asset = await _context.ComicBookAssets.FindAsync(id);

        if (asset == null)
        {
            throw new KeyNotFoundException($"Asset with ID {assetId} not found");
        }

        return new AssetResponse
        {
            AssetId = asset.AssetId.ToString(),
            FullStoryText = asset.FullStoryText,
            ComicBookId = asset.ComicBookId.ToString(),
            Status = asset.Status,
            AssetType = asset.AssetType,
            FilePath = asset.FilePath,
            PageNumber = asset.PageNumber,
            CreatedAt = asset.CreatedAt
        };
    }

    public async Task<AssetResponse> UpdateAssetAsync(string assetId, AssetUpdateRequest request)
    {
        var id = Guid.Parse(assetId);
        var asset = await _context.ComicBookAssets.FindAsync(id);

        if (asset == null)
        {
            throw new KeyNotFoundException($"Asset with ID {assetId} not found");
        }

        if (request.AssetType != null) asset.AssetType = request.AssetType;
        if (request.FilePath != null) asset.FilePath = request.FilePath;
        if (request.FullStoryText != null) asset.FullStoryText = request.FullStoryText;
        if (request.Status != null) asset.Status = request.Status;
        if (request.PageNumber != null) asset.PageNumber = request.PageNumber;

        await _context.SaveChangesAsync();

        return new AssetResponse
        {
            AssetId = asset.AssetId.ToString(),
            ComicBookId = asset.ComicBookId.ToString(),
            AssetType = asset.AssetType,
            FilePath = asset.FilePath,
            FullStoryText = asset.FullStoryText,
            Status = asset.Status,
            PageNumber = asset.PageNumber,
            CreatedAt = asset.CreatedAt
        };
    }

    public async Task<bool> DeleteAssetAsync(string assetId)
    {
        var id = Guid.Parse(assetId);
        var asset = await _context.ComicBookAssets.FindAsync(id);

        if (asset == null)
        {
            throw new KeyNotFoundException($"Asset with ID {assetId} not found");
        }

        _context.ComicBookAssets.Remove(asset);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<AssetResponse>> GetComicBookAssetsAsync(string comicBookId)
    {
        var id = Guid.Parse(comicBookId);
        var assets = await _context.ComicBookAssets
            .Where(a => a.ComicBookId == id)
            .OrderBy(a => a.PageNumber)
            .ToListAsync();

        return assets.Select(asset => new AssetResponse
        {
            AssetId = asset.AssetId.ToString(),
            ComicBookId = asset.ComicBookId.ToString(),
            AssetType = asset.AssetType,
            FilePath = asset.FilePath,
            PageNumber = asset.PageNumber,
            CreatedAt = asset.CreatedAt
        });
    }

    public async Task<bool> GenerateComicBookAsync(Guid assetId)
    {
        var asset = await _context.ComicBookAssets
            .Include(a => a.ComicBook)
            .FirstOrDefaultAsync(a => a.AssetId == assetId);

        if (asset == null)
        {
            throw new KeyNotFoundException($"Asset with ID {assetId} not found");
        }

        // Update asset status to processing
        asset.Status = "PROCESSING";
        await _context.SaveChangesAsync();

        var comicBook = asset.ComicBook;
        var scenes = await _context.Scenes
            .Where(s => s.ComicBookId == comicBook.ComicBookId)
            .OrderBy(s => s.SceneOrder)
            .ToListAsync();

        if (!scenes.Any())
        {
            throw new InvalidOperationException("No scenes found for this comic book.");
        }

        // Create directory for comic book assets if it doesn't exist
        string comicBookFolder = Path.Combine("wwwroot", "comics", comicBook.ComicBookId.ToString());
        if (!Directory.Exists(comicBookFolder))
        {
            Directory.CreateDirectory(comicBookFolder);
        }

        try
        {
            // Process scene images in parallel
            var imageProcessingTasks = scenes
                    .Where(s => !string.IsNullOrEmpty(s.ImagePath) && !string.IsNullOrEmpty(s.UserDescription))
                    .Select(async scene =>
                    {
                        string styledImagePath = await _imageGenerationService.GenerateComicStyleImage(
                            scene.ImagePath,
                            scene.UserDescription,
                            comicBookFolder,
                            comicBook.ComicBookId.ToString());

                        scene.StyledImagePath = styledImagePath;
                        return scene;
                    });

            // Generate full story text in parallel with image processing
            var storyGenerationTask = GenerateFullStory(comicBook, scenes);

            // Wait for all image processing tasks to complete
            var processedScenes = await Task.WhenAll(imageProcessingTasks);

            //// Save updated scenes with styled image paths
            foreach (var scene in processedScenes)
            {
                _context.Scenes.Update(scene);
            }
            await _context.SaveChangesAsync();

            // Get the completed story text
            string fullStory = await storyGenerationTask;

            // Update asset with the full story text
            asset.FullStoryText = fullStory;
            asset.Status = "COMPLETED";
            _context.ComicBookAssets.Update(asset);

            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comic book for asset {AssetId}", assetId);
            asset.Status = "FAILED";
            await _context.SaveChangesAsync();
            throw;
        }
    }

    public async Task<IEnumerable<CompletedComicResponse>> GetCompletedComicsAsync()
    {
        var completedComics = await _context.ComicBooks
            .Where(cb => cb.IsCompleted)
            .Join(
                _context.ComicBookAssets.Where(a => a.AssetType == "FULL_STORY" && a.Status == "COMPLETED"),
                cb => cb.ComicBookId,
                asset => asset.ComicBookId,
                (cb, asset) => new CompletedComicResponse
                {
                    ComicBookId = cb.ComicBookId.ToString(),
                    AssetId = asset.AssetId.ToString(),
                    Title = cb.Title,
                    Description = cb.Description ?? string.Empty,
                    FilePath = asset.FilePath,
                    CompletedAt = asset.CreatedAt
                }
            )
            .OrderByDescending(c => c.CompletedAt)
            .ToListAsync();

        return completedComics;
    }

    private async Task<string> GenerateFullStory(ComicBook comicBook, List<Scene> scenes)
    {
        using var scope = _serviceProvider.CreateScope();
        var llmClient = scope.ServiceProvider.GetRequiredService<ILlmClient>();

        var storyPrompt = new StringBuilder();
        storyPrompt.AppendLine($"Create a complete comic book story with the following details:");
        storyPrompt.AppendLine($"Title: {comicBook.Title}");

        if (!string.IsNullOrEmpty(comicBook.Description))
        {
            storyPrompt.AppendLine($"Description: {comicBook.Description}");
        }

        if (!string.IsNullOrEmpty(comicBook.AdditionalDetails))
        {
            storyPrompt.AppendLine($"Additional Details: {comicBook.AdditionalDetails}");
        }

        storyPrompt.AppendLine("\nThe story should be structured with the following scenes:");

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            storyPrompt.AppendLine($"\nScene {i + 1}: {scene.UserDescription}");
        }

        storyPrompt.AppendLine("\nFormat the story as HTML with placeholders for images in each scene. Use <img> tags with the 'scene-X' class to indicate where each scene's image should be placed, where X is the scene number.");
        storyPrompt.AppendLine("\nStructure the content with proper HTML tags for paragraphs, headings, etc. Include a title, introduction, and conclusion.");

        var finalStoryBuilder = new StringBuilder();
        var allText = new StringBuilder();

        await foreach (var chunk in llmClient.GenerateContentStreamAsync(storyPrompt.ToString()))
        {
            finalStoryBuilder.Append(chunk);
            allText.Append(chunk);
        }

        string htmlStory = finalStoryBuilder.ToString();

        // Process the HTML to insert the actual image paths
        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            string imagePlaceholder = $"<img class=\"scene-{i + 1}\"";
            string imageReplacement = $"<img class=\"scene-{i + 1}\" src=\"{scene.StyledImagePath}\" alt=\"{HttpUtility.HtmlEncode(scene.UserDescription)}\"";

            htmlStory = htmlStory.Replace(imagePlaceholder, imageReplacement);
        }

        return htmlStory;
    }


}