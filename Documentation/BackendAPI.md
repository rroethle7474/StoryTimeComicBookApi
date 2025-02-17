## 4. Detailed Backend API Design (C# .NET)

This section outlines the design of the backend API, built using C# .NET, for the AI Comic Book Generator.

### 4.1. API Endpoints

We will use RESTful API principles for communication between the Angular frontend and the C# backend.  Here's a breakdown of the API endpoints:

| Endpoint                      | HTTP Method | Request Body                                       | Response Body                                                                 | Description                                                                  |
| :---------------------------- | :---------- | :------------------------------------------------- | :-------------------------------------------------------------------------- | :--------------------------------------------------------------------------- |
| `/api/comicbook/create`       | POST        | `ComicBookCreateRequest`                             | `ComicBookCreateResponse`                                                     | Creates a new comic book project.                                         |
| `/api/comicbook/{comicBookId}` | GET         | None                                                 | `ComicBookGetResponse`                                                      | Retrieves a specific comic book project by ID.                             |
| `/api/comicbook/{comicBookId}` | PUT         | `ComicBookUpdateRequest`                             | `ComicBookUpdateResponse`                                                     | Updates an existing comic book project.                                      |
| `/api/comicbook/{comicBookId}` | DELETE      | None                                                 | `ComicBookDeleteResponse`                                                     | Deletes a comic book project.                                            |
| `/api/comicbook/{comicBookId}/scene` | POST        | `SceneCreateRequest`                                 | `SceneCreateResponse`                                                         | Adds a new scene to a comic book.                                         |
| `/api/comicbook/{comicBookId}/scene/{sceneId}` | GET         | None                                                 | `SceneGetResponse`                                                          | Retrieves a specific scene within a comic book.                             |
| `/api/comicbook/{comicBookId}/scene/{sceneId}` | PUT         | `SceneUpdateRequest`                                 | `SceneUpdateResponse`                                                         | Updates an existing scene within a comic book.                               |
| `/api/comicbook/{comicBookId}/scene/{sceneId}` | DELETE      | None                                                 | `SceneDeleteResponse`                                                         | Deletes a scene from a comic book.                                         |
| `/api/comicbook/{comicBookId}/scene/{sceneId}/generate-story` | POST        | `GenerateStoryRequest`                               | `GenerateStoryResponse` (streamed)                                        | Generates story text for a scene using AI (streamed response).            |
| `/api/voice-mimic/start-recording` | POST        | None                                                 | `StartRecordingResponse`                                                      | Starts a new voice recording session (might be session ID).              |
| `/api/voice-mimic/upload-snippet`  | POST        | `AudioSnippetUploadRequest` (multipart/form-data) | `AudioSnippetUploadResponse`                                                  | Uploads an audio snippet for voice training.                               |
| `/api/voice-mimic/train-model`     | POST        | `TrainModelRequest`                                  | `TrainModelResponse`                                                        | Triggers the text-to-speech model training process.                       |
| `/api/voice-mimic/synthesize-speech` | POST        | `SynthesizeSpeechRequest`                            | `SynthesizeSpeechResponse` (audio file or URL)                               | Synthesizes speech from text using the trained voice model.                |

**Notes:**

*   `{comicBookId}` and `{sceneId}` in the paths represent placeholders for unique identifiers.
*   Streamed responses for story generation are crucial for providing a responsive user experience as AI models generate text.
*   For audio uploads (`/api/voice-mimic/upload-snippet`), `multipart/form-data` is used to handle file uploads efficiently.

### 4.2. Data Transfer Objects (DTOs)

These are the data structures (C# classes) that will be used for request and response bodies in the API.  We'll use clear and descriptive names.

#### Comic Book DTOs

```csharp
// Request DTOs
public class ComicBookCreateRequest
{
    public string Title { get; set; }
    public string Description { get; set; }
}

public class ComicBookUpdateRequest
{
    public string? Title { get; set; } // Nullable for partial updates
    public string? Description { get; set; }
}

public class SceneCreateRequest
{
    public string ComicBookId { get; set; } // Link to ComicBook
    public int SceneOrder { get; set; } // Order within the comic book
    public string? ImagePath { get; set; } // Path to stored image
    public string? UserDescription { get; set; } // User-provided scene description
}

public class SceneUpdateRequest
{
    public string? ImagePath { get; set; }
    public string? UserDescription { get; set; }
    public string? AiGeneratedStory { get; set; } // Allow updating AI story if needed
}

public class GenerateStoryRequest
{
    public string SceneId { get; set; } // Identify the scene to generate story for
    public string UserDescription { get; set; } // Pass the scene description again for context
}


// Response DTOs
public class ComicBookCreateResponse
{
    public string ComicBookId { get; set; }
    public string Title { get; set; }
}

public class ComicBookGetResponse
{
    public string ComicBookId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<SceneGetResponse> Scenes { get; set; } // List of scenes in the comic book
}

public class ComicBookUpdateResponse
{
    public string ComicBookId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}

public class ComicBookDeleteResponse
{
    public string ComicBookId { get; set; }
    public bool IsDeleted { get; set; } // Indicate successful deletion
}

public class SceneCreateResponse
{
    public string SceneId { get; set; }
    public int SceneOrder { get; set; }
}

public class SceneGetResponse
{
    public string SceneId { get; set; }
    public int SceneOrder { get; set; }
    public string? ImagePath { get; set; }
    public string? UserDescription { get; set; }
    public string? AiGeneratedStory { get; set; }
}

public class SceneUpdateResponse
{
    public string SceneId { get; set; }
    public int SceneOrder { get; set; }
    public string? ImagePath { get; set; }
    public string? UserDescription { get; set; }
    public string? AiGeneratedStory { get; set; }
}

public class SceneDeleteResponse
{
    public string SceneId { get; set; }
    public bool IsDeleted { get; set; }
}

public class GenerateStoryResponse
{
    public string SceneId { get; set; }
    public string StoryTextChunk { get; set; } // For streaming, send chunks of text
    public bool IsComplete { get; set; } // Indicate end of stream
}
Voice Mimicking DTOs
C#

// Request DTOs
public class StartRecordingRequest { } // Empty request, might be extended later

public class AudioSnippetUploadRequest
{
    public IFormFile AudioFile { get; set; } // For file upload
    // Could add user or session identifier here if needed later
}

public class TrainModelRequest
{
    // Potentially add parameters for training customization later
}

public class SynthesizeSpeechRequest
{
    public string TextToSynthesize { get; set; }
    // Potentially add voice profile identifier if supporting multiple voices later
}


// Response DTOs
public class StartRecordingResponse
{
    public string RecordingSessionId { get; set; } // If needed to track sessions
    public string Message { get; set; } = "Recording session started.";
}

public class AudioSnippetUploadResponse
{
    public string Message { get; set; } = "Audio snippet uploaded successfully.";
    // Could return snippet identifier or path if needed
}

public class TrainModelResponse
{
    public string Message { get; set; } = "Model training initiated.";
    // Could return training job ID or status URL if using async training
}

public class SynthesizeSpeechResponse
{
    public string AudioUrl { get; set; } // URL to access the synthesized audio file
    // Alternatively, could return the audio file directly as a byte stream
}
Notes:

These DTOs are defined in C# syntax. You'll create corresponding classes in your .NET backend project.
For AudioSnippetUploadRequest, IFormFile is used to handle file uploads in ASP.NET Core.
GenerateStoryResponse and SynthesizeSpeechResponse are designed to handle streaming or file URLs, respectively, for potentially large text and audio data.
We've kept the DTOs relatively simple for now. We can always expand them as we add more features.
4.3. Service Layer Design
The backend will have two main service layers: ComicBookService and VoiceMimickingService. These services will encapsulate the business logic and orchestrate interactions with AI APIs and the database.

ComicBookService
Responsibilities:
Handles comic book project creation, retrieval, updating, and deletion.
Manages scenes within comic books (creation, retrieval, updating, deletion).
Orchestrates story generation for scenes:
Receives scene description from the API controller.
Selects and interacts with the configured AI Story Generation API (initially Gemini).
Handles streaming responses from the AI API and sends chunks back to the frontend.
Stores the generated story text in the database, associated with the scene.
Methods (Conceptual):
CreateComicBookAsync(ComicBookCreateRequest request)
GetComicBookAsync(string comicBookId)
UpdateComicBookAsync(string comicBookId, ComicBookUpdateRequest request)
DeleteComicBookAsync(string comicBookId)
CreateSceneAsync(SceneCreateRequest request)
GetSceneAsync(string sceneId)
UpdateSceneAsync(string sceneId, SceneUpdateRequest request)
DeleteSceneAsync(string sceneId)
GenerateSceneStoryAsync(GenerateStoryRequest request, IResponseStreamWriter streamWriter) // Uses a stream writer for streaming response
VoiceMimickingService
Responsibilities:
Manages voice recording sessions (if session tracking is needed).
Handles upload and storage of audio snippets.
Initiates and manages text-to-speech model training:
Interacts with the chosen Text-to-Speech platform API (Replicate, Hugging Face, etc.).
Handles API calls to trigger model training using uploaded audio snippets.
Potentially monitors training status (if the platform provides APIs for this).
Synthesizes speech from text using a trained voice model:
Receives text to synthesize.
Selects and interacts with the trained TTS model (either deployed locally or via an API).
Returns the synthesized audio (as a URL or file stream).
Methods (Conceptual):
StartRecordingSessionAsync() // If session management is needed
UploadAudioSnippetAsync(AudioSnippetUploadRequest request)
TrainVoiceModelAsync(TrainModelRequest request)
SynthesizeSpeechAsync(SynthesizeSpeechRequest request)

4.4. AI API Integration Strategy (Story Generation)

The API uses a client-based strategy for integrating with various Large Language Models (LLMs) like Gemini, OpenAI, etc. This approach provides flexibility and configuration-driven provider selection.

### Architecture Overview

1. **Core Components**
   - `IAiStoryGenerator`: Single interface for story generation
   - `ILlmClient`: Interface for LLM provider clients
   - `AiStoryGenerator`: Main implementation of story generation logic
   - Provider-specific clients (e.g., `GeminiApiClient`, `OpenAiApiClient`)
   - `LlmClientFactory`: Configuration-based client selection

2. **Component Relationships**
```
ComicBookService → AiStoryGenerator → LlmClientFactory → Specific LLM Client
```

### Key Interfaces

```csharp
public interface IAiStoryGenerator
{
    IAsyncEnumerable<string> GenerateStoryAsync(string sceneDescription);
}

public interface ILlmClient
{
    IAsyncEnumerable<string> GenerateContentStreamAsync(string prompt);
}
```

### Configuration
```json
{
  "AI": {
    "LlmType": "Gemini",  // Determines which LLM provider to use
    "Gemini": {
      "ApiKey": "your-gemini-api-key"
    },
    "OpenAI": {
      "ApiKey": "your-openai-key"
    }
  }
}
```

### Implementation Details

1. **AiStoryGenerator**
   - Implements `IAiStoryGenerator`
   - Uses `ILlmClient` for provider-agnostic API calls
   - Handles story generation logic and error handling
   - Maintains streaming capability

2. **LLM Clients**
   - Implement `ILlmClient`
   - Handle provider-specific API communication
   - Convert between generic prompts and provider-specific formats
   - Manage API authentication and request formatting

3. **Factory Pattern**
   - Provides configuration-based client selection
   - Manages client instantiation and dependencies
   - Enables easy addition of new LLM providers

### Benefits
1. **Separation of Concerns**
   - Story generation logic separate from API communication
   - Provider-specific code isolated in client implementations
   - Clean dependency injection

2. **Flexibility**
   - Easy to add new LLM providers
   - Configuration-driven provider selection
   - No code changes needed to switch providers

3. **Maintainability**
   - Single responsibility principle
   - Provider-agnostic core logic
   - Centralized error handling

### Adding New Providers
To add support for a new LLM:
1. Create new client implementing `ILlmClient`
2. Add provider to `LlmClientFactory`
3. Update configuration schema

### Usage Example
```csharp
public class ComicBookService
{
    private readonly IAiStoryGenerator _storyGenerator;

    public async IAsyncEnumerable<GenerateStoryResponse> GenerateSceneStoryAsync(
        GenerateStoryRequest request)
    {
        await foreach (var chunk in _storyGenerator.GenerateStoryAsync(
            request.UserDescription))
        {
            yield return new GenerateStoryResponse
            {
                SceneId = request.SceneId,
                StoryTextChunk = chunk,
                IsComplete = false
            };
        }
    }
}
```