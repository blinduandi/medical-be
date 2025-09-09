# API Response Helper Documentation

## Overview

This project now uses a standardized API response helper system inspired by PHP Laravel's response patterns. All controllers have been updated to use consistent response formats.

## Features

### 1. Standardized Response Format

All API responses now follow this consistent structure:

```json
{
  "error": false,
  "message": "Operation successful",
  "data": { ... }
}
```

For errors:
```json
{
  "error": true,
  "message": "Error description",
  "data": null
}
```

### 2. Base API Controller

All controllers now inherit from `BaseApiController` which provides convenience methods:

- `SuccessResponse(data, message)` - Success with data
- `ErrorResponse(message, data)` - General error
- `ValidationErrorResponse(message, validationErrors)` - Validation failures
- `NotFoundResponse(message)` - 404 responses
- `UnauthorizedResponse(message)` - 401 responses  
- `ForbiddenResponse(message)` - 403 responses
- `InternalServerErrorResponse(message)` - 500 responses
- `PaginatedResponse(data, page, pageSize, totalItems, message)` - Paginated data

### 3. Automatic Model Validation

Controllers now use `ValidateModel()` which automatically checks ModelState and returns validation errors in the standard format.

### 4. Validation Documentation

Development endpoint `/api/Auth/validation-docs` provides validation documentation for DTOs:

- `/api/Auth/validation-docs?model=register` - Registration validation rules
- `/api/Auth/validation-docs?model=login` - Login validation rules
- etc.

## Usage Examples

### Success Response
```csharp
var user = await _userService.GetUserAsync(id);
return SuccessResponse(user, "User retrieved successfully");
```

### Error Response
```csharp
if (user == null)
    return NotFoundResponse("User not found");
```

### Validation
```csharp
public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
{
    var validationResult = ValidateModel();
    if (validationResult != null)
        return validationResult;
    
    // Process the request...
    return SuccessResponse(result, "User created successfully");
}
```

### Paginated Response
```csharp
return PaginatedResponse(users, page, pageSize, totalCount, "Users retrieved successfully");
```

## Benefits

1. **Consistency** - All API responses follow the same format
2. **Error Handling** - Standardized error responses across all endpoints
3. **Validation** - Automatic model validation with consistent error format
4. **Documentation** - Built-in validation documentation for development
5. **Maintainability** - Centralized response logic
6. **Frontend Integration** - Predictable response structure for client applications

## Updated Controllers

All controllers have been updated to use the new response system:

- ✅ `AuthController` - Authentication and registration
- ✅ `AdminController` - User management  
- ✅ `PatientController` - Patient operations
- ✅ `DocumentController` - Document management
- ✅ `MfaController` - Multi-factor authentication

## Breaking Changes

⚠️ **Important**: This is a breaking change for frontend applications. All API responses now use the new standardized format. Frontend applications will need to be updated to handle:

```javascript
// Old format
response.data

// New format  
response.data.data  // for actual data
response.data.error // for error status
response.data.message // for status message
```

## Testing

The validation documentation endpoint can be used to test the new response format:

```bash
GET /api/Auth/validation-docs?model=register
```

This will return validation rules and model attributes in the new response format.
