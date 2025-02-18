using Microsoft.AspNetCore.Mvc;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Interfaces;
using StoryTimeComicBookApi.Models.Common;

namespace StoryTimeComicBookApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComicBookController : ControllerBase
{
    private readonly IComicBookService _comicBookService;
    private readonly ILogger<ComicBookController> _logger;

    public ComicBookController(IComicBookService comicBookService, ILogger<ComicBookController> logger)
    {
        _comicBookService = comicBookService;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<ComicBookCreateResponse>>> CreateComicBook([FromBody] ComicBookCreateRequest request)
    {
        try
        {
            var response = await _comicBookService.CreateComicBookAsync(request);
            return Ok(ApiResponse<ComicBookCreateResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comic book");
            return StatusCode(500, ApiResponse<ComicBookCreateResponse>.Failure(
                "An error occurred while creating the comic book",
                "COMIC_CREATE_ERROR",
                ex.Message));
        }
    }

    [HttpGet("{comicBookId}")]
    public async Task<ActionResult<ApiResponse<ComicBookGetResponse>>> GetComicBook(string comicBookId)
    {
        try
        {
            var response = await _comicBookService.GetComicBookAsync(comicBookId);
            return Ok(ApiResponse<ComicBookGetResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Comic book not found");
            return NotFound(ApiResponse<ComicBookGetResponse>.Failure(
                "Comic book not found",
                "COMIC_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comic book");
            return StatusCode(500, ApiResponse<ComicBookGetResponse>.Failure(
                "An error occurred while retrieving the comic book",
                "COMIC_RETRIEVE_ERROR",
                ex.Message));
        }
    }

    [HttpPut("{comicBookId}")]
    public async Task<ActionResult<ApiResponse<ComicBookUpdateResponse>>> UpdateComicBook(string comicBookId, [FromBody] ComicBookUpdateRequest request)
    {
        try
        {
            var response = await _comicBookService.UpdateComicBookAsync(comicBookId, request);
            return Ok(ApiResponse<ComicBookUpdateResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Comic book not found");
            return NotFound(ApiResponse<ComicBookUpdateResponse>.Failure(
                "Comic book not found",
                "COMIC_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comic book");
            return StatusCode(500, ApiResponse<ComicBookUpdateResponse>.Failure(
                "An error occurred while updating the comic book",
                "COMIC_UPDATE_ERROR",
                ex.Message));
        }
    }

    [HttpDelete("{comicBookId}")]
    public async Task<ActionResult<ApiResponse<ComicBookDeleteResponse>>> DeleteComicBook(string comicBookId)
    {
        try
        {
            var response = await _comicBookService.DeleteComicBookAsync(comicBookId);
            return Ok(ApiResponse<ComicBookDeleteResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Comic book not found");
            return NotFound(ApiResponse<ComicBookDeleteResponse>.Failure(
                "Comic book not found",
                "COMIC_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comic book");
            return StatusCode(500, ApiResponse<ComicBookDeleteResponse>.Failure(
                "An error occurred while deleting the comic book",
                "COMIC_DELETE_ERROR",
                ex.Message));
        }
    }

    [HttpPost("{comicBookId}/scene")]
    public async Task<ActionResult<ApiResponse<SceneCreateResponse>>> CreateScene(string comicBookId, [FromBody] SceneCreateRequest request)
    {
        try
        {
            if (request.ComicBookId != comicBookId)
            {
                return BadRequest(ApiResponse<SceneCreateResponse>.Failure(
                    "Comic book ID in route must match request body",
                    "INVALID_COMIC_ID"));
            }
            var response = await _comicBookService.CreateSceneAsync(request);
            return Ok(ApiResponse<SceneCreateResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Comic book not found");
            return NotFound(ApiResponse<SceneCreateResponse>.Failure(
                "Comic book not found",
                "COMIC_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scene");
            return StatusCode(500, ApiResponse<SceneCreateResponse>.Failure(
                "An error occurred while creating the scene",
                "SCENE_CREATE_ERROR",
                ex.Message));
        }
    }

    [HttpGet("{comicBookId}/scene/{sceneId}")]
    public async Task<ActionResult<ApiResponse<SceneGetResponse>>> GetScene(string comicBookId, string sceneId)
    {
        try
        {
            var response = await _comicBookService.GetSceneAsync(sceneId);
            return Ok(ApiResponse<SceneGetResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Scene not found");
            return NotFound(ApiResponse<SceneGetResponse>.Failure(
                "Scene not found",
                "SCENE_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scene");
            return StatusCode(500, ApiResponse<SceneGetResponse>.Failure(
                "An error occurred while retrieving the scene",
                "SCENE_RETRIEVE_ERROR",
                ex.Message));
        }
    }

    [HttpPut("{comicBookId}/scene/{sceneId}")]
    public async Task<ActionResult<ApiResponse<SceneUpdateResponse>>> UpdateScene(string comicBookId, string sceneId, [FromBody] SceneUpdateRequest request)
    {
        try
        {
            var response = await _comicBookService.UpdateSceneAsync(sceneId, request);
            return Ok(ApiResponse<SceneUpdateResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Scene not found");
            return NotFound(ApiResponse<SceneUpdateResponse>.Failure(
                "Scene not found",
                "SCENE_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scene");
            return StatusCode(500, ApiResponse<SceneUpdateResponse>.Failure(
                "An error occurred while updating the scene",
                "SCENE_UPDATE_ERROR",
                ex.Message));
        }
    }

    [HttpDelete("{comicBookId}/scene/{sceneId}")]
    public async Task<ActionResult<ApiResponse<SceneDeleteResponse>>> DeleteScene(string comicBookId, string sceneId)
    {
        try
        {
            var response = await _comicBookService.DeleteSceneAsync(sceneId);
            return Ok(ApiResponse<SceneDeleteResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Scene not found");
            return NotFound(ApiResponse<SceneDeleteResponse>.Failure(
                "Scene not found",
                "SCENE_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scene");
            return StatusCode(500, ApiResponse<SceneDeleteResponse>.Failure(
                "An error occurred while deleting the scene",
                "SCENE_DELETE_ERROR",
                ex.Message));
        }
    }

    [HttpPost("{comicBookId}/scene/{sceneId}/generate-story")]
    public ActionResult<ApiResponse<IAsyncEnumerable<GenerateStoryResponse>>> GenerateStory(string comicBookId, string sceneId, [FromBody] GenerateStoryRequest request)
    {
        try
        {
            if (request.SceneId != sceneId)
            {
                return BadRequest(ApiResponse<IAsyncEnumerable<GenerateStoryResponse>>.Failure(
                    "Scene ID in route must match request body",
                    "INVALID_SCENE_ID"));
            }
            
            var response = _comicBookService.GenerateSceneStoryAsync(request);
            return Ok(ApiResponse<IAsyncEnumerable<GenerateStoryResponse>>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Scene not found");
            return NotFound(ApiResponse<IAsyncEnumerable<GenerateStoryResponse>>.Failure(
                "Scene not found",
                "SCENE_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating story");
            return StatusCode(500, ApiResponse<IAsyncEnumerable<GenerateStoryResponse>>.Failure(
                "An error occurred while generating the story",
                "STORY_GENERATION_ERROR",
                ex.Message));
        }
    }
}
