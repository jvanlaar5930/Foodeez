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
/// The free/paid switch is the whole reason this provider exists separately, and it is the
/// one setting here that costs money when it is wrong. These cover which slug actually goes
/// on the wire for each combination of the two settings that decide it - including the case
/// where they disagree, because a paid slug quietly winning over a switch labelled "use free
/// models" is the expensive direction to fail in.
/// </summary>
public class OpenRouterAIServiceTests
{
    private const string PaidSlug = "anthropic/claude-sonnet-4.5";

    /// <summary>Answers every request with a canned completion and keeps what it was sent.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public Uri? Url { get; private set; }
        public int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
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

    private static (OpenRouterAIService Service, CapturingHandler Handler) Build(
        string? useFreeModels = null, string? model = null, string? supportsVision = null)
    {
        var handler = new CapturingHandler();

        var settings = new Mock<IAppSettingRepository>();
        settings.Setup(s => s.GetValueAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        settings.Setup(s => s.GetValueAsync("openrouter.apiKey")).ReturnsAsync("sk-or-test");
        settings.Setup(s => s.GetValueAsync("openrouter.useFreeModels")).ReturnsAsync(useFreeModels);
        settings.Setup(s => s.GetValueAsync("openrouter.model")).ReturnsAsync(model);
        settings.Setup(s => s.GetValueAsync("openrouter.supportsVision")).ReturnsAsync(supportsVision);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var service = new OpenRouterAIService(
            new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan },
            new ProviderConfiguration(settings.Object, configuration),
            NullLogger<OpenRouterAIService>.Instance);

        return (service, handler);
    }

    private static JsonElement Root(string body) => JsonDocument.Parse(body).RootElement;

    private static string ModelOf(string body) => Root(body).GetProperty("model").GetString()!;

    [Fact]
    public async Task FreeModels_AreTheDefault_WhenNothingIsConfigured()
    {
        var (service, handler) = Build();

        await service.ParseMealDescriptionAsync("an apple");

        ModelOf(handler.Body!).Should().Be("openrouter/free");
    }

    [Fact]
    public async Task FreeModels_WinOverAConfiguredPaidModel()
    {
        var (service, handler) = Build(model: PaidSlug);

        await service.ParseMealDescriptionAsync("an apple");

        ModelOf(handler.Body!).Should().Be("openrouter/free",
            "a paid slug must not outrank the switch that says to use free models");
    }

    [Fact]
    public async Task TurningFreeModelsOff_SendsTheConfiguredSlug()
    {
        var (service, handler) = Build(useFreeModels: "false", model: PaidSlug);

        await service.ParseMealDescriptionAsync("an apple");

        ModelOf(handler.Body!).Should().Be(PaidSlug);
    }

    [Fact]
    public async Task TurningFreeModelsOff_WithNoModelChosen_SaysWhichSettingIsMissing()
    {
        var (service, _) = Build(useFreeModels: "false");

        var act = async () =>
        {
            await foreach (var _ in service.StreamAsync("hello")) { }
        };

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Admin > Settings*");
    }

    [Fact]
    public async Task TheFreeRouter_TakesAPhoto_WithoutAnyVisionToggle()
    {
        var (service, handler) = Build();

        await service.ParseMealImageAsync([0xFF, 0xD8, 0xFF], "image/jpeg");

        handler.Calls.Should().Be(1, "the free router filters for the features a request needs");
    }

    [Fact]
    public async Task AChosenModel_IsNotSentAPhoto_UntilItIsDeclaredAVisionModel()
    {
        var (service, handler) = Build(useFreeModels: "false", model: PaidSlug);

        var result = await service.ParseMealImageAsync([0xFF, 0xD8, 0xFF], "image/jpeg");

        handler.Calls.Should().Be(0, "nothing here can tell whether a hand-picked slug can see");
        result.Note.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AChosenVisionModel_IsSentThePhoto_OnceItIsDeclared()
    {
        var (service, handler) = Build(useFreeModels: "false", model: PaidSlug, supportsVision: "true");

        await service.ParseMealImageAsync([0xFF, 0xD8, 0xFF], "image/jpeg");

        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task NoOutputCeiling_IsSent_UnlessOneIsConfigured()
    {
        var (service, handler) = Build();

        await service.ParseMealDescriptionAsync("an apple");

        Root(handler.Body!).TryGetProperty("max_tokens", out _).Should().BeFalse(
            "in free mode a different model answers each request, so any ceiling would be " +
            "wrong for some of them");
    }

    [Fact]
    public async Task EveryCall_GoesToTheOpenRouterCompletionsEndpoint()
    {
        var (service, handler) = Build();

        await service.ParseMealDescriptionAsync("an apple");

        handler.Url!.ToString().Should().Be("https://openrouter.ai/api/v1/chat/completions");
    }
}
