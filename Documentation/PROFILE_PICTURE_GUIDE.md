# Profile Picture API Usage Guide

## Overview
This guide shows you how to work with profile pictures for logged-in users in the Medical API.

## 🔐 Authentication Required
All endpoints require a valid JWT token in the Authorization header:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

## 📋 Available Endpoints

### 1. Get Current User Info with Profile Picture
```http
GET /api/auth/me
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "success": true,
  "message": "User information retrieved successfully",
  "data": {
    "id": "user-id-here",
    "email": "patient@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "idnp": "1234567890123",
    "phoneNumber": "+373000000",
    "dateOfBirth": "1990-01-01T00:00:00Z",
    "gender": 1,
    "bloodType": "A+",
    "address": "Chisinau, Moldova",
    "clinicId": null,
    "isEmailVerified": true,
    "createdAt": "2025-09-11T06:00:00Z",
    "roles": ["Patient"],
    "profilePicture": {
      "id": "profile-picture-uuid",
      "name": "profile.jpg",
      "displayName": "Profile Picture",
      "size": 1048576,
      "sizeFormatted": "1.0 MB",
      "extension": ".jpg",
      "mimeType": "image/jpeg",
      "isImage": true,
      "width": 400,
      "height": 400,
      "blurHash": "LEHV6nWB2yk8pyo0adR*.7kCMdnj",
      "downloadUrl": "/api/files/profile-picture-uuid/download",
      "thumbnailUrl": "/api/files/profile-picture-uuid/thumbnail",
      "createdAt": "2025-09-11T06:00:00Z",
      "type": {
        "id": 1,
        "name": "Profile Photo",
        "category": "ProfilePhoto"
      }
    }
  }
}
```

**If no profile picture exists:**
```json
{
  "success": true,
  "message": "User information retrieved successfully", 
  "data": {
    "id": "user-id-here",
    "email": "patient@example.com",
    // ... other user data
    "profilePicture": null
  }
}
```

### 2. Get Only Profile Picture
```http
GET /api/files/profile-picture
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response (if exists):**
```json
{
  "success": true,
  "message": "Profile picture retrieved successfully",
  "data": {
    "id": "profile-picture-uuid",
    "name": "profile.jpg",
    "displayName": "Profile Picture",
    "downloadUrl": "/api/files/profile-picture-uuid/download",
    "thumbnailUrl": "/api/files/profile-picture-uuid/thumbnail",
    // ... other file properties
  }
}
```

**Response (if not found):**
```json
{
  "success": false,
  "message": "Profile picture not found"
}
```

### 3. Upload/Update Profile Picture
```http
POST /api/files/profile-picture
Content-Type: multipart/form-data
Authorization: Bearer YOUR_JWT_TOKEN

Form Data:
- file: [image file] (required)
```

**Response:**
```json
{
  "success": true,
  "message": "Profile picture uploaded successfully",
  "data": {
    "id": "new-profile-picture-uuid",
    "name": "new-profile.jpg",
    "displayName": "Profile Picture",
    "downloadUrl": "/api/files/new-profile-picture-uuid/download",
    "thumbnailUrl": "/api/files/new-profile-picture-uuid/thumbnail",
    // ... other file properties
  }
}
```

### 4. Download Profile Picture (Full Size)
```http
GET /api/files/{profile-picture-id}/download
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:** Binary image data with appropriate content type

### 5. Get Profile Picture Thumbnail
```http
GET /api/files/{profile-picture-id}/thumbnail
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:** Binary thumbnail image data (200x200px max)

### 6. Get Another User's Profile Picture (Doctor/Admin Only)
```http
GET /api/files/profile-picture/{user-id}
Authorization: Bearer YOUR_JWT_TOKEN
```

**Required Roles:** Doctor, Admin

## 🖼️ Supported Image Formats
- **JPG/JPEG** - Recommended for photos
- **PNG** - Recommended for graphics with transparency  
- **WEBP** - Modern, efficient format

## 📏 File Restrictions
- **Maximum Size:** 5MB
- **Image Processing:** Automatic thumbnail generation
- **Blur Hash:** Generated for preview/loading states

## 💡 Frontend Usage Examples

### JavaScript/Fetch Example
```javascript
// Get user info with profile picture
async function getCurrentUser() {
  const response = await fetch('/api/auth/me', {
    headers: {
      'Authorization': `Bearer ${localStorage.getItem('token')}`
    }
  });
  
  const result = await response.json();
  if (result.success) {
    const user = result.data;
    if (user.profilePicture) {
      // User has a profile picture
      console.log('Profile picture URL:', user.profilePicture.downloadUrl);
      console.log('Thumbnail URL:', user.profilePicture.thumbnailUrl);
    } else {
      // No profile picture - show default avatar
      console.log('No profile picture');
    }
  }
}

// Upload profile picture
async function uploadProfilePicture(fileInput) {
  const formData = new FormData();
  formData.append('file', fileInput.files[0]);
  
  const response = await fetch('/api/files/profile-picture', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${localStorage.getItem('token')}`
    },
    body: formData
  });
  
  const result = await response.json();
  if (result.success) {
    console.log('Profile picture uploaded:', result.data);
    // Refresh user info or update UI
  }
}
```

### React Example
```jsx
import { useState, useEffect } from 'react';

function UserProfile() {
  const [user, setUser] = useState(null);
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    loadUserInfo();
  }, []);

  const loadUserInfo = async () => {
    const response = await fetch('/api/auth/me', {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    });
    
    const result = await response.json();
    if (result.success) {
      setUser(result.data);
    }
  };

  const handleProfilePictureUpload = async (event) => {
    const file = event.target.files[0];
    if (!file) return;

    setUploading(true);
    const formData = new FormData();
    formData.append('file', file);
    
    try {
      const response = await fetch('/api/files/profile-picture', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: formData
      });
      
      const result = await response.json();
      if (result.success) {
        // Reload user info to get updated profile picture
        await loadUserInfo();
      }
    } catch (error) {
      console.error('Upload failed:', error);
    } finally {
      setUploading(false);
    }
  };

  if (!user) return <div>Loading...</div>;

  return (
    <div>
      <h2>{user.firstName} {user.lastName}</h2>
      
      {/* Profile Picture Display */}
      <div className="profile-picture">
        {user.profilePicture ? (
          <img 
            src={user.profilePicture.thumbnailUrl} 
            alt="Profile"
            width="100" 
            height="100"
            style={{ borderRadius: '50%' }}
          />
        ) : (
          <div className="default-avatar">
            {user.firstName[0]}{user.lastName[0]}
          </div>
        )}
      </div>

      {/* Upload Form */}
      <div>
        <input 
          type="file" 
          accept="image/*" 
          onChange={handleProfilePictureUpload}
          disabled={uploading}
        />
        {uploading && <p>Uploading...</p>}
      </div>
    </div>
  );
}
```

## 🔒 Security Notes

1. **Authentication Required:** All endpoints require valid JWT token
2. **File Validation:** Only image files are accepted for profile pictures
3. **Size Limits:** 5MB maximum file size enforced
4. **Auto-replacement:** Uploading a new profile picture automatically replaces the old one
5. **Role-based Access:** Users can only access their own profile pictures (except doctors/admins)

## 🚨 Error Handling

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

**No Authentication:**
```json
{
  "success": false,
  "message": "Unauthorized access"
}
```

**Profile Picture Not Found:**
```json
{
  "success": false,
  "message": "Profile picture not found"
}
```
