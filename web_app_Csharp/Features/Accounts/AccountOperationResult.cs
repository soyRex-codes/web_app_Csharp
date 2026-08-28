namespace web_app_Csharp.Features.Accounts;

public enum AccountOperationErrorKind
{
    NotFound,
    Forbidden,
    Validation,
    Conflict
}

public sealed record AccountOperationError(
    AccountOperationErrorKind Kind,
    string Message,
    string? Field = null,
    string? Title = null);

public sealed record AccountOperationResult<T>(T? Value, AccountOperationError? Error)
{
    public bool IsSuccess => Error is null;

    public static AccountOperationResult<T> Success(T value) => new(value, null);

    public static AccountOperationResult<T> Failure(AccountOperationError error) => new(default, error);
}
