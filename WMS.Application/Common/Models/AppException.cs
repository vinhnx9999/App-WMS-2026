namespace WMS.Application.Common.Models;

/// <summary>
/// AppException is a custom exception class that extends the base Exception class. It is designed to represent application-specific errors that can occur in the WMS application. It includes additional properties such as StatusCode and Code to provide more context about the error and how it should be handled by clients.
/// </summary>
/// <param name="statusCode">The HTTP status code to return to the client</param>
/// <param name="code">The error code for identification</param>
/// <param name="message">Description of the exception</param>
public class AppException(int statusCode, string code, string message) : Exception(message)
{
    /// <summary>
    /// StatusCode is the HTTP status code that should be returned to the client when this exception is thrown. It should be a valid HTTP status code (e.g. 400 for bad request, 404 for not found, 500 for internal server error).
    /// </summary>
    public int StatusCode { get; } = statusCode;
    /// <summary>
    /// Code is the message code that can be used by clients to identify the error type and handle it accordingly. It should be a short, uppercase string with underscores as separators (e.g. "VALIDATION_FAILED", "NOT_FOUND", "DUPLICATE_ENTRY").
    /// </summary>
    public string Code { get; } = code;
}
