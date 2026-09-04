using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Foodeez.Integration.Tests;

/// <summary>
/// MVC only resolves a controller's dependencies when a request actually arrives for it, so a
/// use case that was never registered compiles, starts, passes every unit test, and then 500s
/// the first time someone opens that page. Program.cs used to list all thirty-seven of them by
/// hand, which made forgetting one the normal outcome of adding a feature.
///
/// AddApplication discovers them by convention now, and this asks the real container to build
/// every controller so a gap fails here instead.
/// </summary>
public class ControllerDependencyTests : IClassFixture<FoodeezWebApplicationFactory>
{
    private readonly FoodeezWebApplicationFactory _factory;

    public ControllerDependencyTests(FoodeezWebApplicationFactory factory) => _factory = factory;

    public static TheoryData<Type> Controllers()
    {
        var data = new TheoryData<Type>();

        var controllers = typeof(Program).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                           && typeof(ControllerBase).IsAssignableFrom(type));

        foreach (var controller in controllers)
        {
            data.Add(controller);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Controllers))]
    public void EveryController_CanBeConstructedFromTheContainer(Type controllerType)
    {
        using var scope = _factory.Services.CreateScope();

        var constructor = controllerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

        foreach (var parameter in constructor.GetParameters())
        {
            scope.ServiceProvider.GetService(parameter.ParameterType).Should().NotBeNull(
                $"{controllerType.Name} asks for {parameter.ParameterType.Name}, which nothing registers - " +
                "the endpoint would 500 on its first request");
        }
    }

    [Fact]
    public void TheControllersAreActuallyBeingChecked()
    {
        // A discovery predicate that matches nothing would make every test above vacuous.
        Controllers().Count.Should().BeGreaterThan(8);
    }
}
