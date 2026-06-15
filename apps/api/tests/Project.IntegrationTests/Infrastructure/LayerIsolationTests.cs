using System.Reflection;

namespace Project.IntegrationTests.Infrastructure;

/// <summary>
/// VERIFIES Clean Architecture boundary: Domain and Application assemblies
/// MUST NOT reference EF Core, Npgsql, or any persistence infrastructure.
/// </summary>
public sealed class LayerIsolationTests
{
    private static readonly string[] ForbiddenPrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
    ];

    [Fact]
    public void Domain_Assembly_Has_No_EfCore_Or_Npgsql_References()
    {
        var domainAssembly = typeof(Domain.Common.IClock).Assembly;

        var violations = FindViolations(domainAssembly, nameof(Domain));

        Assert.Empty(violations);
    }

    [Fact]
    public void Application_Assembly_Has_No_EfCore_Or_Npgsql_References()
    {
        var applicationAssembly = typeof(Application.Abstractions.Persistence.IBaseRepository<,>).Assembly;

        var violations = FindViolations(applicationAssembly, nameof(Application));

        Assert.Empty(violations);
    }

    private static List<string> FindViolations(Assembly assembly, string layerName)
    {
        var violations = new List<string>();

        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            foreach (var prefix in ForbiddenPrefixes)
            {
                if (reference.Name is not null && reference.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add(
                        $"Layer '{layerName}' references forbidden assembly: {reference.Name}");
                }
            }
        }

        return violations;
    }
}
