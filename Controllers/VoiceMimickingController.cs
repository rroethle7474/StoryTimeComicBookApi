using Microsoft.AspNetCore.Mvc;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Interfaces;
using StoryTimeComicBookApi.Models.Common;
using StoryTimeComicBookApi.Services;

namespace StoryTimeComicBookApi.Controllers;

[ApiController]
[Route("api/voice-mimic")]
public class VoiceMimickingController : ControllerBase
{
    private readonly IVoiceMimickingService _voiceMimickingService;
    private readonly ILogger<VoiceMimickingController> _logger;

    public VoiceMimickingController(IVoiceMimickingService voiceMimickingService, ILogger<VoiceMimickingController> logger)
    {
        _voiceMimickingService = voiceMimickingService;
        _logger = logger;
    }

    [HttpPost("start-recording")]
    public ActionResult<ApiResponse<StartRecordingResponse>> StartRecording()
    {
        try
        {
            var response = _voiceMimickingService.StartRecording();
            return Ok(ApiResponse<StartRecordingResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting recording session");
            return StatusCode(500, ApiResponse<StartRecordingResponse>.Failure(
                "An error occurred while starting the recording session",
                "RECORDING_START_ERROR",
                ex.Message));
        }
    }

    [HttpPost("upload-snippet")]
    public async Task<ActionResult<ApiResponse<AudioSnippetUploadResponse>>> UploadSnippet([FromForm] AudioSnippetUploadRequest request)
    {
        try
        {
            var response = await _voiceMimickingService.UploadAudioSnippetAsync(request);
            return Ok(ApiResponse<AudioSnippetUploadResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading audio snippet");
            return StatusCode(500, ApiResponse<AudioSnippetUploadResponse>.Failure(
                "An error occurred while uploading the audio snippet",
                "UPLOAD_SNIPPET_ERROR",
                ex.Message));
        }
    }

    [HttpPost("train-model")]
    public async Task<ActionResult<ApiResponse<TrainModelResponse>>> TrainModel([FromBody] TrainModelRequest request)
    {
        try
        {
            var response = await _voiceMimickingService.TrainModelAsync(request);
            return Ok(ApiResponse<TrainModelResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training voice model");
            return StatusCode(500, ApiResponse<TrainModelResponse>.Failure(
                "An error occurred while training the voice model",
                "VOICE_TRAIN_ERROR",
                ex.Message));
        }
    }

    [HttpPost("synthesize-speech")]
    public async Task<ActionResult<ApiResponse<SynthesizeSpeechResponse>>> SynthesizeSpeech([FromBody] SynthesizeSpeechRequest request)
    {
        try
        {
            var response = await _voiceMimickingService.SynthesizeSpeechAsync(request);
            return Ok(ApiResponse<SynthesizeSpeechResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing speech");
            return StatusCode(500, ApiResponse<SynthesizeSpeechResponse>.Failure(
                "An error occurred while synthesizing speech",
                "SPEECH_SYNTHESIS_ERROR",
                ex.Message));
        }
    }

    [HttpPost("create-voice-model")]
    public async Task<ActionResult<ApiResponse<CreateVoiceModelResponse>>> CreateVoiceModel([FromBody] CreateVoiceModelRequest request)
    {
        try
        {
            var response = await _voiceMimickingService.CreateVoiceModelAsync(request);
            return Ok(ApiResponse<CreateVoiceModelResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new Voice Model");
            return StatusCode(500, ApiResponse<CreateVoiceModelResponse>.Failure(
                "An error occurred while creating voice model",
                "VOICE_MODEL_ERROR",
                ex.Message));
        }
    }


    [HttpPut("{voiceModelId}")]
    public async Task<ActionResult<ApiResponse<VoiceModelUpdateResponse>>> UpdateVoiceModel(string voiceModelId, [FromBody] VoiceModelUpdateRequest request)
    {
        try
        {
            var response = await _voiceMimickingService.UpdateVoiceModelAsync(voiceModelId, request);
            return Ok(ApiResponse<VoiceModelUpdateResponse>.Success(response));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Voice model not found");
            return NotFound(ApiResponse<VoiceModelUpdateResponse>.Failure(
                "Voice model not found",
                "VOICE_MODEL_NOT_FOUND",
                ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating voice model");
            return StatusCode(500, ApiResponse<VoiceModelUpdateResponse>.Failure(
                "An error occurred while updating the voice model",
                "VOICE_MODEL_UPDATE_ERROR",
                ex.Message));
        }
    }

    [HttpGet("incomplete")]
    public async Task<ActionResult<ApiResponse<IEnumerable<VoiceModelListResponse>>>> GetIncompleteVoiceModels()
    {
        try
        {
            var response = await _voiceMimickingService.GetIncompleteVoiceModelsAsync();
            return Ok(ApiResponse<IEnumerable<VoiceModelListResponse>>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving incomplete voice models");
            return StatusCode(500, ApiResponse<IEnumerable<VoiceModelListResponse>>.Failure(
                "An error occurred while retrieving incomplete voice models",
                "VOICE_MODEL_ERROR",
                ex.Message));
        }
    }
}
