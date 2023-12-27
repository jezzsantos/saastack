using Microsoft.IO;

namespace Common.Extensions;

public static class StreamExtensions
{
    private static readonly RecyclableMemoryStreamManager MemoryManager = new();

    /// <summary>
    ///     Returns the bytes of the <see cref="value" /> copied into memory
    /// </summary>
    public static byte[] ReadFully(this Stream value)
    {
        if (value is MemoryStream memoryStream)
        {
            return memoryStream.ToArray();
        }

        using var reusableStream = MemoryManager.GetStream();
        value.CopyTo(reusableStream);

        return reusableStream.ToArray();
    }

    /// <summary>
    ///     Returns the bytes of the <see cref="value" /> copied into memory
    /// </summary>
    public static async Task<byte[]> ReadFullyAsync(this Stream value, CancellationToken cancellationToken)
    {
        if (value is MemoryStream memoryStream)
        {
            return memoryStream.ToArray();
        }

        using var memoryStream2 = MemoryManager.GetStream();
        await value.CopyToAsync(memoryStream2, cancellationToken);
        return memoryStream2.ToArray();
    }
}