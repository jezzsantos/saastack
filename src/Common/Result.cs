using Common.Extensions;

namespace Common;

/// <summary>
///     Provides a result type that can either be a <see cref="TValue" /> OR a <see cref="TError" />, but never both, nor
///     neither
/// </summary>
public readonly struct Result<TValue, TError>
    where TError : struct
{
    private readonly TError? _error;
    private readonly Optional<TValue> _value;

    public Result(TValue value)
    {
        _value = new Optional<TValue>(value);
        _error = null;
    }

    public Result(Optional<TValue> value)
    {
        _value = value;
        _error = null;
    }

    public Result(TError error)
    {
        _error = error;
        _value = Optional<TValue>.None;
    }

    /// <summary>
    ///     Whether the contained result has a value
    /// </summary>
    public bool IsSuccessful => !_error.HasValue;

    /// <summary>
    ///     Returns the contained <see cref="Value" /> if there is one
    /// </summary>
    /// <exception cref="InvalidOperationException">If the result is in a faulted state</exception>
    public TValue Value
    {
        get
        {
            if (!IsSuccessful)
            {
                throw new InvalidOperationException(Resources.Result_FetchValueWhenFaulted.Format(_error!.Value));
            }

            return _value.Value;
        }
    }

    /// <summary>
    ///     Returns the contained <see cref="Error" /> if there is one
    /// </summary>
    /// <exception cref="InvalidOperationException">If the result is not in a faulted state</exception>
    public TError Error
    {
        get
        {
            if (IsSuccessful)
            {
                throw new InvalidOperationException(Resources.Result_FetchErrorWhenNotFaulted);
            }

            return _error!.Value;
        }
    }

    /// <summary>
    ///     Returns whether the <see cref="Value" /> exists
    /// </summary>
    public bool HasValue => IsSuccessful;

    /// <summary>
    ///     Returns whether the <see cref="Value" /> exists
    /// </summary>
    public bool Exists => IsSuccessful;

    /// <summary>
    ///     Creates a new <see cref="Result{TReturn, TError}" /> in its faulted state, with the <see cref="error" />
    /// </summary>
    public static Result<TValue, TError> FromError(TError error)
    {
        return new Result<TValue, TError>(error);
    }

    /// <summary>
    ///     Creates a new <see cref="Result{TReturn, TError}" /> in its success state, with the <see cref="value" />
    /// </summary>
    public static Result<TValue, TError> FromResult(TValue value)
    {
        return new Result<TValue, TError>(value);
    }

    /// <summary>
    ///     Tries to return the contained <see cref="value" />, if there is one
    /// </summary>
    public bool TryGet(out TValue value)
    {
        if (IsSuccessful)
        {
            value = _value.Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    ///     Tries to return the contained <see cref="error" />, if it is faulted
    /// </summary>
    public bool TryGetError(out TError? error)
    {
        if (!IsSuccessful)
        {
            error = _error!.Value;
            return true;
        }

        error = null;
        return false;
    }

    /// <summary>
    ///     Returns a string representation of the contained value or the contained error
    /// </summary>
    /// <returns></returns>
    public override string? ToString()
    {
        return IsSuccessful
            ? _value.ToString()
            : _error!.ToString();
    }

    /// <summary>
    ///     Converts the <see cref="value" /> into a <see cref="Result{TValue, TError}" />
    /// </summary>
    public static implicit operator Result<TValue, TError>(TValue value)
    {
        return new Result<TValue, TError>(value);
    }

    /// <summary>
    ///     Converts the <see cref="error" /> into a <see cref="Result{TValue, TError}" />
    /// </summary>
    public static implicit operator Result<TValue, TError>(TError error)
    {
        return new Result<TValue, TError>(error);
    }

    /// <summary>
    ///     Returns the contained <see cref="Value" />
    /// </summary>
    public static explicit operator TValue(in Result<TValue, TError> result)
    {
        return result.Value;
    }

    /// <summary>
    ///     Whether both the <see cref="left" /> and <see cref="right" /> results are successful
    /// </summary>
    public static bool operator &(in Result<TValue, TError> left, in Result<TValue, TError> right)
    {
        return left.IsSuccessful && right.IsSuccessful;
    }

    /// <summary>
    ///     Returns the result from the <see cref="onSuccess" /> delegate or <see cref="onError" /> delegate
    ///     depending on whether there is a contained value or not
    /// </summary>
    public TOut Match<TOut>(Func<Optional<TValue>, TOut> onSuccess, Func<TError, TOut> onError)
    {
        return IsSuccessful
            ? onSuccess(_value)
            : onError(_error!.Value);
    }
}