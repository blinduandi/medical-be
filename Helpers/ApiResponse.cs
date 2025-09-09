using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace medical_be.Helpers;

public static class ApiResponse
{
    /// <summary>
    /// Returns a standardized 404 Not Found response
    /// </summary>
    public static IActionResult NotFound(string message = "Resource not found")
    {
        var result = new
        {
            error = true,
            message = message
        };

        return new ObjectResult(result) { StatusCode = 404 };
    }

    /// <summary>
    /// Returns a standardized success response with optional data
    /// </summary>
    public static IActionResult Success(object? data = null, string message = "Operation successful")
    {
        var result = new
        {
            error = false,
            message = message,
            data = data
        };

        return new OkObjectResult(result);
    }

    /// <summary>
    /// Returns a standardized error response with 200 status code
    /// </summary>
    public static IActionResult Error(string message = "An error occurred", object? data = null)
    {
        var result = new
        {
            error = true,
            message = message,
            data = data
        };

        return new OkObjectResult(result);
    }

    /// <summary>
    /// Returns a standardized error response with custom status code
    /// </summary>
    public static IActionResult ErrorWithStatus(string message = "An error occurred", int statusCode = 400, object? data = null)
    {
        var result = new
        {
            error = true,
            message = message,
            data = data
        };

        return new ObjectResult(result) { StatusCode = statusCode };
    }

    /// <summary>
    /// Returns a standardized validation error response
    /// </summary>
    public static IActionResult ValidationError(string message = "Validation failed", object? validationErrors = null)
    {
        var result = new
        {
            error = true,
            message = message,
            validationErrors = validationErrors
        };

        return new BadRequestObjectResult(result);
    }

    /// <summary>
    /// Returns a standardized unauthorized response
    /// </summary>
    public static IActionResult Unauthorized(string message = "Unauthorized access")
    {
        var result = new
        {
            error = true,
            message = message
        };

        return new UnauthorizedObjectResult(result);
    }

    /// <summary>
    /// Returns a standardized forbidden response
    /// </summary>
    public static IActionResult Forbidden(string message = "Access forbidden")
    {
        var result = new
        {
            error = true,
            message = message
        };

        return new ObjectResult(result) { StatusCode = 403 };
    }

    /// <summary>
    /// Returns a standardized internal server error response
    /// </summary>
    public static IActionResult InternalServerError(string message = "Internal server error occurred")
    {
        var result = new
        {
            error = true,
            message = message
        };

        return new ObjectResult(result) { StatusCode = 500 };
    }

    /// <summary>
    /// Validates model state and returns appropriate response
    /// </summary>
    public static IActionResult? ValidateModel(ControllerBase controller)
    {
        if (!controller.ModelState.IsValid)
        {
            var errors = controller.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return ValidationError("Validation failed", errors);
        }

        return null;
    }

    /// <summary>
    /// Returns model validation documentation (for development)
    /// </summary>
    public static IActionResult GetValidationDocumentation<T>(bool includeModelAttributes = false) where T : class, new()
    {
        var validationRules = GetValidationRules<T>();
        
        var result = new
        {
            error = false,
            message = "Validation documentation",
            validations = validationRules
        };

        if (includeModelAttributes)
        {
            var modelAttributes = GetModelAttributes<T>();
            return Success(new { 
                validations = validationRules,
                modelAttributes = modelAttributes 
            }, "Validation documentation with model attributes");
        }

        return Success(result, "Validation documentation");
    }

    /// <summary>
    /// Gets validation rules for a model type
    /// </summary>
    private static Dictionary<string, List<string>> GetValidationRules<T>() where T : class, new()
    {
        var type = typeof(T);
        var properties = type.GetProperties();
        var validationRules = new Dictionary<string, List<string>>();

        foreach (var property in properties)
        {
            var rules = new List<string>();
            var attributes = property.GetCustomAttributes<ValidationAttribute>();

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case RequiredAttribute:
                        rules.Add("required");
                        break;
                    case EmailAddressAttribute:
                        rules.Add("email");
                        break;
                    case PhoneAttribute:
                        rules.Add("phone");
                        break;
                    case MinLengthAttribute minLength:
                        rules.Add($"min_length:{minLength.Length}");
                        break;
                    case MaxLengthAttribute maxLength:
                        rules.Add($"max_length:{maxLength.Length}");
                        break;
                    case CompareAttribute compare:
                        rules.Add($"compare:{compare.OtherProperty}");
                        break;
                    case RangeAttribute range:
                        rules.Add($"range:{range.Minimum}-{range.Maximum}");
                        break;
                }
            }

            if (rules.Any())
            {
                validationRules[property.Name] = rules;
            }
        }

        return validationRules;
    }

    /// <summary>
    /// Gets model attributes for a type
    /// </summary>
    private static List<string> GetModelAttributes<T>() where T : class, new()
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        return properties.Select(p => p.Name).ToList();
    }

    /// <summary>
    /// Creates a paginated response
    /// </summary>
    public static IActionResult Paginated<T>(
        IEnumerable<T> data, 
        int currentPage, 
        int pageSize, 
        int totalItems,
        string message = "Data retrieved successfully")
    {
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        
        var result = new
        {
            error = false,
            message = message,
            data = data,
            pagination = new
            {
                currentPage = currentPage,
                pageSize = pageSize,
                totalItems = totalItems,
                totalPages = totalPages,
                hasNextPage = currentPage < totalPages,
                hasPreviousPage = currentPage > 1
            }
        };

        return new OkObjectResult(result);
    }
}
