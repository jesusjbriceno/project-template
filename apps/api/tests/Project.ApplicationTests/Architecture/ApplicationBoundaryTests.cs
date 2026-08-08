using System.Reflection;
using System.Text.RegularExpressions;
using Project.Application.Common;

namespace Project.ApplicationTests.Architecture;

/// <summary>
/// Architectural boundary tests for the Application layer.
/// Proves the Application layer is HTTP-agnostic and error codes carry no HTTP semantics.
/// </summary>
public sealed class ApplicationBoundaryTests
{
    /// <summary>
    /// The Application assembly must not reference any Microsoft.AspNetCore.* assembly.
    /// This ensures the Application layer remains HTTP-agnostic per Clean Architecture.
    /// </summary>
    [Fact]
    public void ApplicationAssembly_HasNoAspNetCoreReference()
    {
        // ARRANGE
        var applicationAssembly = typeof(ErrorCodes).Assembly;

        // ACT
        var referencedAssemblies = applicationAssembly.GetReferencedAssemblies();
        var aspNetCoreRefs = referencedAssemblies
            .Where(a => a.Name?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true)
            .Select(a => a.Name)
            .ToList();

        // ASSERT
        Assert.Empty(aspNetCoreRefs);
    }

    /// <summary>
    /// ErrorCodes constants must not contain numeric HTTP status codes (e.g., "401", "404", "500").
    /// HTTP status mapping is the API layer's responsibility.
    /// </summary>
    [Fact]
    public void ErrorCodes_Constants_HaveNoNumericHttpStatusSuffix()
    {
        // ARRANGE
        var errorCodeFields = typeof(ErrorCodes)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string));

        // ACT & ASSERT
        foreach (var field in errorCodeFields)
        {
            var value = (string)field.GetRawConstantValue()!;
            Assert.DoesNotMatch(new Regex(@"\d{3}"), value);
        }
    }
}
