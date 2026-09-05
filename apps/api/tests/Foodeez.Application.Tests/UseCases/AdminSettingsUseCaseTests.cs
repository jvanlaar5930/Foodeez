using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Admin;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.Admin;
using Foodeez.Domain.Entities;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

/// <summary>
/// The settings table holds API keys, and this is what decides whether an administrator's
/// browser receives them. Masking is the whole point of the endpoint, so it is worth an
/// assertion rather than a reading.
/// </summary>
public class AdminSettingsUseCaseTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAppSettingRepository> _settings = new();
    private readonly AdminSettingsUseCase _sut;

    public AdminSettingsUseCaseTests()
    {
        _unitOfWork.Setup(u => u.AppSettings).Returns(_settings.Object);
        _sut = new AdminSettingsUseCase(_unitOfWork.Object);
    }

    private void Stored(params AppSetting[] settings) =>
        _settings.Setup(r => r.GetAllAsync()).ReturnsAsync(settings);

    private static AppSetting Setting(string key, string value, bool isSecret) => new()
    {
        Key = key,
        Value = value,
        Category = "ai",
        IsSecret = isSecret
    };

    [Fact]
    public async Task ASecretIsMasked_KeepingOnlyEnoughToRecogniseIt()
    {
        Stored(Setting("claude.apiKey", "sk-ant-abcdefghijklmnop", isSecret: true));

        var value = (await _sut.ListAsync()).Single().Value;

        value.Should().StartWith("sk-a").And.EndWith("mnop");
        value.Should().NotContain("abcdefghijkl");
        value.Should().HaveLength("sk-ant-abcdefghijklmnop".Length);
    }

    [Fact]
    public async Task AShortSecretIsHiddenEntirely()
    {
        // Masking the middle of "abc12345" would leave nothing hidden.
        Stored(Setting("some.key", "abc12345", isSecret: true));

        (await _sut.ListAsync()).Single().Value.Should().Be("****");
    }

    [Fact]
    public async Task ASettingThatIsNotSecretIsShownAsItIs()
    {
        Stored(Setting("ai.provider", "claude", isSecret: false));

        (await _sut.ListAsync()).Single().Value.Should().Be("claude");
    }

    [Fact]
    public async Task AnEmptySecretStaysEmpty_RatherThanBecomingStars()
    {
        // "****" would read as "a key is set", which is the opposite of the truth.
        Stored(Setting("groq.apiKey", "", isSecret: true));

        (await _sut.ListAsync()).Single().Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Upsert_PassesEveryPairThrough()
    {
        IEnumerable<(string Key, string Value)>? written = null;
        _settings
            .Setup(r => r.UpsertManyAsync(It.IsAny<IEnumerable<(string, string)>>()))
            .Callback<IEnumerable<(string, string)>>(pairs => written = pairs.ToList())
            .Returns(Task.CompletedTask);

        await _sut.UpsertAsync(new UpsertSettingsRequest
        {
            Settings =
            [
                new UpsertSettingItem { Key = "ai.provider", Value = "gemini" },
                new UpsertSettingItem { Key = "gemini.apiKey", Value = "AIza-new" }
            ]
        });

        written.Should().BeEquivalentTo([("ai.provider", "gemini"), ("gemini.apiKey", "AIza-new")]);
    }
}
