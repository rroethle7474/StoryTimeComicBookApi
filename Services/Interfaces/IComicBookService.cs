using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;

namespace StoryTimeComicBookApi.Services.Interfaces;

public interface IComicBookService
{
    Task<ComicBookCreateResponse> CreateComicBookAsync(ComicBookCreateRequest request);
    Task<ComicBookGetResponse> GetComicBookAsync(string comicBookId);
    Task<ComicBookUpdateResponse> UpdateComicBookAsync(string comicBookId, ComicBookUpdateRequest request);
    Task<ComicBookDeleteResponse> DeleteComicBookAsync(string comicBookId);
    Task<SceneCreateResponse> CreateSceneAsync(SceneCreateRequest request);
    Task<SceneGetResponse> GetSceneAsync(string sceneId);
    Task<SceneUpdateResponse> UpdateSceneAsync(string sceneId, SceneUpdateRequest request);
    Task<SceneDeleteResponse> DeleteSceneAsync(string sceneId);
    IAsyncEnumerable<GenerateStoryResponse> GenerateSceneStoryAsync(GenerateStoryRequest request);
    Task<IEnumerable<ComicBookListResponse>> GetIncompleteComicBooksAsync();
    Task<IEnumerable<SceneGetResponse>> GetScenesAsync(string comicBookId);

    // Asset management methods
    Task<AssetResponse> CreateAssetAsync(AssetCreateRequest request);
    Task<AssetResponse> GetAssetAsync(string assetId);
    Task<AssetResponse> UpdateAssetAsync(string assetId, AssetUpdateRequest request);
    Task<bool> DeleteAssetAsync(string assetId);
    Task<IEnumerable<AssetResponse>> GetComicBookAssetsAsync(string comicBookId);
} 