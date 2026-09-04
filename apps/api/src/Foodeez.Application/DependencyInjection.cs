using Microsoft.Extensions.DependencyInjection;

namespace Foodeez.Application;

/// <summary>
/// Registers the application layer, mirroring <c>AddInfrastructure</c>.
///
/// Program.cs used to list all thirty-seven use cases by hand. A list like that is only ever
/// correct until the next feature: forgetting a line compiles cleanly and fails when someone
/// opens the page, because MVC only resolves a controller's dependencies when a request
/// arrives for it. Discovering them by convention means adding a use case is one file, and
/// <c>ControllerDependencyTests</c> proves every controller can actually be built.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Types named for what they do. Every use case ends in <c>UseCase</c>; the two
    /// collaborators that read data on a use case's behalf are named for that instead.
    /// </summary>
    private static readonly string[] Suffixes = ["UseCase", "Resolver", "Reader"];

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var types = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true }
                           && !type.IsGenericTypeDefinition
                           && Suffixes.Any(suffix => type.Name.EndsWith(suffix, StringComparison.Ordinal)));

        // Scoped, like the DbContext and repositories they use: one per request, and none
        // held past the end of it.
        foreach (var type in types)
        {
            services.AddScoped(type);
        }

        return services;
    }
}
