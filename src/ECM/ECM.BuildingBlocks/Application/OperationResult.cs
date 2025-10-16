namespace ECM.BuildingBlocks.Application;

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

    public static OperationResult<T> Success(T value) => new(true, value, []);

    public static OperationResult<T> Failure(params string[] errors) => new(false, default, errors);
}

public sealed class OperationResult
{
    private OperationResult(bool isSuccess, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<string> Errors { get; }

    public string Error => Errors.Count > 0 ? Errors[0] : string.Empty;

    public static OperationResult Success() => new(true, []);

    public static OperationResult Failure(params string[] errors) => new(false, errors);
}

public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
