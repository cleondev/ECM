using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Shared.Utilities.ShortCode;

/// <summary>
/// Generates cryptographically strong short codes suitable for public share links.
/// </summary>
public sealed class ShortCodeGenerator : IDisposable
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private static readonly int AlphabetLength = Alphabet.Length;
    private readonly RandomNumberGenerator _rng;

    public ShortCodeGenerator()
    {
        _rng = RandomNumberGenerator.Create();
    }

    /// <summary>
    /// Generates a random short code consisting of base62 characters.
    /// </summary>
    /// <param name="length">Number of characters (inclusive range 6-16).</param>
    /// <returns>A random code.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested length is outside the supported range.</exception>
    public string Generate(int length = 8)
    {
        if (length is < 6 or > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be between 6 and 16 characters.");
        }

        Span<byte> randomBytes = stackalloc byte[length];
        _rng.GetBytes(randomBytes);

        Span<char> result = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            var index = randomBytes[i] % AlphabetLength;
            result[i] = Alphabet[index];
        }

        return new string(result);
    }

    /// <summary>
    /// Generates a deterministic short code from a sequence of bytes using base62 encoding.
    /// </summary>
    /// <remarks>This helper is useful for mapping ULIDs or UUIDs to shorter textual representations.</remarks>
    /// <param name="data">Arbitrary binary data.</param>
    /// <param name="length">Optional padding/truncation length; when null the entire encoded value is returned.</param>
    /// <returns>A base62 encoded string.</returns>
    public static string FromBytes(ReadOnlySpan<byte> data, int? length = null)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var buffer = new List<byte>(data.Length + 1);
        buffer.AddRange(data.ToArray());

        while (buffer.Count > 0)
        {
            var remainder = DivideBy(buffer, AlphabetLength);
            builder.Insert(0, Alphabet[remainder]);
        }

        if (length.HasValue)
        {
            if (length < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length must be at least 1 when specified.");
            }

            if (builder.Length > length.Value)
            {
                return builder.ToString(builder.Length - length.Value, length.Value);
            }

            return builder.ToString().PadLeft(length.Value, Alphabet[0]);
        }

        return builder.ToString();
    }

    private static byte DivideBy(List<byte> buffer, int divisor)
    {
        var remainder = 0;
        for (var i = 0; i < buffer.Count; i++)
        {
            var current = (remainder << 8) + buffer[i];
            buffer[i] = (byte)(current / divisor);
            remainder = current % divisor;
        }

        // Trim leading zeros to avoid infinite loops.
        while (buffer.Count > 0 && buffer[0] == 0)
        {
            buffer.RemoveAt(0);
        }

        return (byte)remainder;
    }

    public void Dispose()
    {
        _rng.Dispose();
    }
}
