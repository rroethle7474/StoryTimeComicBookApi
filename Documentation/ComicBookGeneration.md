Epic: AI-Driven Comic Book Generation
Goal / Description
Enable users to create custom comic books by:

Providing high-level story details (title, description, additional context).
Uploading up to 4 scene images with descriptions.
“Comic-ifying” the scene images via a Stable Diffusion image-to-image pipeline on Replicate.
Generating a cohesive story using an LLM (e.g., GPT).
Displaying the final text and stylized images as one consolidated comic book asset.
Scope & Assumptions
Users input text + upload images via an Angular UI.
The system calls a .NET 8 REST API to handle:
Database operations (PostgreSQL).
Calls to Replicate for image stylization.
Calls to LLM for story text generation.
Final output stored in the database under ComicBookAssets.
Basic progress monitoring UI to show generation status.
High-Level Workflow
User enters comic details (Title, Description, Additional Details).
User uploads scene images and provides short text descriptions.
User clicks "Generate" -> triggers creation of a new ComicBookAssets record with status “IN_PROGRESS.”
System stylizes each scene image via Stable Diffusion image-to-image on Replicate.
System calls the LLM with final details (title, description, scene data, references to stylized images).
System compiles the final text (with references to the stylized images) into a single “comic story” stored in ComicBookAssets.
ComicBookAssets status is set to “COMPLETED,” and the user can view/download the result.
Task Breakdown
1. Frontend (Angular)
Comic Creation UI

Task: Implement a form to capture the comic title, description, additional details.
Task: Create a scene-list section allowing up to 4 uploads (image + short text).
Task: Validate user inputs, enforce max scene limit.
“Generate” Button & Progress Page

Task: On click, send a request to the API to initiate generation, receive a “ComicBookAssetId.”
Task: Redirect user to a progress/monitor page that polls the status endpoint.
Task: Display steps or a progress bar (e.g., “Styling Scenes,” “Generating Story,” etc.).
Final Display Page

Task: Implement a page to show the final story (text) and stylized images.
Task: Optionally allow PDF export or “Print” functionality.
Task: Ensure all images are loaded from their stored file paths (e.g., from StyledImagePath).
2. Backend (ASP.NET Core / .NET 8)
Data Model & Migration

Task: Ensure ComicBooks, Scenes, and ComicBookAssets tables have all needed columns (e.g., StyledImagePath, FullStoryText, Status).
Task: Run database migrations to apply any structure changes.
Generation Controller / Endpoints

Task: Start Generation endpoint
Creates a ComicBookAssets record with Status = IN_PROGRESS.
Returns an identifier for further status checks.
Task: Check Status endpoint
Returns current status of the asset (e.g., “STYLIZING”, “GENERATING_STORY”, “COMPLETED”) plus any partial results.
Task: Complete Generation
Sets the final story text in ComicBookAssets.FullStoryText.
Updates Status to “COMPLETED.”
Integration with Replicate (Stable Diffusion)

Task: Implement a service class to handle the Replicate API calls.
Task: For each uploaded scene image, call the image-to-image endpoint with the correct prompt and style parameters.
Task: Store the resulting stylized image file in wwwroot and set Scenes.StyledImagePath.
Integration with LLM

Task: Implement a service class/method to call the LLM (e.g., GPT).
Task: Construct the prompt including Title, Description, Additional Details, and each scene’s user description & styled image reference.
Task: Receive final text from the LLM and store it in ComicBookAssets.FullStoryText.
Orchestration / Business Logic

Task: Write a workflow or queue-based process to sequentially:
Grab all Scenes for the comic.
For each, stylize the image via Replicate.
Once images are ready, call the LLM to generate the story.
Update ComicBookAssets.
Error Handling & Logging

Task: Ensure failures in stylizing or LLM calls are caught, with partial progress saved.
Task: Return error statuses to the frontend if generation fails.
3. Database
Tables & Columns

Task: Confirm final columns in Scenes:
SceneId, ComicBookId, UserDescription, StyledImagePath, etc.
Task: Confirm final columns in ComicBookAssets:
AssetId, ComicBookId, FullStoryText, FilePath (if PDF is generated), Status, CreatedAt.
Task: Ensure indexing or performance considerations are adequate.
Relationships

Task: Confirm the foreign key from Scenes to ComicBooks.
Task: Confirm the foreign key from ComicBookAssets to ComicBooks.
Final Asset Storage

Task: Decide if you need a text column (FullStoryText) or a JSON structure (if you want references to images).
Task: Implement any needed queries to retrieve final text + scene paths for display.
4. Additional / Optional
PDF Generation

Task: If needed, integrate a service (like wkhtmltopdf or Puppeteer) to convert the final story + images into a PDF.
Task: Store the PDF path in ComicBookAssets.FilePath (or similar).
Re-generation / Versioning

Task: Decide whether to allow multiple runs. If so, add a version column or store multiple ComicBookAssets rows per comic.
Security / Access Control

Task: Ensure only the user who created the comic can trigger generation or view the final asset.
Task: Protect image upload endpoints and final stylized images in wwwroot.
Styling & Theming

Task: In Angular, add a nice layout or “comic-themed” styling for the final display (Tailwind UI components, etc.).
Milestones
Milestone 1: Basic CRUD for Comics and Scenes (Frontend + Backend + DB).
Milestone 2: “Generate” button triggers the asset creation, storing a record with “IN_PROGRESS.”
Milestone 3: Image stylization pipeline integrated with Replicate (store results in Scenes.StyledImagePath).
Milestone 4: LLM integration for final text generation (store in ComicBookAssets.FullStoryText).
Milestone 5: UI display of final story + images, plus status polling.
Milestone 6: Error handling, final polish, optional PDF export.
Summary
This Epic plan provides a high-level roadmap: you’ll develop UI forms for user input, wire them to a backend that orchestrates image transformation (via Replicate) and story generation (via LLM), and store the final result in ComicBookAssets. With each task completed, you’ll have a working end-to-end pipeline that allows users to create, track, and view their AI-generated comic books.