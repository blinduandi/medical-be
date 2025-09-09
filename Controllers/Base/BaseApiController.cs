using Microsoft.AspNetCore.Mvc;
using medical_be.Helpers;

namespace medical_be.Controllers.Base;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Validates the model state and returns validation error response if invalid
    /// </summary>
    protected IActionResult? ValidateModel()
    {
        return ApiResponse.ValidateModel(this);
    }

    /// <summary>
    /// Returns a standardized success response
    /// </summary>
    protected IActionResult SuccessResponse(object? data = null, string message = "Operation successful")
    {
        return ApiResponse.Success(data, message);
    }

    /// <summary>
    /// Returns a standardized error response
    /// </summary>
    protected IActionResult ErrorResponse(string message = "An error occurred", object? data = null)
    {
        return ApiResponse.Error(message, data);
    }

    /// <summary>
    /// Returns a standardized validation error response
    /// </summary>
    protected IActionResult ValidationErrorResponse(string message = "Validation failed", object? validationErrors = null)
    {
        return ApiResponse.ValidationError(message, validationErrors);
    }

    /// <summary>
    /// Returns a standardized not found response
    /// </summary>
    protected IActionResult NotFoundResponse(string message = "Resource not found")
    {
        return ApiResponse.NotFound(message);
    }

    /// <summary>
    /// Returns a standardized unauthorized response
    /// </summary>
    protected IActionResult UnauthorizedResponse(string message = "Unauthorized access")
    {
        return ApiResponse.Unauthorized(message);
    }

    /// <summary>
    /// Returns a standardized forbidden response
    /// </summary>
    protected IActionResult ForbiddenResponse(string message = "Access forbidden")
    {
        return ApiResponse.Forbidden(message);
    }

    /// <summary>
    /// Returns a standardized internal server error response
    /// </summary>
    protected IActionResult InternalServerErrorResponse(string message = "Internal server error occurred")
    {
        return ApiResponse.InternalServerError(message);
    }

    /// <summary>
    /// Returns a paginated response
    /// </summary>
    protected IActionResult PaginatedResponse<T>(
        IEnumerable<T> data, 
        int currentPage, 
        int pageSize, 
        int totalItems,
        string message = "Data retrieved successfully")
    {
        return ApiResponse.Paginated(data, currentPage, pageSize, totalItems, message);
    }
}
