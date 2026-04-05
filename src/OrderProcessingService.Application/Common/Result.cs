namespace OrderProcessingService.Application.Common;

public readonly struct Result<T, TError> where TError : class
{
    public bool IsSuccess { get; }

    public T? Value { get; }

    public TError? Error { get; }

    private Result(bool isSuccess, T? value, TError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T, TError> Success(T value) => new(true, value, null);

    public static Result<T, TError> Failure(TError error) => new(false, default, error);
}
