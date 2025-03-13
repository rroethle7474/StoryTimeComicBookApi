# StoryTimeComicBookApi

## Project Description
This API powers the StoryTimeComicBook App, enabling users to create personalized comic books using their own images and story ideas. The API handles:

- Comic book creation with user-uploaded images
- AI-assisted story generation based on scene descriptions
- Image transformation to convert regular images into comic book style art
- PDF compilation of the final comic book
- Voice model training capabilities using user audio recordings

A voice model trainer is available for users to submit audio snippets of their voice; however, the training of this model on Replicate is not fully completed at this time (as of March 13, 2025).

Future development will allow the fine-tuned audio model to read the story back to users while they view their finished comic.

## Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- [PostgreSQL](https://www.postgresql.org/download/) (version 13 or newer recommended)
- [FFmpeg](https://ffmpeg.org/download.html) (required for audio processing)
- API keys (see API Requirements section)

## API Requirements

This API requires the following API keys to function properly:

1. **LLM API Key** - Choose one of:
   - [Gemini API](https://ai.google.dev/) (recommended default)
   - [OpenAI API](https://openai.com/blog/openai-api)
   - [Anthropic API](https://www.anthropic.com/api)

2. **Replicate API Keys**:
   - [Replicate](https://replicate.com/) - For image processing and voice model training
   - You'll need both the Image API key and Audio API key

## Setup Instructions

### 1. Clone the Repository

### 2. Database Setup

The application uses Entity Framework Core's code-first approach and will automatically create the database schema on first run. You just need to:

1. Install PostgreSQL if not already installed
2. Create an empty database called `ComicBookGeneratorDB` (or your preferred name)
3. Update the connection string in the configuration (see step 3)

### 3. Configuration

1. Copy the template configuration file:
   ```bash
   cp appsettings.template.json appsettings.json
   ```

2. Update the following in `appsettings.json`:
   - Database connection string
   - API keys for your chosen LLM provider (Gemini, OpenAI, or Anthropic)
   - Replicate API keys
   - NOT USED AT THIS TIME BUT COULD BE IN THE FUTURE: Hugging Face API configuration (if using voice features)

Example connection string format:
```
"ConnectionStrings": {
  "ComicBookGeneratorDbConnection": "Host=localhost;Port=5432;Database=ComicBookGeneratorDB;Username=youruser;Password=yourpassword;"
}
```

### 4. FFmpeg Installation

FFmpeg is required for audio processing. Make sure it's installed and available in your system PATH, or specify the path in the `appsettings.json` file:

```json
"FFmpeg": {
  "Path": "/path/to/ffmpeg"
}
```

### 5. Run the Application

```bash
dotnet run
```

### 6. Frontend Integration

This API is designed to work with the StoryTimeComicBook Angular UI, which can be found at:
https://github.com/rroethle7474/comic-book-generator

Please refer to the README in that repository for instructions on setting up the frontend application.

## Project Structure

- **Controllers/**: API endpoints
- **Data/**: Entity Framework models and database context
- **Models/**: Request/response models 
- **Services/**: Business logic implementation
  - **AI/**: AI-based generation services
  - **Clients/**: External API clients (LLM, Replicate, etc.)
- **wwwroot/**: Storage for uploaded and generated files

## API Endpoints

### Comic Book Management
- `POST /api/comicbook/create`: Create a new comic book
- `GET /api/comicbook/{id}`: Get comic book details
- `PUT /api/comicbook/{id}`: Update comic book details
- `DELETE /api/comicbook/{id}`: Delete a comic book

### Scenes
- `POST /api/comicbook/{id}/scene`: Add a scene to a comic book
- `GET /api/comicbook/{id}/scenes`: Get all scenes for a comic book
- `POST /api/comicbook/{id}/scene/{sceneId}/generate-story`: Generate story for a scene

### Assets
- `POST /api/comicbook/{id}/assets`: Create asset (FULL_STORY, STYLED_IMAGE, PDF)
- `POST /api/comicbook/generate/{assetId}`: Generate comic book PDF

### Voice Model
- `POST /api/voice-mimic/create-voice-model`: Create a new voice model
- `GET /api/voice-mimic/steps`: Get recording steps
- `POST /api/voice-mimic/voice-model/{id}/step/{stepId}/recording`: Upload recording for a step
- `POST /api/voice-mimic/voice-model/{id}/train`: Train voice model
- `POST /api/voice-mimic/synthesize/{id}`: Synthesize speech with a voice model

## Data Storage

All files (images, audio recordings, generated PDFs) are stored locally in the `wwwroot` directory structure:
- `/uploads/scenes/`: Original scene images
- `/uploads/audio/`: Voice recordings
- `/comics/{comicBookId}/`: Styled comic images
- `/pdfs/`: Generated comic book PDFs

## Troubleshooting

- **Database Connection Issues**: Verify your PostgreSQL connection string and ensure the server is running
- **API Key Errors**: Double-check your API keys in appsettings.json
- **Image Generation Failures**: Ensure your Replicate API key has enough credits/permissions
- **Audio Processing Errors**: Verify FFmpeg is correctly installed and accessible