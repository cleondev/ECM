using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using FluentAssertions;
using Shared.Utilities.ShortCode;
using Xunit;

namespace Shared.Utilities.Tests;

public class ShortCodeGeneratorTests
{
    [Fact]
    public void Generate_ShouldRespectLengthBoundaries()
    {
        using var generator = new ShortCodeGenerator();
        generator.Invoking(g => g.Generate(5)).Should().Throw<ArgumentOutOfRangeException>();
        generator.Invoking(g => g.Generate(17)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(12)]
    public void Generate_ShouldReturnBase62Characters(int length)
    {
        using var generator = new ShortCodeGenerator();
        var code = generator.Generate(length);

        code.Should().HaveLength(length);
        code.Should().MatchRegex("^[0-9A-Za-z]+$");
    }

    [Fact]
    public void Generate_ShouldProduceLowCollisionRate()
    {
        using var generator = new ShortCodeGenerator();
        var set = new HashSet<string>();

        for (var i = 0; i < 10_000; i++)
        {
            set.Add(generator.Generate());
        }

        set.Should().HaveCount(10_000);
    }

    [Fact]
    public void FromBytes_ShouldEncodeAndTruncate()
    {
        Span<byte> buffer = stackalloc byte[16];
        RandomNumberGenerator.Fill(buffer);

        var encoded = ShortCodeGenerator.FromBytes(buffer, 10);

        encoded.Should().HaveLength(10);
        encoded.Should().MatchRegex("^[0-9A-Za-z]+$");
    }

    [Fact]
    public void FromBytes_ShouldBeDeterministic()
    {
        Span<byte> buffer = stackalloc byte[16];
        for (var i = 0; i < buffer.Length / 2; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer[(i * 2)..], (ushort)i);
        }

        var first = ShortCodeGenerator.FromBytes(buffer, 12);
        var second = ShortCodeGenerator.FromBytes(buffer, 12);

        first.Should().Be(second);
    }
}
