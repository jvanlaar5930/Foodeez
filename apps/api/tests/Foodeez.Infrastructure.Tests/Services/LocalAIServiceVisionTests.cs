using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Foodeez.Infrastructure.Tests.Services;

/// <summary>
/// A self-hosted vision model has to receive the photo on the same terms a hosted one does.
/// The media type is settled above the provider layer, but the local client is the one that
/// has to carry it into a data URI, and the OpenAI content shape it wraps the image in is
/// exact - "image_url", not the camelCase the shared serializer applies to everything else.
/// Both are the kind of thing a rename or a serializer change breaks silently, with the only
/// symptom being that photo logging quietly stops finding anything.
/// </summary>
public class LocalAIServiceVisionTests
{
    private const string BaseUrl = "http://localhost:1234/v1";

    /// <summary>Answers every request with a canned completion and keeps what it was sent.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public Uri? Url { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"choices":[{"message":{"content":"{\"items\":[]}"}}]}""",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private static (LocalAIService Service, CapturingHandler Handler) Build(bool supportsVision)
    {
        var handler = new CapturingHandler();

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        settings.Setup(s => s.GetValueAsync("local.supportsVision"))
            .ReturnsAsync(supportsVision ? "true" : "false");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalAI:BaseUrl"] = BaseUrl,
                ["LocalAI:Model"] = "qwen2-vl"
            })
            .Build();

        var service = new LocalAIService(
            new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan },
            configuration,
            settings.Object,
            NullLogger<LocalAIService>.Instance);

        return (service, handler);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public async Task ParseMealImage_PutsTheMediaTypeItWasGiven_IntoTheDataUri(string mimeType)
    {
        var (service, handler) = Build(supportsVision: true);

        await service.ParseMealImageAsync([0xFF, 0xD8, 0xFF], mimeType);

        var image = ImagePart(handler.Body!);
        image.GetProperty("image_url").GetProperty("url").GetString()
            .Should().StartWith($"data:{mimeType};base64,");
    }

    [Fact]
    public async Task ParseMealImage_SendsTheOpenAiVisionShape_ToTheChatCompletionsEndpoint()
    {
        var (service, handler) = Build(supportsVision: true);

        await service.ParseMealImageAsync([0xFF, 0xD8, 0xFF], "image/jpeg");

        handler.Url!.ToString().Should().Be($"{BaseUrl}/chat/completions");

        var image = ImagePart(handler.Body!);
        image.GetProperty("type").GetString().Should().Be("image_url");

        // The prompt travels with the picture; an image on its own asks the model nothing.
        Content(handler.Body!).EnumerateArray()
            .Should().Contain(part => part.GetProperty("type").GetString() == "text");
    }

    [Fact]
    public async Task ParseMealImage_VisionSwitchedOff_SendsNothingAndSaysWhy()
    {
        var (service, handler) = Build(supportsVision: false);

        var result = await service.ParseMealImageAsync([0xFF, 0xD8, 0xFF], "image/jpeg");

        handler.Body.Should().BeNull(because: "a text-only model only buys a slow failure");
        result.Items.Should().BeEmpty();

        // Whoever reads this has just taken a photograph, so it has to name the switch where
        // they can find it rather than the settings key it happens to be stored under.
        result.Note.Should().Contain("Local Model Supports Images").And.Contain("Admin");
    }

    private static JsonElement Content(string body) =>
        JsonDocument.Parse(body).RootElement
            .GetProperty("messages")[0]
            .GetProperty("content");

    private static JsonElement ImagePart(string body) =>
        Content(body).EnumerateArray()
            .Single(part => part.GetProperty("type").GetString() == "image_url");
}
