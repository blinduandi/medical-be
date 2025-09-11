# File Management System Documentation

## Overview

The Medical API now includes a comprehensive file management system for handling patient-related files such as:

- 🖼️ **Profile Photos** - Patient and staff profile images
- 🩻 **Medical Scans** - X-rays, MRIs, CT scans, Ultrasounds
- 📄 **Lab Results** - Laboratory test reports and results
- 💊 **Prescriptions** - Medical prescriptions and medication documents
- 📋 **Medical Documents** - General medical documentation

## Features

### ✅ **Core Functionality**
- ✅ Secure file upload with validation
- ✅ Multiple file type support (images, PDFs, DICOM)
- ✅ File size limits per type
- ✅ Image processing (thumbnails, blur hash)
- ✅ Soft delete with audit trail
- ✅ Polymorphic file associations
- ✅ RESTful API endpoints

### 🔐 **Security Features**
- ✅ Role-based access control
- ✅ File type validation
- ✅ Size limits enforcement
- ✅ Audit logging for all operations
- ✅ Optional password protection

### 📊 **Management Features**
- ✅ File type configuration
- ✅ Bulk operations
- ✅ Search and filtering
- ✅ Temporary file cleanup
- ✅ Pagination support

## Database Schema

### FileTypes Table
```sql
- Id (int, PK)
- Name (nvarchar(100))
- Description (nvarchar(500))
- Category (nvarchar(100))
- AllowedExtensions (nvarchar(max)) -- JSON array
- MaxSizeBytes (bigint)
- IsActive (bit)
- CreatedAt (datetime2)
- UpdatedAt (datetime2)
```

### MedicalFiles Table
```sql
- Id (uniqueidentifier, PK)
- TypeId (int, FK)
- Name (nvarchar(255))
- Path (nvarchar(500))
- Size (bigint)
- Extension (nvarchar(10))
- MimeType (nvarchar(100))
- ModelType (nvarchar(100)) -- Entity type (User, MedicalRecord, etc.)
- ModelId (nvarchar(450)) -- Entity ID
- CreatedById (nvarchar(450), FK)
- UpdatedById (nvarchar(450), FK)
- DeletedById (nvarchar(450), FK)
- Password (nvarchar(500)) -- Optional encryption
- Label (nvarchar(200)) -- Custom file label
- IsTemporary (bit)
- BlurHash (nvarchar(100)) -- Image preview
- Width (int) -- Image width
- Height (int) -- Image height
- Metadata (nvarchar(1000)) -- JSON metadata
- DeletedAt (datetime2) -- Soft delete
- CreatedAt (datetime2)
- UpdatedAt (datetime2)
```

## API Endpoints

### 📤 **File Upload**
```http
POST /api/files/upload
Content-Type: multipart/form-data

Form Data:
- File: [file] (required)
- TypeId: [int] (required) - File type ID
- Label: [string] (optional) - Custom label
- ModelType: [string] (optional) - Entity type
- ModelId: [string] (optional) - Entity ID
- Password: [string] (optional) - File encryption
- IsTemporary: [bool] (optional) - Temporary file flag
```

**Response:**
```json
{
  "success": true,
  "message": "File uploaded successfully",
  "data": {
    "id": "uuid",
    "name": "original-filename.jpg",
    "displayName": "Custom Label or original name",
    "size": 1048576,
    "sizeFormatted": "1.0 MB",
    "extension": ".jpg",
    "mimeType": "image/jpeg",
    "isImage": true,
    "width": 1920,
    "height": 1080,
    "blurHash": "LEHV6nWB2yk8pyo0adR*.7kCMdnj",
    "downloadUrl": "/api/files/{id}/download",
    "thumbnailUrl": "/api/files/{id}/thumbnail",
    "createdAt": "2025-09-11T06:00:00Z",
    "type": {
      "id": 1,
      "name": "Profile Photo",
      "category": "ProfilePhoto"
    }
  }
}
```

### 📥 **File Download**
```http
GET /api/files/{id}/download
Authorization: Bearer {token}
```

### 🖼️ **Image Thumbnail**
```http
GET /api/files/{id}/thumbnail
Authorization: Bearer {token}
```

### 🔍 **Search Files**
```http
GET /api/files?page=1&pageSize=20&modelType=User&modelId=123&typeId=1
Authorization: Bearer {token}
```

### 📁 **Get Entity Files**
```http
GET /api/files/entity/{modelType}/{modelId}
Authorization: Bearer {token}
```

### ✏️ **Update File**
```http
PUT /api/files/{id}
Content-Type: application/json
Authorization: Bearer {token}

{
  "label": "Updated label",
  "isTemporary": false
}
```

### 🗑️ **Delete File**
```http
DELETE /api/files/{id}
Authorization: Bearer {token}
```

### 🗑️ **Bulk Delete**
```http
POST /api/files/bulk-delete
Content-Type: application/json
Authorization: Bearer {token}

{
  "fileIds": ["uuid1", "uuid2", "uuid3"]
}
```

### ⚙️ **File Types Management (Admin Only)**

#### Get File Types
```http
GET /api/files/types
Authorization: Bearer {token}
```

#### Create File Type
```http
POST /api/files/types
Content-Type: application/json
Authorization: Bearer {token} (Admin role required)

{
  "name": "Blood Test Results",
  "description": "Laboratory blood test reports",
  "category": "LabResult",
  "allowedExtensions": ["pdf", "jpg", "png"],
  "maxSizeBytes": 10485760
}
```

## Pre-configured File Types

1. **Profile Photo** - User profile images (5MB max)
2. **X-Ray** - X-ray medical images (50MB max)
3. **MRI Scan** - MRI medical images (100MB max)
4. **CT Scan** - CT scan medical images (100MB max)
5. **Ultrasound** - Ultrasound medical images (20MB max)
6. **Lab Result** - Laboratory test results (10MB max)
7. **Prescription** - Medical prescriptions (10MB max)
8. **Medical Document** - General medical documents (25MB max)
9. **Medical Scan** - General medical scans (50MB max)

## Configuration

### appsettings.json
```json
{
  "FileStorage": {
    "UploadPath": "uploads",
    "ThumbnailPath": "uploads/thumbnails",
    "MaxFileSize": 104857600,
    "AllowedExtensions": ["jpg", "jpeg", "png", "pdf", "doc", "docx", "dcm"]
  }
}
```

## Usage Examples

### 👤 **Upload Patient Profile Photo**
```http
POST /api/files/upload
Content-Type: multipart/form-data

File: patient-photo.jpg
TypeId: 1 (Profile Photo)
ModelType: User
ModelId: patient-user-id
Label: "Patient Profile Photo"
```

### 🩻 **Upload X-Ray for Medical Record**
```http
POST /api/files/upload
Content-Type: multipart/form-data

File: chest-xray.jpg
TypeId: 2 (X-Ray)
ModelType: MedicalRecord
ModelId: medical-record-id
Label: "Chest X-Ray - Follow-up"
```

### 📄 **Upload Lab Results**
```http
POST /api/files/upload
Content-Type: multipart/form-data

File: blood-test-results.pdf
TypeId: 6 (Lab Result)
ModelType: VisitRecord
ModelId: visit-record-id
Label: "Blood Test Results - Annual Checkup"
```

### 🔍 **Get All Files for a Patient**
```http
GET /api/files/entity/User/patient-user-id
Authorization: Bearer {token}
```

### 🔍 **Search Medical Scans**
```http
GET /api/files?category=XRay&page=1&pageSize=10
Authorization: Bearer {token}
```

## Security Considerations

### 🔐 **Access Control**
- ✅ Users can only access their own files
- ✅ Doctors can access patient files they're treating
- ✅ Admins have full access
- ✅ File uploads are authenticated only

### 🛡️ **File Validation**
- ✅ File type restrictions by category
- ✅ File size limits enforced
- ✅ MIME type validation
- ✅ Extension validation

### 📋 **Audit Trail**
- ✅ All file operations logged
- ✅ User tracking for create/update/delete
- ✅ Soft delete with restoration capability

## Error Handling

### Common Error Responses

**File Too Large:**
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": "File size exceeds maximum allowed size for this file type"
}
```

**Invalid File Type:**
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": "File extension not allowed for this file type"
}
```

**File Not Found:**
```json
{
  "success": false,
  "message": "File not found"
}
```

**Unauthorized Access:**
```json
{
  "success": false,
  "message": "Unauthorized access"
}
```

## Maintenance

### 🧹 **Cleanup Temporary Files**
```http
POST /api/files/cleanup-temp
Authorization: Bearer {token} (Admin role required)
```

This endpoint removes temporary files older than 24 hours.

---

## Next Steps

1. **Integration** - Integrate file uploads into patient dashboard
2. **Mobile Support** - Add mobile-optimized file upload
3. **Batch Operations** - Implement batch file uploads
4. **File Sharing** - Add secure file sharing between doctors
5. **DICOM Support** - Enhanced medical imaging support
6. **Cloud Storage** - Optional cloud storage integration
