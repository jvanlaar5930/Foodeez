using FluentAssertions;
using Foodeez.Application.Common;
using Xunit;

namespace Foodeez.Application.Tests.Common;

/// <summary>
/// The mobile app derived the media type from the photo's file name, so a camera frame saved
/// as ".jpg" was uploaded as "image/jpg" - not a media type any vision provider accepts, so
/// every photograph came back as "that could not be read automatically". These are what say
/// the bytes now settle it, whatever the upload claimed.
/// </summary>
public class ImageMediaTypeTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
    private static readonly byte[] Gif = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00];
    // Both are containers: a tag, a size field, then the tag that says what is inside.
    private static readonly byte[] Webp = Header((0, "RIFF"), (8, "WEBP"));
    private static readonly byte[] Heic = Header((4, "ftyp"), (8, "heic"));

    [Theory]
    [InlineData("image/jpg")]
    [InlineData("image/jpeg")]
    [InlineData("application/octet-stream")]
    [InlineData(null)]
    public void Resolve_ReadsAJpegFromItsBytes_WhateverTheUploadClaimed(string? declared)
    {
        ImageMediaType.Resolve(Jpeg, declared).Should().Be("image/jpeg");
    }

    [Fact]
    public void Resolve_RecognisesTheOtherFormatsAProviderTakes()
    {
        ImageMediaType.Resolve(Png, "image/jpg").Should().Be("image/png");
        ImageMediaType.Resolve(Gif, null).Should().Be("image/gif");
        ImageMediaType.Resolve(Webp, null).Should().Be("image/webp");
        ImageMediaType.Resolve(Heic, null).Should().Be("image/heic");
    }

    [Theory]
    [InlineData("image/jpg", "image/jpeg")]
    [InlineData("IMAGE/JPEG", "image/jpeg")]
    [InlineData("image/png; charset=binary", "image/png")]
    [InlineData(" image/webp ", "image/webp")]
    [InlineData("image/heif", "image/heic")]
    public void Resolve_UnrecognisedBytes_FallBackToTheDeclaredTypeNormalised(string declared, string expected)
    {
        ImageMediaType.Resolve([0x00, 0x01, 0x02, 0x03], declared).Should().Be(expected);
    }

    [Theory]
    [InlineData("image/tiff")]
    [InlineData("")]
    [InlineData(null)]
    public void Resolve_NothingUsableEitherWay_AssumesJpeg(string? declared)
    {
        ImageMediaType.Resolve([0x00, 0x01, 0x02, 0x03], declared).Should().Be("image/jpeg");
    }

    [Fact]
    public void Resolve_TooFewBytesToSniff_DoesNotThrow()
    {
        ImageMediaType.Resolve([], "image/jpg").Should().Be("image/jpeg");
        ImageMediaType.Resolve([0xFF, 0xD8], null).Should().Be("image/jpeg");
    }

    /// <summary>Sixteen bytes with the given tags written at the given offsets.</summary>
    private static byte[] Header(params (int Offset, string Tag)[] tags)
    {
        var bytes = new byte[16];
        foreach (var (offset, tag) in tags)
        {
            for (var i = 0; i < tag.Length; i++) bytes[offset + i] = (byte)tag[i];
        }
        return bytes;
    }
}
