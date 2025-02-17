# StoryTime Comic Book API - Frontend Integration Guide

## Overview
This document provides comprehensive documentation for integrating with the StoryTime Comic Book API from the Angular frontend application. The API provides endpoints for managing comic books, scenes, and voice mimicking features.

**Base URL**: `https://localhost:7049`

## Table of Contents
1. [Comic Book Management](#comic-book-management)
2. [Scene Management](#scene-management)
3. [Voice Mimicking](#voice-mimicking)
4. [Angular Service Examples](#angular-service-examples)

## Comic Book Management

### Create Comic Book
Creates a new comic book project.

**Endpoint**: `POST /api/ComicBook/create`

**Request Body**:
```typescript
interface ComicBookCreateRequest {
    title: string;
    description: string;
}
```

**Response**:
```typescript
interface ComicBookCreateResponse {
    comicBookId: string;
    title: string;
}
```

**Example Usage**:
```typescript
// Example request
const request = {
    title: "My First Comic",
    description: "An exciting adventure story"
};

// Example response
{
    "comicBookId": "550e8400-e29b-41d4-a716-446655440000",
    "title": "My First Comic"
}
```

### Get Comic Book
Retrieves a specific comic book and its scenes.

**Endpoint**: `GET /api/ComicBook/{comicBookId}`

**Parameters**:
- `comicBookId`: string (UUID) - The ID of the comic book to retrieve

**Response**:
```typescript
interface ComicBookGetResponse {
    comicBookId: string;
    title: string;
    description: string | null;
    scenes: SceneGetResponse[];
}

interface SceneGetResponse {
    sceneId: string;
    sceneOrder: number;
    imagePath: string | null;
    userDescription: string | null;
    aiGeneratedStory: string | null;
}
```

**Example Response**:
```json
{
    "comicBookId": "550e8400-e29b-41d4-a716-446655440000",
    "title": "My First Comic",
    "description": "An exciting adventure story",
    "scenes": [
        {
            "sceneId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
            "sceneOrder": 1,
            "imagePath": "/images/scene1.jpg",
            "userDescription": "A hero standing on a mountain top",
            "aiGeneratedStory": "The morning sun cast long shadows..."
        }
    ]
}
```

### Update Comic Book
Updates an existing comic book's information.

**Endpoint**: `PUT /api/ComicBook/{comicBookId}`

**Parameters**:
- `comicBookId`: string (UUID) - The ID of the comic book to update

**Request Body**:
```typescript
interface ComicBookUpdateRequest {
    title?: string;
    description?: string;
}
```

**Response**:
```typescript
interface ComicBookUpdateResponse {
    comicBookId: string;
    title: string;
    description: string | null;
}
```

**Example Usage**:
```typescript
// Example request
const request = {
    title: "My Updated Comic",
    description: "A new description"
};

// Example response
{
    "comicBookId": "550e8400-e29b-41d4-a716-446655440000",
    "title": "My Updated Comic",
    "description": "A new description"
}
```

### Delete Comic Book
Deletes a comic book and all its associated scenes.

**Endpoint**: `DELETE /api/ComicBook/{comicBookId}`

**Parameters**:
- `comicBookId`: string (UUID) - The ID of the comic book to delete

**Response**:
```typescript
interface ComicBookDeleteResponse {
    comicBookId: string;
    isDeleted: boolean;
}
```

**Example Response**:
```json
{
    "comicBookId": "550e8400-e29b-41d4-a716-446655440000",
    "isDeleted": true
}
```

# Scene Management

## Create Scene
Adds a new scene to an existing comic book.

**Endpoint**: `POST /api/ComicBook/{comicBookId}/scene`

**Parameters**:
- `comicBookId`: string (UUID) - The ID of the comic book to add the scene to

**Request Body**:
```typescript
interface SceneCreateRequest {
    comicBookId: string;   // Must match the comicBookId in the URL
    sceneOrder: number;    // Position in the comic book
    imagePath?: string;    // Optional path to the scene image
    userDescription?: string; // Optional description of the scene
}
```

**Response**:
```typescript
interface SceneCreateResponse {
    sceneId: string;
    sceneOrder: number;
    imagePath?: string;
    userDescription?: string;
}
```

**Example Usage**:
```typescript
// Example request
const request = {
    comicBookId: "550e8400-e29b-41d4-a716-446655440000",
    sceneOrder: 1,
    imagePath: "/uploads/scene1.jpg",
    userDescription: "A brave hero stands atop a mountain, surveying the landscape below"
};

// Example response
{
    "sceneId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "sceneOrder": 1,
    "imagePath": "/uploads/scene1.jpg",
    "userDescription": "A brave hero stands atop a mountain, surveying the landscape below"
}
```

## Get Scene
Retrieves a specific scene from a comic book.

**Endpoint**: `GET /api/ComicBook/{comicBookId}/scene/{sceneId}`

**Parameters**:
- `comicBookId`: string (UUID) - The ID of the comic book
- `sceneId`: string (UUID) - The ID of the scene to retrieve

**Response**:
```typescript
interface SceneGetResponse {
    sceneId: string;
    sceneOrder: number;
    imagePath?: string;
    userDescription?: string;
    aiGeneratedStory?: string;
}
```

**Example Response**:
```json
{
    "sceneId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "sceneOrder": 1,
    "imagePath": "/uploads/scene1.jpg",
    "userDescription": "A brave hero stands atop a mountain",
    "aiGeneratedStory": "As the morning sun cast its golden rays across the valley..."
}
```

## Update Scene
Updates an existing scene within a comic book.

**Endpoint**: `PUT /api/ComicBook/{comicBookId}/scene/{sceneId}`

**Parameters**:
- `comicBookId`: string (UUID) - The ID of the comic book
- `sceneId`: string (UUID) - The ID of the scene to update

**Request Body**:
```typescript
interface SceneUpdateRequest {
    imagePath?: string;
    userDescription?: string;
    aiGeneratedStory?: string;
}
```

**Response**:
```typescript
interface SceneUpdateResponse {
    sceneId: string;
    sceneOrder: number;
    imagePath?: string;
    userDescription?: string;
    aiGeneratedStory?: string;
}
```

**Example Usage**:
```typescript
// Example request
const request = {
    userDescription: "Updated scene description",
    aiGeneratedStory: "The hero gazed across the landscape..."
};

// Example response
{
    "sceneId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "sceneOrder": 1,
    "imagePath": "/uploads/scene1.jpg",
    "userDescription": "Updated scene description",
    "aiGeneratedStory": "The hero gazed across the landscape..."
}
```

## Delete Scene
Removes a scene from a comic book.

**Endpoint**: `DELETE /api/ComicBook/{comicBookId}/scene/{sceneId}`

**Parameters**:
- `comicBookId`: string (UUID) - The ID of the comic book
- `sceneId`: string (UUID) - The ID of the scene to delete

**Response**:
```typescript
interface SceneDeleteResponse {
    sceneId: string;
    isDeleted: boolean;
}
```

**Example Response**:
```json
{
    "sceneId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "isDeleted": true
}
```

## Generate Story
Generates an AI-powered story for a specific scene. This endpoint returns a stream of story chunks.

**Endpoint**: `POST /api/ComicBook/{comicBookId}/scene/{sceneId}/generate-story`

**Parameters**:
- `comicBookId`: string (UUID) - The ID of the comic book
- `sceneId`: string (UUID) - The ID of the scene

**Request Body**:
```typescript
interface GenerateStoryRequest {
    sceneId: string;       // Must match the sceneId in the URL
    userDescription: string; // Description to base the story on
}
```

**Response Stream**:
```typescript
interface GenerateStoryResponse {
    sceneId: string;
    storyTextChunk: string;
    isComplete: boolean;
}
```

**Example Usage**:
```typescript
// Example request
const request = {
    sceneId: "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    userDescription: "A brave hero stands atop a mountain"
};

// Example response stream
[
    {
        "sceneId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "storyTextChunk": "As the morning sun rose over the distant peaks",
        "isComplete": false
    },
    {
        "sceneId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "storyTextChunk": ", our hero surveyed the vast landscape before them",
        "isComplete": false
    },
    {
        "sceneId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "storyTextChunk": ".",
        "isComplete": true
    }
]
```

**Note**: This endpoint streams the response, so you'll need to handle it appropriately in your Angular service. Consider using RxJS for proper stream handling.

# Voice Mimicking

The Voice Mimicking API provides endpoints for recording audio snippets, training voice models, and synthesizing speech using the trained models.

## Start Recording Session
Initializes a new voice recording session.

**Endpoint**: `POST /api/voice-mimic/start-recording`

**Request Body**: Empty (no request body needed)

**Response**:
```typescript
interface StartRecordingResponse {
    recordingSessionId: string;
    message: string;
}
```

**Example Response**:
```json
{
    "recordingSessionId": "8f7e6d5c-4b3a-2a1f-9e8d-7c6b5a4d3f2e",
    "message": "Recording session started."
}
```

## Upload Audio Snippet
Uploads a recorded audio snippet for voice training.

**Endpoint**: `POST /api/voice-mimic/upload-snippet`

**Request Body**: 
```typescript
// Must be sent as multipart/form-data
interface AudioSnippetUploadRequest {
    audioFile: File;  // The audio file to upload
}
```

**Response**:
```typescript
interface AudioSnippetUploadResponse {
    message: string;
}
```

**Example Usage**:
```typescript
// Example using FormData
const formData = new FormData();
formData.append('audioFile', audioFile);  // audioFile is a File object

// Example response
{
    "message": "Audio snippet uploaded successfully."
}
```

**Important Notes**:
- The request must be sent as `multipart/form-data`
- The file input name must be "audioFile"
- Consider implementing proper file size and format validation in your frontend

## Train Voice Model
Initiates the training process for a voice model using uploaded audio snippets.

**Endpoint**: `POST /api/voice-mimic/train-model`

**Request Body**:
```typescript
interface TrainModelRequest {
    // Currently empty, but may include training parameters in future versions
}
```

**Response**:
```typescript
interface TrainModelResponse {
    message: string;
}
```

**Example Response**:
```json
{
    "message": "Model training initiated."
}
```

**Important Notes**:
- Training is an asynchronous process
- The frontend should handle the possibility that training might take some time
- Consider implementing a way to check training status in future versions

## Synthesize Speech
Generates speech using the trained voice model.

**Endpoint**: `POST /api/voice-mimic/synthesize-speech`

**Request Body**:
```typescript
interface SynthesizeSpeechRequest {
    textToSynthesize: string;
}
```

**Response**:
```typescript
interface SynthesizeSpeechResponse {
    audioUrl: string;
}
```

**Example Usage**:
```typescript
// Example request
const request = {
    textToSynthesize: "Hello, this is a test of the voice synthesis system."
};

// Example response
{
    "audioUrl": "/api/audio/synthesized_12345.wav"
}
```

## Angular Service Example

Here's an example of how to implement these endpoints in an Angular service:

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class VoiceMimicService {
    private baseUrl = 'https://localhost:7049/api/voice-mimic';

    constructor(private http: HttpClient) {}

    startRecording(): Observable<StartRecordingResponse> {
        return this.http.post<StartRecordingResponse>
            (`${this.baseUrl}/start-recording`, {});
    }

    uploadAudioSnippet(audioFile: File): Observable<AudioSnippetUploadResponse> {
        const formData = new FormData();
        formData.append('audioFile', audioFile);
        
        return this.http.post<AudioSnippetUploadResponse>
            (`${this.baseUrl}/upload-snippet`, formData);
    }

    trainModel(): Observable<TrainModelResponse> {
        return this.http.post<TrainModelResponse>
            (`${this.baseUrl}/train-model`, {});
    }

    synthesizeSpeech(text: string): Observable<SynthesizeSpeechResponse> {
        return this.http.post<SynthesizeSpeechResponse>
            (`${this.baseUrl}/synthesize-speech`, { textToSynthesize: text });
    }
}
```

## Error Handling

All endpoints may return the following error responses:

- **400 Bad Request**: When the request is malformed or invalid
- **500 Internal Server Error**: When an unexpected error occurs on the server

Example error response:
```json
{
    "status": 500,
    "message": "An error occurred while synthesizing speech"
}
```

It's recommended to implement proper error handling in your Angular service:

```typescript
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

// In your service methods:
synthesizeSpeech(text: string): Observable<SynthesizeSpeechResponse> {
    return this.http.post<SynthesizeSpeechResponse>
        (`${this.baseUrl}/synthesize-speech`, { textToSynthesize: text })
        .pipe(
            catchError(error => {
                console.error('Error synthesizing speech:', error);
                return throwError(() => new Error('Failed to synthesize speech'));
            })
        );
}
```

# Angular Implementation Guidelines

This guide provides best practices and implementation guidelines for integrating with the StoryTime Comic Book API in your Angular application.

## Environment Setup

### 1. API Configuration
Create environment files for different deployment scenarios:

```typescript
// src/environments/environment.ts
export const environment = {
    production: false,
    apiBaseUrl: 'https://localhost:7049/api'
};

// src/environments/environment.prod.ts
export const environment = {
    production: true,
    apiBaseUrl: 'https://your-production-url/api'
};
```

### 2. Required Dependencies
Add these to your `package.json`:

```json
{
  "dependencies": {
    "@angular/common": "^17.x.x",
    "@angular/core": "^17.x.x",
    "rxjs": "^7.x.x"
  }
}
```

## Core Services Setup

### Base API Service
Create a base service for common API functionality:

```typescript
// src/app/core/services/api-base.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class ApiBaseService {
    protected baseUrl = environment.apiBaseUrl;

    constructor(protected http: HttpClient) {}

    protected handleError(error: HttpErrorResponse) {
        let errorMessage = 'An error occurred';
        
        if (error.error instanceof ErrorEvent) {
            // Client-side error
            errorMessage = error.error.message;
        } else {
            // Server-side error
            errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
        }
        
        console.error(errorMessage);
        return throwError(() => new Error(errorMessage));
    }
}
```

### HTTP Interceptor
Create an interceptor for handling API requests:

```typescript
// src/app/core/interceptors/api.interceptor.ts
import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable()
export class ApiInterceptor implements HttpInterceptor {
    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        // Only intercept requests to our API
        if (request.url.startsWith(environment.apiBaseUrl)) {
            request = request.clone({
                setHeaders: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                }
            });
        }
        
        return next.handle(request);
    }
}
```

Register the interceptor in your `app.module.ts`:

```typescript
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { ApiInterceptor } from './core/interceptors/api.interceptor';

@NgModule({
    // ...
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: ApiInterceptor,
            multi: true
        }
    ]
})
```

## Service Implementation Examples

### Comic Book Service
Example of a complete service implementation:

```typescript
// src/app/services/comic-book.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiBaseService } from '../core/services/api-base.service';

@Injectable({
    providedIn: 'root'
})
export class ComicBookService extends ApiBaseService {
    constructor(http: HttpClient) {
        super(http);
    }

    createComicBook(title: string, description: string): Observable<ComicBookCreateResponse> {
        return this.http.post<ComicBookCreateResponse>(
            `${this.baseUrl}/ComicBook/create`,
            { title, description }
        ).pipe(catchError(this.handleError));
    }

    getComicBook(id: string): Observable<ComicBookGetResponse> {
        return this.http.get<ComicBookGetResponse>(
            `${this.baseUrl}/ComicBook/${id}`
        ).pipe(catchError(this.handleError));
    }

    // Additional methods...
}
```

## Best Practices

### 1. State Management
For managing application state, consider using one of these approaches:

```typescript
// Option 1: Simple Service-based State
@Injectable({
    providedIn: 'root'
})
export class ComicBookStateService {
    private currentComicBook = new BehaviorSubject<ComicBook | null>(null);
    currentComicBook$ = this.currentComicBook.asObservable();

    setCurrentComicBook(comicBook: ComicBook) {
        this.currentComicBook.next(comicBook);
    }
}

// Option 2: NgRx (for larger applications)
// comic-book.actions.ts
export const loadComicBook = createAction(
    '[Comic Book] Load Comic Book',
    props<{ id: string }>()
);
```

### 2. Error Handling
Implement a global error handling service:

```typescript
// src/app/core/services/error-handler.service.ts
@Injectable({
    providedIn: 'root'
})
export class ErrorHandlerService {
    handleError(error: any) {
        if (error instanceof HttpErrorResponse) {
            // Handle HTTP errors
            switch (error.status) {
                case 400:
                    // Handle bad request
                    break;
                case 500:
                    // Handle server error
                    break;
                default:
                    // Handle other errors
            }
        }
    }
}
```

### 3. File Upload Handling
For handling image and audio uploads:

```typescript
// src/app/core/services/file-upload.service.ts
@Injectable({
    providedIn: 'root'
})
export class FileUploadService {
    validateFile(file: File, allowedTypes: string[]): boolean {
        return allowedTypes.includes(file.type);
    }

    createFormData(file: File, additionalData?: Record<string, any>): FormData {
        const formData = new FormData();
        formData.append('file', file);
        
        if (additionalData) {
            Object.entries(additionalData).forEach(([key, value]) => {
                formData.append(key, value);
            });
        }
        
        return formData;
    }
}
```

### 4. Streaming Response Handling
For handling story generation streams:

```typescript
// src/app/services/story-generator.service.ts
@Injectable({
    providedIn: 'root'
})
export class StoryGeneratorService extends ApiBaseService {
    generateStory(comicBookId: string, sceneId: string, description: string): Observable<string> {
        return this.http.post<GenerateStoryResponse>(
            `${this.baseUrl}/ComicBook/${comicBookId}/scene/${sceneId}/generate-story`,
            { sceneId, userDescription: description },
            { observe: 'events', reportProgress: true }
        ).pipe(
            filter(event => event.type === HttpEventType.Response),
            map(event => (event as HttpResponse<GenerateStoryResponse>).body?.storyTextChunk || ''),
            catchError(this.handleError)
        );
    }
}
```

## Testing Guidelines

### 1. Service Testing
Example of testing a service:

```typescript
describe('ComicBookService', () => {
    let service: ComicBookService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [ComicBookService]
        });

        service = TestBed.inject(ComicBookService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    it('should create a comic book', (done) => {
        const mockResponse: ComicBookCreateResponse = {
            comicBookId: '123',
            title: 'Test Comic'
        };

        service.createComicBook('Test Comic', 'Description').subscribe(response => {
            expect(response).toEqual(mockResponse);
            done();
        });

        const req = httpMock.expectOne(`${environment.apiBaseUrl}/ComicBook/create`);
        expect(req.request.method).toBe('POST');
        req.flush(mockResponse);
    });
});
```

### 2. Component Integration
Example of integrating services in a component:

```typescript
@Component({
    selector: 'app-comic-book-creator',
    template: `
        <form [formGroup]="comicBookForm" (ngSubmit)="onSubmit()">
            <input formControlName="title" placeholder="Title">
            <textarea formControlName="description" placeholder="Description"></textarea>
            <button type="submit">Create Comic Book</button>
        </form>
    `
})
export class ComicBookCreatorComponent implements OnInit {
    comicBookForm: FormGroup;

    constructor(
        private formBuilder: FormBuilder,
        private comicBookService: ComicBookService,
        private errorHandler: ErrorHandlerService
    ) {
        this.comicBookForm = this.formBuilder.group({
            title: ['', Validators.required],
            description: ['']
        });
    }

    onSubmit() {
        if (this.comicBookForm.valid) {
            const { title, description } = this.comicBookForm.value;
            this.comicBookService.createComicBook(title, description)
                .pipe(
                    catchError(error => {
                        this.errorHandler.handleError(error);
                        return EMPTY;
                    })
                )
                .subscribe(response => {
                    // Handle successful creation
                });
        }
    }
}
```

## Production Considerations

1. **Environment Configuration**
   - Use environment files for different deployment scenarios
   - Never commit sensitive information to source control
   - Use environment variables for API keys and endpoints

2. **Performance**
   - Implement proper unsubscribe patterns
   - Use OnPush change detection where appropriate
   - Implement lazy loading for feature modules

3. **Security**
   - Sanitize user inputs
   - Implement CORS properly
   - Use HTTPS for all API communications
   - Validate file uploads on both client and server

4. **Error Handling**
   - Implement proper error boundaries
   - Log errors appropriately
   - Provide user-friendly error messages
   - Handle offline scenarios

## Deployment Checklist

- [ ] Update environment.prod.ts with production API URL
- [ ] Enable production mode
- [ ] Implement proper error logging
- [ ] Set up proper CORS configuration
- [ ] Configure build optimization
- [ ] Test production build locally
- [ ] Verify all API endpoints are accessible
- [ ] Check file upload functionality
- [ ] Test voice recording features
- [ ] Verify story generation streaming