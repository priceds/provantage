namespace ProVantage.Application.Common.Models;

/// <summary>
/// Encapsulates the result of an operation with success/failure semantics.
/// Avoids exceptions for expected failures (validation, not-found, etc.).
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public int? StatusCode { get; }

    protected Result(bool isSuccess, string error, int? statusCode = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error, int statusCode = 400) => new(false, error, statusCode);
    public static Result NotFound(string message = "Resource not found.") => new(false, message, 404);
    public static Result Forbidden(string message = "Access denied.") => new(false, message, 403);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string error, int? statusCode = null)
        : base(isSuccess, error, statusCode)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public new static Result<T> Failure(string error, int statusCode = 400) => new(false, default, error, statusCode);
    public new static Result<T> NotFound(string message = "Resource not found.") => new(false, default, message, 404);
    public new static Result<T> Forbidden(string message = "Access denied.") => new(false, default, message, 403);
}
