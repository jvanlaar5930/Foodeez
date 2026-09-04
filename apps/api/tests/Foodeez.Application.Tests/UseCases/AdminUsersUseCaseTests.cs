using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.Admin;
using Foodeez.Domain.Entities;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

/// <summary>
/// The rule that an administrator cannot demote or disable themselves used to live in the
/// controller, which made it an HTTP rule rather than a rule about accounts - and it is the
/// only thing standing between the last administrator and a system nobody can administer.
/// </summary>
public class AdminUsersUseCaseTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly AdminUsersUseCase _sut;

    public AdminUsersUseCaseTests()
    {
        _unitOfWork.Setup(u => u.Users).Returns(_users.Object);
        _sut = new AdminUsersUseCase(_unitOfWork.Object);
    }

    private User Existing(bool isAdmin = false, bool isActive = true)
    {
        var user = User.Create("someone@test.com", "hash", "Some", "One");
        user.IsAdmin = isAdmin;
        user.IsActive = isActive;
        _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task ToggleAdmin_OnSomeoneElse_FlipsIt()
    {
        var target = Existing(isAdmin: false);

        var result = await _sut.ToggleAdminAsync(target.Id, Guid.NewGuid());

        result.Outcome.Should().Be(AdminUsersUseCase.ToggleOutcome.Changed);
        result.Value.Should().BeTrue();
        target.IsAdmin.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleAdmin_OnYourself_IsRefused_AndSavesNothing()
    {
        var self = Existing(isAdmin: true);

        var result = await _sut.ToggleAdminAsync(self.Id, self.Id);

        result.Outcome.Should().Be(AdminUsersUseCase.ToggleOutcome.RefusedSelf);
        result.Reason.Should().NotBeNullOrWhiteSpace();
        self.IsAdmin.Should().BeTrue(because: "the last administrator must not be able to lock everyone out");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleActive_OnYourself_IsRefused()
    {
        var self = Existing(isActive: true);

        var result = await _sut.ToggleActiveAsync(self.Id, self.Id);

        result.Outcome.Should().Be(AdminUsersUseCase.ToggleOutcome.RefusedSelf);
        self.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActive_OnSomeoneElse_FlipsIt()
    {
        var target = Existing(isActive: true);

        var result = await _sut.ToggleActiveAsync(target.Id, Guid.NewGuid());

        result.Outcome.Should().Be(AdminUsersUseCase.ToggleOutcome.Changed);
        result.Value.Should().BeFalse();
        target.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Toggling_AnAccountThatIsNotThere_SaysSo()
    {
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var result = await _sut.ToggleAdminAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Outcome.Should().Be(AdminUsersUseCase.ToggleOutcome.NotFound);
    }

    [Fact]
    public async Task SelfCheck_HappensBeforeTheAccountIsEvenLoaded()
    {
        // Refusing early is what makes the rule hold for an id that does not exist either.
        var id = Guid.NewGuid();

        await _sut.ToggleAdminAsync(id, id);

        _users.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }
}
