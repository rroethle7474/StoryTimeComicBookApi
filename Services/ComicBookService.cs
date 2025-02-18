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

        if (request.Title != null)
        {
            comicBook.Title = request.Title;
        }
        if (request.Description != null)
        {
            comicBook.Description = request.Description;
        }

        if (request.IsCompleted != null)
        {
            comicBook.IsCompleted = request.IsCompleted.Value;
        }


        comicBook.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ComicBookUpdateResponse
        {
            ComicBookId = comicBook.ComicBookId.ToString(),
            Title = comicBook.Title,
            Description = comicBook.Description,
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
            UserDescription = request.UserDescription,
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
            UserDescription = scene.UserDescription
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

        if (request.ImagePath != null)
        {
            scene.ImagePath = request.ImagePath;
        }
        if (request.UserDescription != null)
        {
            scene.UserDescription = request.UserDescription;
        }
        if (request.AiGeneratedStory != null)
        {
            scene.AiGeneratedStory = request.AiGeneratedStory;
        }
        scene.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new SceneUpdateResponse
        {
            SceneId = scene.SceneId.ToString(),
            SceneOrder = scene.SceneOrder,
            ImagePath = scene.ImagePath,
            UserDescription = scene.UserDescription,
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
                UserDescription = s.UserDescription,
                AiGeneratedStory = s.AiGeneratedStory
            });
    }
} 