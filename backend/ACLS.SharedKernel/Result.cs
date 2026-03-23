namespace ACLS.SharedKernel;

/// <summary>
/// Represents the outcome of an operation that returns a value of type T.
/// Use Result{T}.Success(value) for successful outcomes.
/// Use Result{T}.Failure(error) for expected business rule failures.
/// Exceptions are reserved for truly unexpected infrastructure failures.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = Error.None;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The success value. Throws InvalidOperationException if the result is a failure.
    /// Always check IsSuccess before accessing Value.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            $"Cannot access Value on a failed Result. Error: {_error!.Code} — {_error.Message}");

    /// <summary>
    /// The failure error. Throws InvalidOperationException if the result is a success.
    /// Always check IsFailure before accessing Error.
    /// </summary>
    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException(
            "Cannot access Error on a successful Result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Implicitly converts a value to a successful Result{T}.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts an Error to a failed Result{T}.
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}

/// <summary>
/// Represents the outcome of an operation that returns no value (void operations).
/// Use for commands that succeed or fail without producing a return value.
/// </summary>
public sealed class Result
{
    private readonly Error? _error;

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The failure error. Throws InvalidOperationException if the result is a success.
    /// </summary>
    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException(
            "Cannot access Error on a successful Result.");

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Implicitly converts an Error to a failed Result.
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);
}
