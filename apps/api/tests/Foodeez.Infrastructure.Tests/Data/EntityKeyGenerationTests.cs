using Foodeez.Domain.Common;
using Foodeez.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foodeez.Infrastructure.Tests.Data;

public class EntityKeyGenerationTests
{
    /// <summary>
    /// Ids come from BaseEntity, never from the database. If the model says otherwise, EF reads
    /// an already-filled-in Guid as proof the row exists, so a new child attached to a loaded
    /// parent is saved as an UPDATE of a row that was never inserted - 0 rows affected, reported
    /// as a concurrency conflict on every edit.
    /// </summary>
    [Fact]
    public void EveryEntityId_IsNotStoreGenerated()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=none", new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        using var context = new AppDbContext(options);

        var storeGenerated = context.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType))
            .Select(e => e.FindProperty(nameof(BaseEntity.Id)))
            .Where(p => p is not null && p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never)
            .Select(p => $"{p!.DeclaringType.ClrType.Name}.{p.Name}")
            .ToList();

        storeGenerated.Should().BeEmpty();
    }
}
