
# Steps to Implement PostgreSQL Database for Backend (.NET 8 C#)

This document outlines the steps to implement a PostgreSQL database for the AI Comic Book Generator backend, based on the architecture described in the `ArchitecturePlan.md` and `BackendAPI.md` documents.

## 1. Prerequisites

Before you begin, ensure you have the following installed and configured:

*   **PostgreSQL Server:** Download and install PostgreSQL from the official website ([https://www.postgresql.org/](https://www.postgresql.org/)). Ensure you have the `pgAdmin` tool or another PostgreSQL administration tool installed as well for database management.
*   **.NET 8 SDK:**  Make sure you have the .NET 8 SDK installed on your development machine.
*   **Visual Studio 2022 (or your preferred .NET IDE):** You'll need an IDE to work with your .NET 8 C# backend project.
*   **Npgsql NuGet Package:** You will need to add the Npgsql NuGet package to your .NET project to enable PostgreSQL connectivity.

## 2. Steps

**Step 1: Install and Configure PostgreSQL Server**

1.  **Installation:** Run the PostgreSQL installer and follow the on-screen instructions. During installation, you will be prompted to set a password for the `postgres` user (the default PostgreSQL administrator user). Remember this password. Choose appropriate installation options for your local development environment.
2.  **Verify Installation:** After installation, verify that the PostgreSQL server is running. You can usually check this in your operating system's services management or by trying to connect using `pgAdmin` or `psql` command-line tool.
3.  **Database Administration Tool (pgAdmin):** Launch `pgAdmin` (or your preferred PostgreSQL administration tool). Connect to your local PostgreSQL server using the `postgres` user and the password you set during installation.

**Step 2: Create Databases**

1.  **Create `ComicBookData` Database:** In `pgAdmin`, right-click on "Databases" and select "Create" -> "Database...".
    *   Enter `ComicBookData` as the Database Name.
    *   Leave other options as default for now.
    *   Click "Save". This database will store comic book projects, scenes, images, and generated story text as outlined in your architecture plan.

2.  **Create `VoiceMimicData` Database:**  (If you are choosing to separate databases as initially depicted in the architecture diagram. If using a single database, you can skip this and use schemas instead - see Note below)
    *   In `pgAdmin`, right-click on "Databases" and select "Create" -> "Database...".
    *   Enter `VoiceMimicData` as the Database Name.
    *   Leave other options as default.
    *   Click "Save". This database will store user-recorded audio snippets and potentially trained voice models.

    **Note on Database Separation:** As discussed in the architecture review, consider if you *really* need two separate PostgreSQL *database instances*. For a local setup, using a single PostgreSQL instance with *two separate schemas* (e.g., `comic_book_schema` and `voice_mimic_schema`) within the *same* `ComicBookGeneratorDB` database might be simpler to manage.  Schemas provide logical separation within a database. Choose the approach that best suits your needs and perceived level of data isolation required (which is likely low for a local demo). If using schemas, you'd create one database, e.g., `ComicBookGeneratorDB`, and then create schemas within it instead of creating two separate databases. For simplicity in this guide, we'll assume separate databases initially as per your diagram, but schema-based approach is often preferred for logical separation within a single application.

PostgreSQL port: 5432
Installation Directory: C:\Program Files\PostgreSQL\17
Server Installation Directory: C:\Program Files\PostgreSQL\17
Data Directory: C:\Program Files\PostgreSQL\17\data
Database Port: 5432
Database Superuser: postgres
Operating System Account: NT AUTHORITY\NetworkService
Database Service: postgresql-x64-17
Command Line Tools Installation Directory: C:\Program Files\PostgreSQL\17
pgAdmin4 Installation Directory: C:\Program Files\PostgreSQL\17\pgAdmin 4
Stack Builder Installation Directory: C:\Program Files\PostgreSQL\17
Installation Log: C:\Users\rroet\AppData\Local\Temp\install-postgresql.log
**Step 3: Define Database Schemas/Tables**

1.  **Design Tables based on DTOs:** Based on your DTO definitions in `BackendAPI.md` (especially the `ComicBookGetResponse`, `SceneGetResponse`, and Voice Mimicking DTOs that imply data entities), design your database tables. Consider the following initial tables and columns within the `ComicBookData` database (or `comic_book_schema` if using schemas):

    *   **`ComicBooks` Table:**
        *   `ComicBookId` (UUID or SERIAL/Identity - Primary Key, unique identifier)
        *   `Title` (VARCHAR - String for comic book title)
        *   `Description` (TEXT - String for comic book description)
        *   `CreatedAt` (TIMESTAMP WITH TIME ZONE - Automatically record creation time)
        *   `UpdatedAt` (TIMESTAMP WITH TIME ZONE - Automatically record update time)

    *   **`Scenes` Table:**
        *   `SceneId` (UUID or SERIAL/Identity - Primary Key, unique identifier)
        *   `ComicBookId` (UUID or Foreign Key referencing `ComicBooks.ComicBookId`)
        *   `SceneOrder` (INTEGER - Order of the scene within the comic book)
        *   `ImagePath` (VARCHAR - String for path to stored image - nullable)
        *   `UserDescription` (TEXT - String for user-provided scene description - nullable)
        *   `AiGeneratedStory` (TEXT - String for AI-generated story - nullable)
        *   `CreatedAt` (TIMESTAMP WITH TIME ZONE)
        *   `UpdatedAt` (TIMESTAMP WITH TIME ZONE)

    Within the `VoiceMimicData` database (or `voice_mimic_schema` if using schemas):

    *   **`AudioSnippets` Table:**
        *   `AudioSnippetId` (UUID or SERIAL/Identity - Primary Key, unique identifier)
        *   `AudioFilePath` (VARCHAR - Path to stored audio file)
        *   `CreatedAt` (TIMESTAMP WITH TIME ZONE)
        *   `UpdatedAt` (TIMESTAMP WITH TIME ZONE)
        *   **(Potentially other columns):**  Consider if you need to store user identifiers, recording session identifiers, or other metadata related to audio snippets.

    *   **`VoiceModels` Table:** (If you plan to store trained voice models metadata)
        *   `VoiceModelId` (UUID or SERIAL/Identity - Primary Key)
        *   `VoiceModelName` (VARCHAR)
        *   `TrainingDate` (TIMESTAMP WITH TIME ZONE)
        *   **(Potentially columns to store model path/identifier, associated user, etc.)**

2.  **Create Tables using `pgAdmin` or SQL Scripts:**  You can use `pgAdmin`'s interface to create these tables interactively, or write SQL `CREATE TABLE` scripts and execute them in `pgAdmin`'s query tool.

    **Example SQL Script (for `ComicBookData` database - you'd adapt for `VoiceMimicData`):**

    ```sql
    CREATE TABLE ComicBooks (
        ComicBookId UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- or use SERIAL for auto-incrementing integer ID
        Title VARCHAR(255) NOT NULL,
        Description TEXT,
        CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
        UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
    );

    CREATE TABLE Scenes (
        SceneId UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- or use SERIAL
        ComicBookId UUID NOT NULL REFERENCES ComicBooks(ComicBookId) ON DELETE CASCADE, -- Foreign Key
        SceneOrder INTEGER NOT NULL,
        ImagePath VARCHAR(255),
        UserDescription TEXT,
        AiGeneratedStory TEXT,
        CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
        UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
    );

    -- Add indexes for performance (optional but recommended for larger datasets)
    CREATE INDEX IX_Scenes_ComicBookId ON Scenes (ComicBookId);
    ```

    **Important Notes on IDs:**
    *   **UUID vs SERIAL/Identity:** UUIDs (Universally Unique Identifiers) are generally preferred for distributed systems and when you need to generate IDs client-side or avoid sequential ID guessing. `SERIAL` or Identity columns are auto-incrementing integers, simpler for local setup and often efficient. Choose based on your project's long-term needs. The example uses UUIDs.
    *   **Data Types:** Adjust data types (e.g., VARCHAR lengths) as needed.
    *   **Relationships:** Pay attention to foreign key relationships (e.g., `Scenes` table referencing `ComicBooks`). `ON DELETE CASCADE` in the foreign key relationship for `Scenes.ComicBookId` means if a `ComicBook` is deleted, all associated `Scenes` will also be deleted, which is often desired.
    *   **Indexes:**  Consider adding indexes to frequently queried columns (like `ComicBookId` in the `Scenes` table for efficient retrieval of scenes for a comic book).

    -- SQL scripts to create tables for Comic Book Generator Database with Schemas

--------------------------------------------------------------------------------
-- Schema: comic_book_schema
--------------------------------------------------------------------------------

-- Create schema if it doesn't exist (optional, but good practice for script idempotency)
CREATE SCHEMA IF NOT EXISTS comic_book_schema;

-- Set the search path to include comic_book_schema so tables are created in it by default
SET search_path TO comic_book_schema, public;

-- Table: ComicBooks
CREATE TABLE comic_book_schema."ComicBooks" (
    "ComicBookId" UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- UUID as Primary Key, auto-generated
    "Title" VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP, -- Automatically set on creation
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP  -- Automatically updated on modification (you'll likely need to handle this in your backend logic)
);

-- Table: Scenes
CREATE TABLE comic_book_schema."Scenes" (
    "SceneId" UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- UUID as Primary Key, auto-generated
    "ComicBookId" UUID NOT NULL, -- Foreign Key referencing ComicBooks
    "SceneOrder" INTEGER NOT NULL, -- Order of the scene within the comic book
    "ImagePath" VARCHAR(255),      -- Path to stored image (nullable)
    "UserDescription" TEXT,       -- User-provided scene description (nullable)
    "AiGeneratedStory" TEXT,      -- AI-generated story text (nullable)
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP, -- Automatically set on creation
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,  -- Automatically updated on modification

    -- Foreign Key Constraint
    CONSTRAINT "FK_Scenes_ComicBooks" FOREIGN KEY ("ComicBookId")
        REFERENCES comic_book_schema."ComicBooks" ("ComicBookId") ON DELETE CASCADE
        -- ON DELETE CASCADE: When a ComicBook is deleted, all associated Scenes are also deleted.
);

-- Index on ComicBookId in Scenes table for efficient querying of scenes by ComicBook
CREATE INDEX "IX_Scenes_ComicBookId" ON comic_book_schema."Scenes" ("ComicBookId");


--------------------------------------------------------------------------------
-- Schema: voice_mimic_schema
--------------------------------------------------------------------------------

-- Create schema if it doesn't exist (optional, but good practice for script idempotency)
CREATE SCHEMA IF NOT EXISTS voice_mimic_schema;

-- Set the search path to include voice_mimic_schema so tables are created in it by default
SET search_path TO voice_mimic_schema, public;

-- Table: AudioSnippets
CREATE TABLE voice_mimic_schema."AudioSnippets" (
    "AudioSnippetId" UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- UUID as Primary Key, auto-generated
    "AudioFilePath" VARCHAR(255) NOT NULL, -- Path to the stored audio file
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP, -- Automatically set on creation
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP  -- Automatically updated on modification

    -- Add other columns here if needed in the future:
    -- Example: "UserId" UUID,  -- If you want to associate snippets with users
    --          "RecordingSessionId" UUID, -- If you implement recording sessions
    --          "SnippetMetadata" JSONB, -- For flexible metadata storage
);

-- Table: VoiceModels
CREATE TABLE voice_mimic_schema."VoiceModels" (
    "VoiceModelId" UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- UUID as Primary Key, auto-generated
    "VoiceModelName" VARCHAR(255),       -- Name of the voice model
    "TrainingDate" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP -- Date when the model was trained

    -- Consider adding more columns as needed:
    -- Example: "ModelFilePath" VARCHAR(255), -- Path to the trained model file (if stored locally)
    --          "ModelIdentifier" VARCHAR(255), -- Identifier if model is hosted on a platform
    --          "UserId" UUID, -- If voice models are user-specific
    --          "ModelMetadata" JSONB, -- For storing training parameters, etc.
);


-- Reset search path to default (optional, but good practice)
SET search_path TO public;

**Step 4: Configure .NET 8 Project for PostgreSQL Connection**

1.  **Install Npgsql Entity Framework Core NuGet Package:** In your .NET 8 backend project, use the NuGet Package Manager (in Visual Studio or using the .NET CLI) to install `Npgsql.EntityFrameworkCore.PostgreSQL`.  (If you choose to use a different data access method like Dapper, install the appropriate Npgsql package for raw ADO.NET access, like `Npgsql`).  For this guide, we will assume EF Core.

2.  **Connection String:** In your `appsettings.json` file (or `appsettings.Development.json` for development), add a connection string for PostgreSQL.  If you created two databases, you'll need two connection strings or you'll need to configure your DbContext(s) to point to schemas instead if you opted for the single database with schemas approach.

    **Example `appsettings.json` (for single database `ComicBookGeneratorDB` with schemas approach - adjust if using separate databases):**

    ```json
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "AllowedHosts": "*",
      "ConnectionStrings": {
        "ComicBookGeneratorDbConnection": "Host=localhost;Port=5432;Database=ComicBookGeneratorDB;Username=postgres;Password=your_postgres_password;"  // Replace with your password
      }
    }
    ```

    **If using two separate databases (adjust connection strings accordingly):**

    ```json
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "AllowedHosts": "*",
      "ConnectionStrings": {
        "ComicBookDataConnection": "Host=localhost;Port=5432;Database=ComicBookData;Username=postgres;Password=your_postgres_password;", // Replace password
        "VoiceMimicDataConnection": "Host=localhost;Port=5432;Database=VoiceMimicData;Username=postgres;Password=your_postgres_password;"  // Replace password
      }
    }
    ```

    **Important:** Replace `your_postgres_password` with the actual password you set for the `postgres` user during PostgreSQL installation. For production, never hardcode passwords in `appsettings.json`. Use environment variables or secrets management.

**Step 5: Implement Data Access Layer (using Entity Framework Core)**

1.  **Create DbContext Classes:** Create C# classes that inherit from `DbContext` for each database (or schema set if using schemas). For example:

    ```csharp
    // ComicBookDataContext.cs (If using separate ComicBookData database or comic_book_schema)
    using Microsoft.EntityFrameworkCore;
    using YourProjectNamespace.Models; // Assuming your model classes are in a 'Models' namespace

    namespace YourProjectNamespace.Data
    {
        public class ComicBookDataContext : DbContext
        {
            public ComicBookDataContext(DbContextOptions<ComicBookDataContext> options) : base(options)
            {
            }

            public DbSet<ComicBook> ComicBooks { get; set; } // DbSet for ComicBook entity
            public DbSet<Scene> Scenes { get; set; }       // DbSet for Scene entity

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                // Configure relationships and any specific model configurations here, if needed.
                // Example: modelBuilder.Entity<Scene>().HasOne(s => s.ComicBook).WithMany(cb => cb.Scenes).HasForeignKey(s => s.ComicBookId);
            }
        }
    }


    // VoiceMimicDataContext.cs (If using separate VoiceMimicData database or voice_mimic_schema)
    using Microsoft.EntityFrameworkCore;
    using YourProjectNamespace.Models; // Assuming your voice mimic models are also in 'Models'

    namespace YourProjectNamespace.Data
    {
        public class VoiceMimicDataContext : DbContext
        {
            public VoiceMimicDataContext(DbContextOptions<VoiceMimicDataContext> options) : base(options)
            {
            }

            public DbSet<AudioSnippet> AudioSnippets { get; set; } // DbSet for AudioSnippet
            public DbSet<VoiceModel> VoiceModels { get; set; }     // DbSet for VoiceModel

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                // Configure voice mimic specific model configurations if needed
            }
        }
    }
    ```

2.  **Define Entity Models:** Create C# classes in a `Models` folder (or similar) that map to your database tables. These classes will be used by Entity Framework Core.

    **Example Models (in `Models` folder):**

    ```csharp
    // ComicBook.cs
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace YourProjectNamespace.Models
    {
        public class ComicBook
        {
            [Key]
            public Guid ComicBookId { get; set; } // Assuming UUIDs, if using SERIAL, use int and [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            [Required]
            [MaxLength(255)]
            public string Title { get; set; }
            public string? Description { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }

            public ICollection<Scene> Scenes { get; set; } = new List<Scene>(); // Navigation property for Scenes
        }
    }


    // Scene.cs
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace YourProjectNamespace.Models
    {
        public class Scene
        {
            [Key]
            public Guid SceneId { get; set; } // UUID or int
            [Required]
            public Guid ComicBookId { get; set; } // Foreign Key to ComicBook
            [Required]
            public int SceneOrder { get; set; }
            [MaxLength(255)]
            public string? ImagePath { get; set; }
            public string? UserDescription { get; set; }
            public string? AiGeneratedStory { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }

            [ForeignKey("ComicBookId")] // Explicitly define foreign key relationship
            public ComicBook ComicBook { get; set; } // Navigation property to ComicBook
        }
    }

    // AudioSnippet.cs (VoiceMimicData models - example)
    using System;
    using System.ComponentModel.DataAnnotations;

    namespace YourProjectNamespace.Models
    {
        public class AudioSnippet
        {
            [Key]
            public Guid AudioSnippetId { get; set; } // UUID or int
            [Required]
            [MaxLength(255)]
            public string AudioFilePath { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            // Add other properties as needed
        }
    }

    // VoiceModel.cs (VoiceMimicData models - example)
    using System;
    using System.ComponentModel.DataAnnotations;

    namespace YourProjectNamespace.Models
    {
        public class VoiceModel
        {
            [Key]
            public Guid VoiceModelId { get; set; } // UUID or int
            [MaxLength(255)]
            public string? VoiceModelName { get; set; }
            public DateTime TrainingDate { get; set; }
            // Add other properties as needed
        }
    }
    ```

3.  **Configure DbContext in `Program.cs` (or `Startup.cs` in older .NET versions):**  Register your DbContext(s) with the dependency injection system and configure them to use PostgreSQL and your connection string.

    ```csharp
    // Program.cs (Example for a single DbContext - adjust if using two or schema approach)
    using YourProjectNamespace.Data; // Namespace where your DbContext is
    using Microsoft.EntityFrameworkCore;

    var builder = WebApplication.CreateBuilder(args);

    // ... other services configuration ...

    builder.Services.AddDbContext<ComicBookDataContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("ComicBookGeneratorDbConnection"))); // Use your connection string name

    // If using separate DbContexts, register the second one as well, e.g.,
    // builder.Services.AddDbContext<VoiceMimicDataContext>(options =>
    //     options.UseNpgsql(builder.Configuration.GetConnectionString("VoiceMimicDataConnection")));


    // ... rest of Program.cs ...
    ```

**Step 6: Integrate Data Access Layer in Services**

1.  **Inject DbContext into Services:** Inject your `ComicBookDataContext` and (if applicable) `VoiceMimicDataContext` into your `ComicBookService` and `VoiceMimickingService` classes, respectively, via constructor injection.

    ```csharp
    // Example in ComicBookService.cs
    using YourProjectNamespace.Data;
    using Microsoft.EntityFrameworkCore; // For async operations
    using System.Threading.Tasks;

    namespace YourProjectNamespace.Services
    {
        public class ComicBookService : IComicBookService // Assuming you have an interface
        {
            private readonly ComicBookDataContext _context; // Inject DbContext

            public ComicBookService(ComicBookDataContext context)
            {
                _context = context;
            }

            public async Task<ComicBookCreateResponse> CreateComicBookAsync(ComicBookCreateRequest request)
            {
                var comicBook = new ComicBook
                {
                    Title = request.Title,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ComicBooks.Add(comicBook);
                await _context.SaveChangesAsync(); // Save changes to the database

                return new ComicBookCreateResponse { ComicBookId = comicBook.ComicBookId.ToString(), Title = comicBook.Title };
            }

            // ... Implement other service methods, using _context to interact with the database ...
        }
    }
    ```

2.  **Implement Service Methods:** Implement the methods in your `ComicBookService` and `VoiceMimickingService` to perform database operations using the injected `DbContext`. Use Entity Framework Core methods (e.g., `_context.ComicBooks.Add()`, `_context.ComicBooks.FindAsync()`, `_context.ComicBooks.Update()`, `_context.ComicBooks.Remove()`, `_context.SaveChangesAsync()`, LINQ queries) to interact with your PostgreSQL database.

**Step 7: Basic Validation and Testing**

1.  **Run Migrations (if using EF Core Migrations):** If you want to use EF Core Migrations to create the database schema from your code (instead of creating tables manually in `pgAdmin`), you can create and apply migrations.
    *   Add a migration: `Add-Migration InitialCreate -Context ComicBookDataContext` (and similar for `VoiceMimicDataContext` if you have it) in the Package Manager Console in Visual Studio (or using `.NET CLI` commands).
    *   Apply migrations to the database: `Update-Database -Context ComicBookDataContext` (and similarly for VoiceMimicDataContext). This will create the tables in your PostgreSQL database based on your entity models and DbContext configurations.

2.  **Test API Endpoints:**  Use tools like Postman, Insomnia, or Swagger UI to test your API endpoints (`/api/comicbook/create`, `/api/comicbook/{comicBookId}`, etc.).  Verify that data is correctly persisted to the PostgreSQL database when you create comic books and scenes, and retrieved correctly when you fetch them.  Test update and delete operations as well.

## 3. Conclusion

You have now implemented the PostgreSQL database for your backend. You have:

*   Installed and configured PostgreSQL.
*   Created databases (or considered schemas).
*   Defined database schemas and tables.
*   Configured your .NET 8 project to connect to PostgreSQL using Entity Framework Core.
*   Implemented a basic data access layer and started integrating it into your service layer.
*   Performed basic validation and testing steps.

**Next Steps:**

*   Continue implementing the remaining service layer methods to fully integrate database operations for all API endpoints.
*   Implement input validation and error handling in your API controllers and service layer.
*   Develop unit tests and integration tests for your services and data access layer.
*   Start building the Angular frontend and connect it to your backend API to create a complete end-to-end flow.
