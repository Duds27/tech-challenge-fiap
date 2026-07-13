namespace OficinaMecanica.Application.Common;

public record ServiceResult<T>(bool Success, T? Data, string? Error, int StatusCode = 200)
{
    public static ServiceResult<T> Ok(T data) => new(true, data, null, 200);
    public static ServiceResult<T> Created(T data) => new(true, data, null, 201);
    public static ServiceResult<T> NotFound(string msg) => new(false, default, msg, 404);
    public static ServiceResult<T> BadRequest(string msg) => new(false, default, msg, 400);
    public static ServiceResult<T> Conflict(string msg) => new(false, default, msg, 409);
}
