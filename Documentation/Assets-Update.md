Asset Management API Changes
New Enum Type: AssetType


enum AssetType {
  FULL_STORY = 'FULL_STORY',
  STYLED_IMAGE = 'STYLED_IMAGE',
  PDF = 'PDF',
  OTHER = 'OTHER'
}

Updated Asset Models
Asset Creation

interface AssetCreateRequest {
  comicBookId: string;
  assetType: AssetType;           // Changed from string to enum
  filePath: string;
  fullStoryText?: string;         // New field
  status: string;                 // New field, defaults to "IN_PROGRESS"
  pageNumber?: number;
}

Asset Update
interface AssetUpdateRequest {
  assetType?: AssetType;          // Changed from string to enum
  filePath?: string;
  fullStoryText?: string;         // New field
  status?: string;                // New field
  pageNumber?: number;
}

Asset Response
interface AssetResponse {
  assetId: string;
  comicBookId: string;
  assetType: AssetType;           // Changed from string to enum
  filePath: string;
  fullStoryText?: string;         // New field
  status: string;                 // New field, defaults to "IN_PROGRESS"
  pageNumber?: number;
  createdAt: Date;
}

Key Changes to Note:
AssetType is now an enum instead of a free-form string
New fullStoryText field for storing generated story content
New status field to track asset generation progress (defaults to "IN_PROGRESS")
All existing endpoints remain the same, but will now include these new fields in their responses
API Endpoints (Unchanged but with Updated Response Types)
POST /api/ComicBook/{comicBookId}/assets - Create asset
GET /api/ComicBook/assets/{assetId} - Get single asset
PUT /api/ComicBook/assets/{assetId} - Update asset
DELETE /api/ComicBook/assets/{assetId} - Delete asset
GET /api/ComicBook/{comicBookId}/assets - Get all assets for a comic book
The frontend team should update their type definitions and any UI components that display or edit asset data to accommodate these new fields.