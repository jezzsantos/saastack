using Common;

namespace UnitTesting.Common;

public static class ResultExtensions
{
    public static ResultAssertions<TValue> Should<TValue>(this Result<TValue, Error> instance)
    {
        return new ResultAssertions<TValue>(instance);
    }

    public static ResultAssertions Should(this Result<Error> instance)
    {
        return new ResultAssertions(instance);
    }

    public static ErrorAssertions Should(this Error instance)
    {
        return new ErrorAssertions(instance);
    }
}