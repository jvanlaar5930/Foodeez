using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    bool ValidateToken(string token, out Guid userId);
}
