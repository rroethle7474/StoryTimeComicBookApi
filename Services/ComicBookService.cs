using Microsoft.EntityFrameworkCore;
using StoryTimeComicBookApi.Data;
using StoryTimeComicBookApi.Data.Entities;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Interfaces;

namespace StoryTimeComicBookApi.Services;

public class ComicBookService : IComicBookService
{
    private readonly IAiStoryGenerator _storyGenerator;
    private readonly ComicBookDataContext _context;
    private readonly ILogger<ComicBookService> _logger;

    public ComicBookService(
        IAiStoryGenerator storyGenerator,
        ComicBookDataContext context,
        ILogger<ComicBookService> logger)
    {
        _storyGenerator = storyGenerator;
        _context = context;
        _logger = logger;
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
        var comicBook = await _context.ComicBooks.FindAsync(id);

        if (comicBook == null)
        {
            throw new KeyNotFoundException($"Comic book with ID {comicBookId} not found");
        }

        _context.ComicBooks.Remove(comicBook);
        await _context.SaveChangesAsync();

        return new ComicBookDeleteResponse
        {
            ComicBookId = comicBookId,
            IsDeleted = true
        };
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
            PageNumber = request.PageNumber,
            CreatedAt = DateTime.UtcNow
        };

        _context.ComicBookAssets.Add(asset);
        await _context.SaveChangesAsync();

        return new AssetResponse
        {
            AssetId = asset.AssetId.ToString(),
            ComicBookId = asset.ComicBookId.ToString(),
            AssetType = asset.AssetType,
            FilePath = asset.FilePath,
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
            ComicBookId = asset.ComicBookId.ToString(),
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
        if (request.PageNumber != null) asset.PageNumber = request.PageNumber;

        await _context.SaveChangesAsync();

        return new AssetResponse
        {
            AssetId = asset.AssetId.ToString(),
            ComicBookId = asset.ComicBookId.ToString(),
            AssetType = asset.AssetType,
            FilePath = asset.FilePath,
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
} 