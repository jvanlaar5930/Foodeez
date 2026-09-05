using FluentAssertions;
using Foodeez.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Foodeez.Integration.Tests;

/// <summary>
/// The AI registrations are resolved by name at runtime, so a mistake in them does not show up
/// at compile time - it shows up as a 500 the first time someone asks for an analysis. These
/// build the real container and ask it for what the controllers ask for.
/// </summary>
public class AIServiceRegistrationTests : IClassFixture<FoodeezWebApplicationFactory>
{
    private readonly FoodeezWebApplicationFactory _factory;

    public AIServiceRegistrationTests(FoodeezWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public void BothAIInterfaces_Resolve()
    {
        using var scope = _factory.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAIService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IStreamingAIService>().Should().NotBeNull();
    }

    [Fact]
    public void BothAIInterfaces_AreTheSameInstanceWithinARequest()
    {
        // They share the provider resolved from the settings table. Two instances would mean
        // two lookups per request, and could disagree if the setting changed between them.
        using var scope = _factory.Services.CreateScope();

        var ai = scope.ServiceProvider.GetRequiredService<IAIService>();
        var streaming = scope.ServiceProvider.GetRequiredService<IStreamingAIService>();

        streaming.Should().BeSameAs(ai);
    }

    [Fact]
    public void EachScope_GetsItsOwnInstance()
    {
        // The resolved provider is cached per scope, so it must not outlive the request - an
        // admin changing ai.provider has to take effect on the next one.
        using var first = _factory.Services.CreateScope();
        using var second = _factory.Services.CreateScope();

        first.ServiceProvider.GetRequiredService<IAIService>()
            .Should().NotBeSameAs(second.ServiceProvider.GetRequiredService<IAIService>());
    }

    [Theory]
    [InlineData(typeof(Foodeez.Infrastructure.Services.ClaudeAIService))]
    [InlineData(typeof(Foodeez.Infrastructure.Services.GeminiAIService))]
    [InlineData(typeof(Foodeez.Infrastructure.Services.GroqAIService))]
    [InlineData(typeof(Foodeez.Infrastructure.Services.OllamaAIService))]
    [InlineData(typeof(Foodeez.Infrastructure.Services.LocalAIService))]
    public void EveryConcreteProvider_CanBeResolvedByName(Type providerType)
    {
        // DynamicAIService reaches for these with GetRequiredService when ai.provider names
        // them, so an unregistered one fails only for whoever selected it.
        using var scope = _factory.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService(providerType).Should().NotBeNull();
    }
}
