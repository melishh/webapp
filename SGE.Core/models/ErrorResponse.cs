namespace SGE.Core.Models;

/// <summary>
/// Represents a standardized structure for error responses in an application.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error message associated with the response.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the code that represents the specific error encountered.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code associated with the error response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the timestamp indicating when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the unique identifier for tracing the request.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets a dictionary containing validation errors for specific fields.
    /// </summary>
    public Dictionary<string, List<string>>? ValidationErrors { get; set; }

    /// <summary>
    /// Creates a new instance of the ErrorResponse class with the specified error details.
    /// </summary>
    public static ErrorResponse Create(string message, string errorCode, int statusCode, string? traceId = null)
    {
        return new ErrorResponse
        {
            Message = message,
            ErrorCode = errorCode,
            StatusCode = statusCode,
            TraceId = traceId
        };
    }

    /// <summary>
    /// Creates a new instance of the ErrorResponse class specifically for validation errors.
    /// </summary>
    public static ErrorResponse CreateValidation(Dictionary<string, List<string>> validationErrors, string? traceId = null)
    {
        return new ErrorResponse
        {
            Message = "Erreurs de validation",
            ErrorCode = "VALIDATION_ERROR",
            StatusCode = 400,
            ValidationErrors = validationErrors,
            TraceId = traceId
        };
    }
}
