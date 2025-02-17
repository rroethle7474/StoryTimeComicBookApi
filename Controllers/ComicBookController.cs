using Microsoft.AspNetCore.Mvc;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Interfaces;

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
    public async Task<ActionResult<ComicBookCreateResponse>> CreateComicBook([FromBody] ComicBookCreateRequest request)
    {
        try
        {
            var response = await _comicBookService.CreateComicBookAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comic book");
            return StatusCode(500, "An error occurred while creating the comic book");
        }
    }

    [HttpGet("{comicBookId}")]
    public async Task<ActionResult<ComicBookGetResponse>> GetComicBook(string comicBookId)
    {
        try
        {
            var response = await _comicBookService.GetComicBookAsync(comicBookId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comic book");
            return StatusCode(500, "An error occurred while retrieving the comic book");
        }
    }

    [HttpPut("{comicBookId}")]
    public async Task<ActionResult<ComicBookUpdateResponse>> UpdateComicBook(string comicBookId, [FromBody] ComicBookUpdateRequest request)
    {
        try
        {
            var response = await _comicBookService.UpdateComicBookAsync(comicBookId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comic book");
            return StatusCode(500, "An error occurred while updating the comic book");
        }
    }

    [HttpDelete("{comicBookId}")]
    public async Task<ActionResult<ComicBookDeleteResponse>> DeleteComicBook(string comicBookId)
    {
        try
        {
            var response = await _comicBookService.DeleteComicBookAsync(comicBookId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comic book");
            return StatusCode(500, "An error occurred while deleting the comic book");
        }
    }

    [HttpPost("{comicBookId}/scene")]
    public async Task<ActionResult<SceneCreateResponse>> CreateScene(string comicBookId, [FromBody] SceneCreateRequest request)
    {
        try
        {
            if (request.ComicBookId != comicBookId)
            {
                return BadRequest("Comic book ID in route must match request body");
            }
            var response = await _comicBookService.CreateSceneAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scene");
            return StatusCode(500, "An error occurred while creating the scene");
        }
    }

    [HttpGet("{comicBookId}/scene/{sceneId}")]
    public async Task<ActionResult<SceneGetResponse>> GetScene(string comicBookId, string sceneId)
    {
        try
        {
            var response = await _comicBookService.GetSceneAsync(sceneId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scene");
            return StatusCode(500, "An error occurred while retrieving the scene");
        }
    }

    [HttpPut("{comicBookId}/scene/{sceneId}")]
    public async Task<ActionResult<SceneUpdateResponse>> UpdateScene(string comicBookId, string sceneId, [FromBody] SceneUpdateRequest request)
    {
        try
        {
            var response = await _comicBookService.UpdateSceneAsync(sceneId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scene");
            return StatusCode(500, "An error occurred while updating the scene");
        }
    }

    [HttpDelete("{comicBookId}/scene/{sceneId}")]
    public async Task<ActionResult<SceneDeleteResponse>> DeleteScene(string comicBookId, string sceneId)
    {
        try
        {
            var response = await _comicBookService.DeleteSceneAsync(sceneId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scene");
            return StatusCode(500, "An error occurred while deleting the scene");
        }
    }

    [HttpPost("{comicBookId}/scene/{sceneId}/generate-story")]
    public IActionResult GenerateStory(string comicBookId, string sceneId, [FromBody] GenerateStoryRequest request)
    {
        try
        {
            if (request.SceneId != sceneId)
            {
                return BadRequest("Scene ID in route must match request body");
            }
            
            // Return a stream of story chunks
            return Ok(_comicBookService.GenerateSceneStoryAsync(request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating story");
            return StatusCode(500, "An error occurred while generating the story");
        }
    }
}
