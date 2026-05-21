using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Auth;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Application.UseCases.Auth;
using Foodeez.Domain.Entities;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

public class RegisterUseCaseTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly RegisterUseCase _sut;

    public RegisterUseCaseTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _jwtServiceMock = new Mock<IJwtService>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("test-jwt-token");

        _sut = new RegisterUseCase(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingUser = User.Create("test@example.com", "hash", "Jane", "Doe");
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(existingUser);

        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(request, _jwtServiceMock.Object));
    }

    [Fact]
    public async Task ExecuteAsync_NewUser_PasswordIsHashed()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        User? capturedUser = null;
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "SuperSecret123!",
            FirstName = "John",
            LastName = "Smith"
        };

        // Act
        await _sut.ExecuteAsync(request, _jwtServiceMock.Object);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().NotBe("SuperSecret123!");
        capturedUser.PasswordHash.Should().StartWith("$2"); // BCrypt prefix
        BCrypt.Net.BCrypt.Verify("SuperSecret123!", capturedUser.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_NewUser_ProfileIsCreated()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        User? capturedUser = null;
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "Alice",
            LastName = "Wonder"
        };

        // Act
        await _sut.ExecuteAsync(request, _jwtServiceMock.Object);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Profile.Should().NotBeNull();
        capturedUser.Profile!.ProfileCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_ReturnsAuthResponse()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123!",
            FirstName = "Bob",
            LastName = "Builder"
        };

        // Act
        var result = await _sut.ExecuteAsync(request, _jwtServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("test-jwt-token");
        result.User.Email.Should().Be("user@example.com");
        result.User.FirstName.Should().Be("Bob");
        result.User.LastName.Should().Be("Builder");
        result.User.ProfileCompleted.Should().BeFalse();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_SaveChangesIsCalled()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        await _sut.ExecuteAsync(request, _jwtServiceMock.Object);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
