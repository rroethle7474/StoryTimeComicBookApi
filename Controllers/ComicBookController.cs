using Microsoft.AspNetCore.Mvc;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Interfaces;
using StoryTimeComicBookApi.Models.Common;
using DinkToPdf.Contracts;
using DinkToPdf;

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

    [HttpGet("incomplete")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ComicBookListResponse>>>> GetIncompleteComicBooks()
    {
        try
        {
            var response = await _comicBookService.GetIncompleteComicBooksAsync();
            return Ok(ApiResponse<IEnumerable<ComicBookListResponse>>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving incomplete comic books");
            return StatusCode(500, ApiResponse<IEnumerable<ComicBookListResponse>>.Failure(
                "An error occurred while retrieving incomplete comic books",
                "COMIC_LIST_ERROR",
                ex.Message));
        }
    }

    [HttpGet("{comicBookId}/scenes")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SceneGetResponse>>>> GetScenes(string comicBookId)
    {
        try
        {
            var response = await _comicBookService.GetScenesAsync(comicBookId);
            return Ok(ApiResponse<IEnumerable<SceneGetResponse>>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Comic book not found");
            return NotFound(ApiResponse<IEnumerable<SceneGetResponse>>.Failure(
                "Comic book not found",
                "COMIC_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scenes");
            return StatusCode(500, ApiResponse<IEnumerable<SceneGetResponse>>.Failure(
                "An error occurred while retrieving the scenes",
                "SCENES_RETRIEVE_ERROR",
                ex.Message));
        }
    }

    [HttpPost("upload/scene-image")]
    public async Task<ActionResult<ApiResponse<ImageUploadResponse>>> UploadSceneImage(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<ImageUploadResponse>.Failure("No file uploaded"));

            // Create uploads directory if it doesn't exist
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "scenes");
            Directory.CreateDirectory(uploadsDir);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path that will be stored in database
            var relativePath = $"/uploads/scenes/{fileName}";
            
            return Ok(ApiResponse<ImageUploadResponse>.Success(new ImageUploadResponse 
            { 
                ImagePath = relativePath 
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ImageUploadResponse>.Failure(
                "Error uploading image",
                "IMAGE_UPLOAD_ERROR",
                ex.Message));
        }
    }

    [HttpPost("{comicBookId}/assets")]
    public async Task<ActionResult<ApiResponse<AssetResponse>>> CreateAsset(string comicBookId, [FromBody] AssetCreateRequest request)
    {
        try
        {
            if (request.ComicBookId != comicBookId)
            {
                return BadRequest(ApiResponse<AssetResponse>.Failure(
                    "Comic book ID in route must match request body",
                    "INVALID_COMIC_ID"));
            }
            var response = await _comicBookService.CreateAssetAsync(request);
            return Ok(ApiResponse<AssetResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<AssetResponse>.Failure(
                "Comic book not found",
                "COMIC_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset");
            return StatusCode(500, ApiResponse<AssetResponse>.Failure(
                "An error occurred while creating the asset",
                "ASSET_CREATE_ERROR",
                ex.Message));
        }
    }

    [HttpGet("assets/{assetId}")]
    public async Task<ActionResult<ApiResponse<AssetResponse>>> GetAsset(string assetId)
    {
        try
        {
            var response = await _comicBookService.GetAssetAsync(assetId);
            return Ok(ApiResponse<AssetResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<AssetResponse>.Failure(
                "Asset not found",
                "ASSET_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset");
            return StatusCode(500, ApiResponse<AssetResponse>.Failure(
                "An error occurred while retrieving the asset",
                "ASSET_RETRIEVE_ERROR",
                ex.Message));
        }
    }

    [HttpPut("assets/{assetId}")]
    public async Task<ActionResult<ApiResponse<AssetResponse>>> UpdateAsset(string assetId, [FromBody] AssetUpdateRequest request)
    {
        try
        {
            var response = await _comicBookService.UpdateAssetAsync(assetId, request);
            return Ok(ApiResponse<AssetResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<AssetResponse>.Failure(
                "Asset not found",
                "ASSET_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset");
            return StatusCode(500, ApiResponse<AssetResponse>.Failure(
                "An error occurred while updating the asset",
                "ASSET_UPDATE_ERROR",
                ex.Message));
        }
    }

    [HttpDelete("assets/{assetId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAsset(string assetId)
    {
        try
        {
            var response = await _comicBookService.DeleteAssetAsync(assetId);
            return Ok(ApiResponse<bool>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.Failure(
                "Asset not found",
                "ASSET_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset");
            return StatusCode(500, ApiResponse<bool>.Failure(
                "An error occurred while deleting the asset",
                "ASSET_DELETE_ERROR",
                ex.Message));
        }
    }

    [HttpGet("{comicBookId}/assets")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AssetResponse>>>> GetComicBookAssets(string comicBookId)
    {
        try
        {
            var response = await _comicBookService.GetComicBookAssetsAsync(comicBookId);
            return Ok(ApiResponse<IEnumerable<AssetResponse>>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comic book assets");
            return StatusCode(500, ApiResponse<IEnumerable<AssetResponse>>.Failure(
                "An error occurred while retrieving the comic book assets",
                "ASSETS_RETRIEVE_ERROR",
                ex.Message));
        }
    }

    [HttpGet("status/{assetId}")]
    public async Task<ActionResult<ApiResponse<ComicBookStatusResponse>>> GetComicBookStatus(Guid assetId)
    {
        try
        {
            var asset = await _comicBookService.GetAssetAsync(assetId.ToString());
            if (asset == null)
            {
                return NotFound(ApiResponse<ComicBookStatusResponse>.Failure("Asset not found", "ASSET_NOT_FOUND"));
            }

            int progress = asset.Status switch
            {
                "Pending" => 0,
                "In Progress" => 50, // Example, can be dynamically calculated
                "Completed" => 100,
                "Failed" => 0,
                _ => 0
            };

            string estimatedTime = asset.Status == "In Progress" ? "2-3 minutes remaining" : null;

            var response = new ComicBookStatusResponse
            {
                Status = asset.Status,
                Progress = progress,
                EstimatedTimeRemaining = estimatedTime,
                Message = asset.Status == "Failed" ? "An error occurred. Please retry." : null,
                AssetId = asset.AssetId
            };

            return Ok(ApiResponse<ComicBookStatusResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comic book status");
            return StatusCode(500, ApiResponse<ComicBookStatusResponse>.Failure(
                "Error retrieving status",
                "STATUS_ERROR",
                ex.Message));
        }
    }

    [HttpPost("generate/{assetId}")]
    public async Task<ActionResult<ApiResponse<bool>>> GenerateComicBook(Guid assetId)
    {
        try
        {
            var result = await _comicBookService.GenerateComicBookAsync(assetId);
            return Ok(ApiResponse<bool>.Success(result));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Asset not found");
            return NotFound(ApiResponse<bool>.Failure(
                "Asset not found",
                "ASSET_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comic book");
            return StatusCode(500, ApiResponse<bool>.Failure(
                "An error occurred while generating the comic book",
                "COMIC_GENERATE_ERROR",
                ex.Message));
        }
    }

    // Install packages
    // dotnet add package DinkToPdf
    // dotnet add package DinkToPdf.Natives.Linux
    // For Windows: DinkToPdf.Natives.Win

    [HttpGet("generate-pdf/{assetId}")]
    public async Task<ActionResult<ApiResponse<string>>> GenerateComicBookPdf(string assetId)
    {
        try
        {
            var asset = await _comicBookService.GetAssetAsync(assetId);
            if (asset == null)
            {
                return NotFound(ApiResponse<string>.Failure("Asset not found", "ASSET_NOT_FOUND"));
            }

            if (string.IsNullOrWhiteSpace(asset.FullStoryText))
            {
                return BadRequest(ApiResponse<string>.Failure("No content available for PDF generation", "NO_CONTENT"));
            }

            // Generate PDF
            var htmlContent = asset.FullStoryText;

            // Make image paths absolute for PDF generation
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            htmlContent = htmlContent.Replace("src=\"/", $"src=\"{baseUrl}/");

            // Add custom styles for PDF
            var styledHtml = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Comic Book</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
                img {{ max-width: 100%; height: auto; margin: 10px 0; }}
                h1 {{ color: #333; }}
                p {{ line-height: 1.6; }}
            </style>
        </head>
        <body>
            {htmlContent}
        </body>
        </html>";

            // Initialize converter
            var converter = new BasicConverter(new PdfTools());
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 },
                DocumentTitle = "Comic Book"
            },
                Objects = {
                new ObjectSettings
                {
                    HtmlContent = styledHtml,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
            };

            // Generate PDF bytes
            byte[] pdfBytes = converter.Convert(doc);

            // Create unique filename
            string fileName = $"Comic_{asset.ComicBookId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            string pdfDirectory = Path.Combine("wwwroot", "pdfs");

            // Ensure directory exists
            if (!Directory.Exists(pdfDirectory))
            {
                Directory.CreateDirectory(pdfDirectory);
            }

            string pdfPath = Path.Combine(pdfDirectory, fileName);
            await System.IO.File.WriteAllBytesAsync(pdfPath, pdfBytes);

            // Save the PDF path to the asset
            var pdfWebPath = $"/pdfs/{fileName}";

            // Update the asset with the PDF path
            var updateRequest = new AssetUpdateRequest
            {
                FilePath = pdfWebPath,
                Status = "COMPLETED"
            };

            await _comicBookService.UpdateAssetAsync(assetId, updateRequest);

            // Return the URL to the generated PDF
            return Ok(ApiResponse<string>.Success($"/pdfs/{fileName}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for asset {AssetId}", assetId);
            return StatusCode(500, ApiResponse<string>.Failure(
                "An error occurred while generating the PDF",
                "PDF_GENERATION_ERROR",
                ex.Message));
        }
    }

    [HttpGet("assets/{assetId}/details")]
    public async Task<ActionResult<ApiResponse<AssetDetailsResponse>>> GetAssetDetails(string assetId)
    {
        try
        {
            var asset = await _comicBookService.GetAssetAsync(assetId);

            // Create a response with the specific fields needed for viewing
            var response = new AssetDetailsResponse
            {
                AssetId = asset.AssetId,
                ComicBookId = asset.ComicBookId,
                FilePath = asset.FilePath,
                FullStoryText = asset.FullStoryText,
                Status = asset.Status
            };

            return Ok(ApiResponse<AssetDetailsResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<AssetDetailsResponse>.Failure(
                "Asset not found",
                "ASSET_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset details");
            return StatusCode(500, ApiResponse<AssetDetailsResponse>.Failure(
                "An error occurred while retrieving asset details",
                "ASSET_DETAILS_ERROR",
                ex.Message));
        }
    }

}
