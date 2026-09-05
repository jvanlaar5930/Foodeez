using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Users;

namespace Foodeez.Application.UseCases.Users;

public class GetUserProfileUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserProfileUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserProfileDto> ExecuteAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{userId}' was not found.");

        var profile = user.Profile;
        if (profile == null)
            throw new KeyNotFoundException($"Profile for user '{userId}' was not found.");

        return UserProfileMapper.ToDto(profile);
    }
}
