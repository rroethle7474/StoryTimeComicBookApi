using Microsoft.AspNetCore.Mvc;
using StoryTimeComicBookApi.Models.Requests;
using StoryTimeComicBookApi.Models.Responses;
using StoryTimeComicBookApi.Services.Interfaces;

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
    public ActionResult<StartRecordingResponse> StartRecording()
    {
        try
        {
            var response = _voiceMimickingService.StartRecording();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting recording session");
            return StatusCode(500, "An error occurred while starting the recording session");
        }
    }

    [HttpPost("upload-snippet")]
    public async Task<ActionResult<AudioSnippetUploadResponse>> UploadSnippet([FromForm] AudioSnippetUploadRequest request)
    {
        try
        {
            var response = await _voiceMimickingService.UploadAudioSnippetAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading audio snippet");
            return StatusCode(500, "An error occurred while uploading the audio snippet");
        }
    }

    [HttpPost("train-model")]
    public async Task<ActionResult<TrainModelResponse>> TrainModel([FromBody] TrainModelRequest request)
    {
        try
        {
            var response = await _voiceMimickingService.TrainModelAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training voice model");
            return StatusCode(500, "An error occurred while training the voice model");
        }
    }

    [HttpPost("synthesize-speech")]
    public async Task<ActionResult<SynthesizeSpeechResponse>> SynthesizeSpeech([FromBody] SynthesizeSpeechRequest request)
    {
        try
        {
            var response = await _voiceMimickingService.SynthesizeSpeechAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing speech");
            return StatusCode(500, "An error occurred while synthesizing speech");
        }
    }
}
