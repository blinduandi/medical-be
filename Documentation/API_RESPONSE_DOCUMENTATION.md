# API Response Structure Update

## 🔄 Updated Response Format

All API responses now return **HTTP 200** status codes with a consistent JSON structure that includes a `success` field to indicate the operation result.

## 📋 Response Structure

### ✅ Success Response
```json
{
  "success": true,
  "error": false,
  "message": "Operation successful",
  "data": {
    // Response data here
  }
}
```

### ❌ Error Response (Business Logic Errors)
```json
{
  "success": false,
  "error": true,
  "message": "Error description",
  "data": null
}
```

### 🔍 Validation Error Response
```json
{
  "success": false,
  "error": true,
  "message": "Validation failed",
  "validationErrors": {
    "fieldName": ["Error message 1", "Error message 2"],
    "anotherField": ["Error message"]
  }
}
```

### 📊 Paginated Response
```json
{
  "success": true,
  "error": false,
  "message": "Data retrieved successfully",
  "data": [
    // Array of items
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalItems": 100,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

## 🚦 HTTP Status Codes

### HTTP 200 - OK (Most Common)
- ✅ **Success operations**
- ❌ **Business logic errors** (validation, not found, etc.)
- 🔍 **Validation errors**
- 📊 **Paginated responses**

### HTTP 401 - Unauthorized
- 🔐 **Authentication required**
- 🚫 **Invalid JWT token**

### HTTP 403 - Forbidden  
- 🛡️ **Access denied** (valid user, insufficient permissions)

### HTTP 500 - Internal Server Error
- 💥 **Server errors** (unhandled exceptions, database issues)

## 🔧 Backend Implementation

### BaseApiController Methods
```csharp
// Success response (HTTP 200)
protected IActionResult SuccessResponse(object? data = null, string message = "Operation successful")

// Business logic error (HTTP 200)
protected IActionResult ErrorResponse(string message = "An error occurred", object? data = null)

// Validation error (HTTP 200)
protected IActionResult ValidationErrorResponse(string message = "Validation failed", object? validationErrors = null)

// Not found (HTTP 200)
protected IActionResult NotFoundResponse(string message = "Resource not found")

// Unauthorized (HTTP 401)
protected IActionResult UnauthorizedResponse(string message = "Unauthorized access")

// Forbidden (HTTP 403)
protected IActionResult ForbiddenResponse(string message = "Access forbidden")

// Internal Server Error (HTTP 500)
protected IActionResult InternalServerErrorResponse(string message = "Internal server error occurred")

// Paginated response (HTTP 200)
protected IActionResult PaginatedResponse<T>(
    IEnumerable<T> data, 
    int currentPage, 
    int pageSize, 
    int totalItems,
    string message = "Data retrieved successfully")
```

## 📱 Frontend Usage

### JavaScript/TypeScript Example
```typescript
interface ApiResponse<T = any> {
  success: boolean;
  error: boolean;
  message: string;
  data?: T;
  validationErrors?: Record<string, string[]>;
  pagination?: {
    currentPage: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  };
}

// Generic API call handler
async function apiCall<T>(url: string, options?: RequestInit): Promise<ApiResponse<T>> {
  try {
    const response = await fetch(url, {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token')}`,
        ...options?.headers
      },
      ...options
    });

    const result: ApiResponse<T> = await response.json();
    
    // Check for HTTP errors (401, 403, 500)
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${result.message || 'Unknown error'}`);
    }
    
    return result;
  } catch (error) {
    throw error;
  }
}

// Usage examples
async function loginUser(email: string, password: string) {
  const result = await apiCall<{ token: string; user: any }>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  });

  if (result.success) {
    // Login successful
    localStorage.setItem('token', result.data!.token);
    return result.data!.user;
  } else {
    // Business logic error (wrong credentials, etc.)
    throw new Error(result.message);
  }
}

async function getCurrentUser() {
  const result = await apiCall<any>('/api/auth/me');
  
  if (result.success) {
    return result.data;
  } else {
    throw new Error(result.message);
  }
}

async function uploadProfilePicture(file: File) {
  const formData = new FormData();
  formData.append('file', file);
  
  const result = await apiCall<any>('/api/files/profile-picture', {
    method: 'POST',
    body: formData,
    headers: {} // Don't set Content-Type for FormData
  });

  if (result.success) {
    return result.data;
  } else {
    // Could be validation error
    if (result.validationErrors) {
      console.error('Validation errors:', result.validationErrors);
    }
    throw new Error(result.message);
  }
}
```

### React Hook Example
```tsx
import { useState, useEffect } from 'react';

function useApiCall<T>(url: string, dependencies: any[] = []) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      setLoading(true);
      setError(null);
      
      try {
        const result = await apiCall<T>(url);
        if (result.success) {
          setData(result.data || null);
        } else {
          setError(result.message);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    }

    fetchData();
  }, dependencies);

  return { data, loading, error };
}

// Usage
function UserProfile() {
  const { data: user, loading, error } = useApiCall<any>('/api/auth/me');

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!user) return <div>No user data</div>;

  return (
    <div>
      <h1>{user.firstName} {user.lastName}</h1>
      {user.profilePicture && (
        <img src={user.profilePicture.thumbnailUrl} alt="Profile" />
      )}
    </div>
  );
}
```

## ✨ Benefits of This Approach

1. **Consistent Structure**: All responses follow the same pattern
2. **Easy Error Handling**: Frontend can check `success` field uniformly
3. **Better UX**: HTTP 200 prevents browser error alerts for business logic errors
4. **Clear Distinction**: Separates HTTP transport errors from business logic errors
5. **Flexible**: Can include additional metadata (validation errors, pagination)

## 🔄 Migration Guide

### Before (Mixed Status Codes)
```json
// HTTP 400 for validation error
{
  "error": true,
  "message": "Validation failed",
  "validationErrors": {...}
}

// HTTP 404 for not found
{
  "error": true,
  "message": "User not found"
}
```

### After (Consistent HTTP 200)
```json
// HTTP 200 for validation error
{
  "success": false,
  "error": true,
  "message": "Validation failed",
  "validationErrors": {...}
}

// HTTP 200 for not found
{
  "success": false,
  "error": true,
  "message": "User not found"
}
```

### Frontend Changes Required
```javascript
// Before: Check HTTP status
if (response.status === 400) {
  // Handle validation error
} else if (response.status === 404) {
  // Handle not found
}

// After: Check success field
const result = await response.json();
if (!result.success) {
  if (result.validationErrors) {
    // Handle validation error
  } else {
    // Handle other business logic errors
  }
}
```

This approach provides a more consistent and user-friendly API experience! 🚀
