namespace Ecm.Application.Common;

public sealed class OperationResult<T>
{
    private OperationResult(bool isSuccess, T? value, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public IReadOnlyList<string> Errors { get; }

    public static OperationResult<T> Success(T value) => new(true, value, Array.Empty<string>());

    public static OperationResult<T> Failure(params string[] errors) => new(false, default, errors);
}
