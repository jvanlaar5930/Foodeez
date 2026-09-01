using Foodeez.Application.Common;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.MealLogs;
using Foodeez.Domain.Entities;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

public class DeleteMealLogUseCaseTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMealLogRepository> _mealLogRepoMock;
    private readonly DeleteMealLogUseCase _sut;

    public DeleteMealLogUseCaseTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mealLogRepoMock = new Mock<IMealLogRepository>();

        _unitOfWorkMock.Setup(u => u.MealLogs).Returns(_mealLogRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = new DeleteMealLogUseCase(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingMealLog_DeletesIt()
    {
        var mealLog = new MealLog { Id = Guid.NewGuid() };
        _mealLogRepoMock.Setup(r => r.GetDetailedByIdAsync(mealLog.Id)).ReturnsAsync(mealLog);

        await _sut.ExecuteAsync(mealLog.Id);

        _mealLogRepoMock.Verify(r => r.Delete(mealLog), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MissingMealLog_ThrowsKeyNotFoundException()
    {
        var mealLogId = Guid.NewGuid();
        _mealLogRepoMock.Setup(r => r.GetDetailedByIdAsync(mealLogId)).ReturnsAsync((MealLog?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ExecuteAsync(mealLogId));
    }
}
