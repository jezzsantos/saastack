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

        using var memoryStream2 = MemoryManager.GetStream();
        value.CopyTo(memoryStream2);
        return memoryStream2.ToArray();
    }
}