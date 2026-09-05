using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Application.UseCases.MealLogs;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

/// <summary>
/// The media type is settled here, once, above the provider layer - so it reaches whichever
/// provider ai.provider happens to name: Claude, Gemini, or a self-hosted vision model behind
/// the OpenAI-compatible client, which puts it in the data URI it sends. A per-provider fix
/// would have left the local one still receiving "image/jpg" from the app.
/// </summary>
public class ParseMealImageUseCaseTests
{
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];

    private readonly Mock<IAIService> _aiServiceMock = new();
    private readonly ParseMealImageUseCase _sut;

    /// <summary>What the provider was actually handed, whatever the upload declared.</summary>
    private string? _mimeSeenByProvider;

    public ParseMealImageUseCaseTests()
    {
        _aiServiceMock
            .Setup(ai => ai.ParseMealImageAsync(It.IsAny<byte[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((byte[] _, string? mimeType, CancellationToken _) => _mimeSeenByProvider = mimeType)
            .ReturnsAsync(new ParsedMealDto());

        _sut = new ParseMealImageUseCase(
            _aiServiceMock.Object,
            new ParsedMealResolver(new Mock<IUnitOfWork>().Object));
    }

    [Theory]
    [InlineData("image/jpg")]        // what the mobile app sent for every camera frame
    [InlineData("image/jpeg")]
    [InlineData("application/octet-stream")]
    [InlineData(null)]
    public async Task ExecuteAsync_GivesTheProviderARealMediaType(string? declared)
    {
        await _sut.ExecuteAsync(Guid.NewGuid(), JpegBytes, declared);

        _mimeSeenByProvider.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task ExecuteAsync_ReadsTheFormatFromTheImage_NotTheUpload()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

        await _sut.ExecuteAsync(Guid.NewGuid(), png, "image/jpg");

        _mimeSeenByProvider.Should().Be("image/png");
    }

    [Fact]
    public async Task ExecuteAsync_NothingRecognised_KeepsTheProvidersOwnExplanation()
    {
        // A provider that cannot see says so, and that note has to survive to the client
        // rather than being replaced by the generic "try a clearer shot".
        _aiServiceMock
            .Setup(ai => ai.ParseMealImageAsync(It.IsAny<byte[]>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParsedMealDto { Note = "Photos are switched off for the local model." });

        var result = await _sut.ExecuteAsync(Guid.NewGuid(), JpegBytes, "image/jpg");

        result.Items.Should().BeEmpty();
        result.Note.Should().Be("Photos are switched off for the local model.");
    }
}
