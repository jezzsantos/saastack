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

        using var reusableStream = MemoryManager.GetStream("StreamReader");
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

        await using var memoryStream2 = MemoryManager.GetStream("StreamReader");
        await value.CopyToAsync(memoryStream2, cancellationToken);
        return memoryStream2.ToArray();
    }

    /// <summary>
    ///     Rewinds a stream to its beginning
    /// </summary>
    public static void Rewind(this Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }
    }
}