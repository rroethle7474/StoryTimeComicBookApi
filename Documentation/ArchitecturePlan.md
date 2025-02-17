# Architectural Plan: AI Comic Book Generator - Summary (Up to Backend API Design)

This document summarizes the architectural plan for an AI-powered comic book generator application, as discussed so far.

## 1. System Goals

*   **Comic Book Generation:**  Generate a visually appealing, short comic book (max 6 scenes) by combining user-provided hand-drawn images and descriptions with AI-driven story generation.
*   **Voice Mimicking:** Enable users to record audio snippets to train a text-to-speech model that mimics their voice.
*   **User-Friendly Interface:** Develop a modern and intuitive user interface using Angular.
*   **Scalable and Maintainable Backend:** Build a robust and scalable backend API that can integrate with various AI services and manage data efficiently (initially focusing on local setup).
*   **Local Setup:** Application will be designed for local execution on a Windows 11 machine for demo purposes, initially without cloud deployment or user authentication.

## 2. System Architecture Diagram (Local Setup)

```mermaid
graph LR
    subgraph Frontend (Angular - Local)
        A[Comic Book Creator UI] --> B{Backend API Gateway (Local)}
        C[Voice Recording UI] --> B
    end
    subgraph Backend API (Local)
        B --> D[API Gateway]
        D --> E[Comic Book Service]
        D --> F[Voice Mimicking Service]
        E --> G[AI Story Generation (Local or API)]
        F --> H[Text-to-Speech Model Training (Local or API)]
        E --> I[Database (PostgreSQL - Local)]
        F --> J[Database (PostgreSQL - Local)]
    end
3. Component Breakdown
3.1. Frontend (Angular Application)
Technology: Angular (Standalone Components), HTML, CSS, Tailwind CSS
Functionality:
Comic Book Creator UI (A): Image upload, description input, comic book preview, scene management (max 6 scenes), styling with Tailwind CSS.
Voice Recording UI (C): Audio recording, recording management, user feedback, integration with backend for audio upload.
3.2. Backend API (C# .NET)
Technology: C# (.NET), ASP.NET Core for API
API Gateway (D): Entry point, routing, potential authentication (future consideration).
Comic Book Service (E): Comic book and scene management, orchestrates AI story generation, interacts with database for comic book data.
Voice Mimicking Service (F): Voice recording management, text-to-speech model training orchestration, speech synthesis, interacts with database for audio data.
AI Story Generation (G): Integration with AI APIs (initially Gemini, with flexibility for OpenAI, Anthropic, Deepseek). Focus on API-based solutions for local setup.
Text-to-Speech Model Training (H): Integration with TTS platforms (Replicate, Hugging Face, etc.). Focus on ease of use and training.
Data Storage (I, J): PostgreSQL database for storing images, text, audio, and project metadata.
3.3. Database
Technology: PostgreSQL (Local Setup Recommended)
Functionality:
Image/Text Storage (I): Store image paths, generated story text, comic book project data.
Audio Storage (J): Store user-recorded audio snippets, potentially trained voice models.
3.4. AI Model Platforms
AI Story Generation (G): Google Gemini (initially), OpenAI, Anthropic, Deepseek APIs.
Text-to-Speech Model Training (H): Replicate, Hugging Face (open to other platforms prioritizing ease of use).
4. Technology Stack Summary
Frontend: Angular, HTML, CSS, Tailwind CSS
Backend: C# (.NET), ASP.NET Core for API
Database: PostgreSQL
AI Story Generation: Google Gemini API (initially)
Text-to-Speech Model Training: Replicate/Hugging Face (to be further explored)
IDE: Visual Studio 2022 (Backend), Cursor AI (Frontend - VS Code based)
OS: Windows 11 Pro (Development Machine)
5. Development Workflow (Iterative Approach)
Backend API First: Implement and validate core backend API endpoints and services (including AI story generation integration).
Database Integration: Integrate PostgreSQL database and validate data persistence.
Frontend Skeleton: Build a basic Angular frontend with core components and integrate with the backend API for end-to-end flow validation.
Iterative Frontend Development: Gradually enhance the frontend UI, features, and user experience.
Voice Mimicking Feature Implementation: Implement voice recording, TTS model training, and speech synthesis features in backend and frontend.
Refinement and Testing: Testing, bug fixing, and iterative refinement of all components.
6. Detailed Backend API Design (C# .NET)
(Detailed API endpoints, DTOs, Service Layer Design, and AI API Integration Strategy as outlined in the BackendApiDesign.md document)