using System.IO.Compression;

namespace Borderlands4SaveDataResignerCore.Helpers;

public class Zlib
{
    /// <summary>
    /// Decompresses a byte array that was compressed using the Zlib compression format.
    /// </summary>
    /// <param name="compressedData">A byte array containing the data to decompress.</param>
    /// <returns>A byte array containing the decompressed data.</returns>
    public static byte[] Decompress(byte[] compressedData)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var zLibStream = new ZLibStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        zLibStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Compresses the specified data using the Zlib compression algorithm.
    /// </summary>
    /// <param name="data">The data to compress, represented as a read-only span of bytes.</param>
    /// <returns>A byte array containing the compressed data.</returns>
    public static byte[] Compress(ReadOnlySpan<byte> data)
    {
        using var outputStream = new MemoryStream();
        using (var zLibStream = new ZLibStream(outputStream, CompressionLevel.Optimal, true))
        {
            zLibStream.Write(data);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    /// Computes the Adler-32 checksum for the specified data.
    /// </summary>
    /// <param name="data">A read-only span of bytes representing the input data for which the checksum is calculated.</param>
    /// <returns>The computed Adler-32 checksum as an unsigned 32-bit integer.</returns>
    public static uint ComputeAdler32Checksum(ReadOnlySpan<byte> data)
    {
        const uint modAdler = 65521;
        uint a = 1, b = 0;
        foreach (var c in data)
        {
            a = (a + c) % modAdler;
            b = (b + a) % modAdler;
        }
        return (b << 16) | a;
    }
}