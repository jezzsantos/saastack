using System.Diagnostics;
using System.Runtime.InteropServices;
using Common.Extensions;
using JetBrains.Annotations;

namespace Common;

public static class Result
{
    /// <summary>
    ///     Returns a successful result
    /// </summary>
    public static Result<Error> Ok => new();
}

/// <summary>
///     Provides a result type that can either be success OR a <see cref="TError" />, but never both, nor
///     neither
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Result<TError>
    where TError : struct
{
    private const string OkStringValue = "OK";

    private readonly TError? _error;

    public Result()
    {
        _error = null;
    }

    public Result(TError error)
    {
        _error = error;
    }

    /// <summary>
    ///     Whether the contained result has a value
    /// </summary>
    public bool IsSuccessful => !_error.HasValue;

    /// <summary>
    ///     Whether the contained result has an error
    /// </summary>
    public bool IsFailure => !IsSuccessful;

    /// <summary>
    ///     Returns the contained <see cref="Error" /> if there is one
    /// </summary>
    /// <exception cref="InvalidOperationException">If the result is not in a faulted state</exception>
    public TError Error
    {
        [DebuggerStepThrough]
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
    ///     Creates a new <see cref="Result{TError}" /> in its faulted state, with the <see cref="error" />
    /// </summary>
    public static Result<TError> FromError(TError error)
    {
        return new Result<TError>(error);
    }

    /// <summary>
    ///     Tries to return the contained <see cref="error" />, if it is faulted
    /// </summary>
    public bool TryGetError(out TError? error)
    {
        if (IsFailure)
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
            ? OkStringValue
            : _error!.ToString();
    }

    /// <summary>
    ///     Converts the <see cref="error" /> into a <see cref="Result{TError}" />
    /// </summary>
    public static implicit operator Result<TError>(TError error)
    {
        return new Result<TError>(error);
    }

    /// <summary>
    ///     Whether both the <see cref="left" /> and <see cref="right" /> results are successful
    /// </summary>
    public static bool operator &(in Result<TError> left, in Result<TError> right)
    {
        return left.IsSuccessful && right.IsSuccessful;
    }

    /// <summary>
    ///     Returns the result from the <see cref="onSuccess" /> delegate or <see cref="onError" /> delegate
    ///     depending on whether there is a successful value or not
    /// </summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<TError, TOut> onError)
    {
        return IsSuccessful
            ? onSuccess()
            : onError(_error!.Value);
    }
}

/// <summary>
///     Provides a result type that can either be a <see cref="TValue" /> OR a <see cref="TError" />, but never both, nor
///     neither
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Result<TValue, TError>
    where TError : struct
{
    private const string NoValueStringValue = "unspecified";
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
    public bool IsSuccessful
    {
        [DebuggerStepThrough] get => !_error.HasValue;
    }

    /// <summary>
    ///     Whether the contained result has an error
    /// </summary>
    public bool IsFailure
    {
        [DebuggerStepThrough] get => !IsSuccessful;
    }

    /// <summary>
    ///     Returns the contained <see cref="Value" /> if there is one
    /// </summary>
    /// <exception cref="InvalidOperationException">If the result is in a faulted state</exception>
    public TValue Value
    {
        [DebuggerStepThrough]
        get
        {
            if (IsFailure)
            {
                var errorDescription = _error!.HasValue
                    ? _error.Value.ToJson()!
                    : NoValueStringValue;
                throw new InvalidOperationException(Resources.Result_FetchValueWhenFaulted.Format(errorDescription));
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
        [DebuggerStepThrough]
        get
        {
            if (IsSuccessful)
            {
                var successDescription = _value.HasValue
                    ? _value.Value.ToJson()!
                    : NoValueStringValue;
                throw new InvalidOperationException(
                    Resources.Result_FetchErrorWhenNotFaulted.Format(successDescription));
            }

            return _error!.Value;
        }
    }

    /// <summary>
    ///     Returns whether the contained <see cref="Value" /> has a value
    /// </summary>
    public bool HasValue
    {
        [DebuggerStepThrough] get => IsSuccessful && _value.HasValue;
    }

    /// <summary>
    ///     Returns whether the contained <see cref="Value" /> has a value
    /// </summary>
    public bool Exists
    {
        [DebuggerStepThrough] get => HasValue;
    }

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
    ///     Tries to return the contained <see cref="Value" />, if there is one
    /// </summary>
    [ContractAnnotation("=> true, value: notnull; => false, value: null")]
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
    [ContractAnnotation("=> true, error: notnull; => false, error: null")]
    public bool TryGetError(out TError? error)
    {
        if (IsFailure)
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
    [DebuggerStepThrough]
    public TOut Match<TOut>(Func<Optional<TValue>, TOut> onSuccess, Func<TError, TOut> onError)
    {
        return IsSuccessful
            ? onSuccess(_value)
            : onError(_error!.Value);
    }
}